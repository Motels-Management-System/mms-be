using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Mms.Application.Abstractions;
using Mms.Infrastructure.Identity;
using Mms.Infrastructure.Persistence;
using Mms.Infrastructure.Persistence.Interceptors;
using Mms.Infrastructure.Reports;
using Mms.Infrastructure.Services;
using Npgsql;

namespace Mms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Default")
            ?? configuration["DATABASE_URL"];

        var connectionString = NormalizeConnectionString(rawConnectionString);

        // DateTimeProvider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // HttpCurrentUserProvider cho AuditSaveChangesInterceptor
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();

        // EF Core + PostgreSQL
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Báo cáo SQL thuần
        services.AddScoped<IReportQueries, ReportQueries>();

        // ASP.NET Identity
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        // JWT service & Identity service
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }

    /// <summary>
    /// Chuyển đổi chuỗi kết nối PostgreSQL: hỗ trợ cả định dạng URI (postgres://... của Render, Railway, Supabase)
    /// lẫn định dạng chuẩn Key=Value của ADO.NET (Host=...;Port=5432;Database=...).
    /// </summary>
    private static string NormalizeConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var cleaned = raw.Trim().Trim('"', '\'');

        if (cleaned.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(cleaned);
            var userInfo = uri.UserInfo.Split(':');
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = user,
                Password = password,
                SslMode = SslMode.Prefer
            };

            return builder.ConnectionString;
        }

        return cleaned;
    }
}
