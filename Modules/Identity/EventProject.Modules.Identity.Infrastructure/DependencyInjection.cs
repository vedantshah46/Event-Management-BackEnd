using Shared.Core.Identity;
using System.Text;
using EventProject.Modules.Identity.Core.Application.Admin;
using EventProject.Modules.Identity.Infrastructure.Authentication;
using EventProject.Modules.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventProjectDbContext>((provider, options) =>
        {
            var connection = provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            options.UseSqlServer(connection);
        });
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<EventProjectDbContext>().AddSignInManager();

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAdminAccountStore, AdminAccountStore>();
        services.AddScoped<JwtTokenIssuer>();
        services.AddScoped<IAccessTokenIssuer>(provider => provider.GetRequiredService<JwtTokenIssuer>());
        services.AddScoped<IAccountTokenIssuer>(provider => provider.GetRequiredService<JwtTokenIssuer>());
        services.AddScoped<IAccountIdentityStore, AccountIdentityStore>();
        services.AddScoped<AdminAuthService>();
        services.AddScoped<AdminProvisioner>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configured) =>
            {
                var jwt = configured.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                    ValidateAudience = true, ValidAudience = jwt.Audience,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(5), NameClaimType = "sub", RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id))
                        {
                            context.Fail("Invalid account.");
                            return;
                        }
                        var principal = context.Principal!;
                        var roleClaim = principal.FindFirst("role")?.Value;
                        if (!Enum.TryParse<PlatformRole>(roleClaim, out var role) || !Enum.IsDefined(role)
                            || role.ToString() != roleClaim)
                        {
                            context.Fail("Invalid role.");
                            return;
                        }
                        AccountIdentity? identity;
                        if (role == PlatformRole.Admin)
                        {
                            var store = context.HttpContext.RequestServices.GetRequiredService<IAdminAccountStore>();
                            var admin = await store.FindActiveAsync(id, context.HttpContext.RequestAborted);
                            identity = admin is null ? null : new AccountIdentity(admin.Id, PlatformRole.Admin, admin.SecurityStamp);
                        }
                        else
                        {
                            var store = context.HttpContext.RequestServices.GetRequiredService<IAccountIdentityStore>();
                            identity = await store.FindActiveAsync(id, context.HttpContext.RequestAborted);
                        }
                        if (identity is null || identity.Role != role
                            || identity.SecurityStamp != principal.FindFirst("security_stamp")?.Value)
                            context.Fail("Account is no longer authorized.");
                    }
                };
            });
        services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Admin")));
        return services;
    }
}
