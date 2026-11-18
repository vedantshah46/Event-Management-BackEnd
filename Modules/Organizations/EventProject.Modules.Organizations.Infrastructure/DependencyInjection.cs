using EventProject.Modules.Organizations.Core.Application.Interfaces;
using EventProject.Modules.Organizations.Core.Application.Services;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Infrastructure.Authorization;
using EventProject.Modules.Organizations.Infrastructure.Persistence;
using EventProject.Modules.Organizations.Infrastructure.Persistence.Configurations;
using EventProject.Modules.Organizations.Infrastructure.Persistence.Repositories;
using EventProject.Modules.Organizations.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Organizations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleModelConfiguration, OrganizationModelConfiguration>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationAccountService, OrganizationAccountService>();
        services.AddScoped<IOrganizationTransaction, OrganizationTransaction>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationStaffService, OrganizationStaffService>();
        services.AddScoped<IAuthorizationHandler, OrganizationPermissionHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("OrganizationProfile", policy => policy.RequireAuthenticatedUser().RequireRole("Organizer")
                .AddRequirements(new OrganizationPermissionRequirement(OrganizationPermissions.ViewProfile, false)));
            options.AddPolicy("ApprovedOrganization", policy => policy.RequireAuthenticatedUser().RequireRole("Organizer")
                .AddRequirements(new OrganizationPermissionRequirement(OrganizationPermissions.ViewProfile, true)));
        });
        return services;
    }
}
