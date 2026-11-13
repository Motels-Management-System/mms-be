using Microsoft.OpenApi.Models;
using Mms.Api.Middleware;
using Mms.Application;
using Mms.Application.Common;
using Mms.Infrastructure;
using Mms.Infrastructure.BackgroundJobs;
using Mms.Infrastructure.Identity;
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

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MMS API v1"));
    }

    // Profile http chỉ bind một endpoint nên không có cổng https để chuyển hướng tới.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Seed roles and default admin if database is reachable
    try
    {
        await IdentitySeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Chưa thể seed Identity data lúc khởi động (có thể DB chưa khởi động hoặc chưa update migration)");
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
