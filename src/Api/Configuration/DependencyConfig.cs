using System.Text;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Auth.Validators;
using Core.Interfaces.Services;
using Core.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Api.Configuration;

public static class DependencyConfig
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<AdminLoginRequestValidator>();

        return services
            .AddSerilogLogging(configuration)
            .AddEndpointsApiExplorer()
            .AddOpenApiSpec()
            .AddIdentity()
            .AddAuthentication(configuration)
            .AddPostgres(configuration)
            .AddRedis(configuration)
            .AddAppServices();
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) =>
        {
            lc.ReadFrom.Configuration(configuration)
              .ReadFrom.Services(sp)
              .Enrich.FromLogContext()
              .WriteTo.Console();

            var seqUrl = configuration["Seq:ServerUrl"];
            if (!string.IsNullOrEmpty(seqUrl))
                lc.WriteTo.Seq(seqUrl);
        });

        return services;
    }

    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Tandur Restaurant API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token"
            });

            c.OperationFilter<Api.Swagger.AuthorizeOperationFilter>();
        });

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<JwtService>();
        services.AddScoped<IRefreshTokenService, RedisRefreshTokenService>();
        services.AddScoped<IOtpService, RedisOtpService>();
        services.AddScoped<IOtpSender, ConsoleOtpSender>();
        services.AddScoped<IOtpSessionService, RedisOtpSessionService>();
        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")))
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(TandurPolicies.AdminPanel, policy =>
                policy.RequireRole(TandurRoles.Admin, TandurRoles.SuperAdmin));

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "tandur:";
        });

        return services;
    }

    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
