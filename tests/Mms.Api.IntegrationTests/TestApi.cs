using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Abstractions;
using Mms.Domain.Entities;
using Mms.Domain.ValueObjects;
using Mms.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;

namespace Mms.Api.IntegrationTests;

/// <summary>
/// Harness integration test: PostgreSQL thật trong container, migration thật được apply
/// (nên exclusion constraint và unique index tồn tại) + host API thật.
/// </summary>
public sealed class TestApi
{
    /// <summary>Gọi với token rỗng khi cần kiểm tra hành vi của khách chưa đăng nhập.</summary>
    public const string NoAuth = "";

    public const string AdminEmail = "admin@mms.local";
    public const string AdminPassword = "Admin@123456";

    /// <summary>
    /// "Hôm nay" theo múi giờ Việt Nam, cùng cách tính với IDateTimeProvider của API
    /// nên các mốc như DaysRemaining so sánh được. Không thay clock bằng giá trị cố định:
    /// JwtBearer xác minh hạn token theo giờ hệ thống, đồng hồ giả sẽ làm mọi request 401.
    /// </summary>
    public DateOnly Today { get; private set; } = VietnamToday();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // API serialize enum dạng string (JsonStringEnumConverter trong Program.cs).
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("MotelManagementSystem")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = default!;

    public string AdminToken { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();

        await using (var migrationContext = CreateContext(connectionString))
        {
            await migrationContext.Database.MigrateAsync();
        }

        _factory = new MmsWebApplicationFactory(connectionString);
        Client = _factory.CreateClient();

        // IdentitySeeder chạy lúc khởi động host nên tài khoản Admin đã có sẵn.
        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login", new { email = AdminEmail, password = AdminPassword });
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        AdminToken = auth!.AccessToken;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public Task<HttpResponseMessage> GetAsync(string path, string? token = null)
        => SendAsync(HttpMethod.Get, path, null, token);

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null, string? token = null)
        => SendAsync(HttpMethod.Post, path, body, token);

    public Task<HttpResponseMessage> PutAsync(string path, object? body = null, string? token = null)
        => SendAsync(HttpMethod.Put, path, body, token);

    public Task<HttpResponseMessage> DeleteAsync(string path, string? token = null)
        => SendAsync(HttpMethod.Delete, path, null, token);

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, string? token)
    {
        var request = new HttpRequestMessage(method, path);

        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        var bearer = token ?? AdminToken;
        if (!string.IsNullOrEmpty(bearer))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        return await Client.SendAsync(request);
    }

    /// <summary>Đọc JSON khi mong đợi status 2xx; thất bại thì ném kèm body để dễ debug.</summary>
    public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"HTTP {(int)response.StatusCode} {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {text}");

        return JsonSerializer.Deserialize<T>(text, JsonOptions)!;
    }

    public static async Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ProblemResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions)!;

    /// <summary>Truy cập DB thật của container, dùng để chuẩn bị dữ liệu mà API chưa tạo được.</summary>
    public async Task<T> InDatabaseAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory!.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>
    /// Gọi thẳng service trong DI của host. Background job không chạy ở môi trường Testing
    /// nên test logic hết hạn dùng cách này, không phải chờ timer.
    /// </summary>
    public async Task<T> InServicesAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory!.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Tạo loại phòng + phòng + hợp đồng đang hiệu lực với một người đại diện,
    /// phục vụ các test cần hợp đồng hiện tại (endpoint hợp đồng thuộc Giai đoạn 3).
    /// </summary>
    public async Task<(Guid RoomId, Guid ContractId)> SeedActiveContractAsync(
        string roomCode, Guid tenantId, DateOnly start, DateOnly end)
        => await InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {roomCode}", 30, 2, 3_000_000m, null);
            var room = new Room(roomCode, 1, roomType.Id);
            var contract = new Contract(room.Id, new DateRange(start, end));

            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);
            contract.Activate();

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Contracts.Add(contract);

            await db.SaveChangesAsync();

            return (room.Id, contract.Id);
        });

    public static string UniqueCode(string prefix)
        => (prefix + Guid.NewGuid().ToString("N")).ToUpperInvariant()[..10];

    private static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new MigrationClock());
    }

    private static DateOnly VietnamToday()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }

        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
    }

    private sealed class MmsWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                var optionsDescriptor =
                    services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (optionsDescriptor is not null)
                    services.Remove(optionsDescriptor);

                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }

    /// <summary>Clock cho riêng context chạy migration, nơi không có host DI để lấy service.</summary>
    private sealed class MigrationClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateOnly Today => VietnamToday();
    }

    private sealed record AuthResponse(string AccessToken);
}
