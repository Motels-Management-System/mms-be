using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mms.Api.Middleware;
using Mms.Application;
using Mms.Application.Common;
using Mms.Infrastructure;
using Mms.Infrastructure.BackgroundJobs;
using Mms.Infrastructure.Identity;
using Mms.Infrastructure.Persistence;
using Mms.Infrastructure.Persistence.SeedData;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    // CORS cho phép frontend (Vite/React) gọi API từ mọi origin
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // Swagger/OpenAPI with Bearer token support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Motel Management System API",
            Version = "v1",
            Description = "Hệ thống quản lý nhà trọ - API"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập JWT token"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Global exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Application + Infrastructure
    builder.Services.Configure<RentalOptions>(
        builder.Configuration.GetSection(RentalOptions.SectionName));
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health Checks: kiểm tra kết nối DB
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database", tags: ["db", "ready"]);

    // Integration test chạy ở môi trường Testing và gọi thẳng handler hết hạn,
    // nên background job không đăng ký ở đó (tránh timer can thiệp lúc đang assert).
    if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.Ordinal))
    {
        builder.Services.AddHostedService<ReservationExpiryJob>();
        builder.Services.AddHostedService<ContractExpiryJob>();
    }

    var app = builder.Build();

    // Middleware pipeline
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseCors();

    // Bật Swagger ở Development hoặc khi có cấu hình EnableSwagger (mặc định bật để xem demo trên Render)
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MMS API v1"));
    }

    // Profile http chỉ bind một endpoint nên không có cổng https để chuyển hướng tới.
    if (!app.Environment.IsDevelopment() && !string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // 1. Tự động kiểm tra và áp dụng Migration khi khởi động (chỉ chạy khi không phải môi trường Testing)
    if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.Ordinal))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            Log.Information("Đang kiểm tra và áp dụng Database Migrations...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Áp dụng Database Migrations thành công.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Không thể áp dụng Database Migrations lúc khởi động. Vui lòng kiểm tra chuỗi kết nối Database.");
            throw;
        }
    }

    // 2. Seed roles và tài khoản default admin
    try
    {
        await IdentitySeeder.SeedAsync(app.Services);

        // 3. Seed development data phong phú bằng Bogus (chỉ môi trường Development hoặc khi bật SeedDevelopmentData)
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SeedDevelopmentData", false))
        {
            await DevelopmentDataSeeder.SeedAsync(app.Services);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Chưa thể seed Identity/Development data lúc khởi động");
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Ứng dụng khởi động thất bại");
}
finally
{
    Log.CloseAndFlush();
}

// Lớp Program mặc định là internal với top-level statements; khai báo public để
// WebApplicationFactory của integration test tham chiếu được.
public partial class Program;
