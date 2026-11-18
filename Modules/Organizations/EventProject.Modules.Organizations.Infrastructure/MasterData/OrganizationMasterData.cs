using System.Text.Json;
using System.Text.Json.Serialization;
using EventProject.Modules.Organizations.Core.Domain;
using EventProject.Modules.Organizations.Core.Domain.Entities;

namespace EventProject.Modules.Organizations.Infrastructure.MasterData;

public static class OrganizationMasterData
{
    public static (OrganizationRole[] Roles, OrganizationRolePermission[] Permissions, EventOrganizationStatusMaster[] Statuses) Load()
    {
        var statuses = Read<EventOrganizationStatusMaster>("EventOrganizationStatus.json");
        if (statuses.Length != Enum.GetValues<OrganizationStatus>().Length
            || statuses.Select(status => status.EventOrganizationStatusId).Distinct().Count() != statuses.Length
            || statuses.Any(status => !Enum.IsDefined(status.EventOrganizationStatusId)
                || string.IsNullOrWhiteSpace(status.EventOrganizationStatus) || status.EventOrganizationStatus.Length > 100)
            || statuses.Select(status => status.EventOrganizationStatus).Distinct(StringComparer.OrdinalIgnoreCase).Count() != statuses.Length)
            throw new InvalidOperationException("EventOrganizationStatus.json requires every enum ID exactly once and unique valid status names.");
        var roles = Read<OrganizationRole>("OrganizationRoles.json");
        var permissions = Read<OrganizationRolePermission>("OrganizationRolePermissions.json");
        if (roles.Length == 0 || roles.Any(role => role.Id <= 0 || string.IsNullOrWhiteSpace(role.Code)
                || role.Code.Length > 50 || string.IsNullOrWhiteSpace(role.Name) || role.Name.Length > 100)
            || roles.Select(role => role.Id).Distinct().Count() != roles.Length
            || roles.Select(role => role.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != roles.Length
            || !roles.Any(role => role.Code == "OWNER" && role.IsActive))
            throw new InvalidOperationException("OrganizationRoles.json requires unique positive IDs/codes and an active OWNER role.");
        if (permissions.Any(permission => !roles.Any(role => role.Id == permission.OrganizationRoleId)
                || !OrganizationPermissions.All.Contains(permission.PermissionCode))
            || permissions.Select(permission => (permission.OrganizationRoleId, permission.PermissionCode)).Distinct().Count() != permissions.Length
            || roles.Where(role => role.IsActive).Any(role => !permissions.Any(permission =>
                permission.OrganizationRoleId == role.Id && permission.PermissionCode == OrganizationPermissions.ViewProfile)))
            throw new InvalidOperationException("OrganizationRolePermissions.json contains missing roles, unknown/duplicate permissions, or an active role without profile access.");
        return (roles.OrderBy(role => role.Id).ToArray(), permissions.OrderBy(permission => permission.OrganizationRoleId)
            .ThenBy(permission => permission.PermissionCode, StringComparer.Ordinal).ToArray(), statuses.OrderBy(status => status.EventOrganizationStatusId).ToArray());
    }

    private static T[] Read<T>(string file)
    {
        var assembly = typeof(OrganizationMasterData).Assembly;
        var resource = $"{assembly.GetName().Name}.MasterData.{file}";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded master data: {file}");
        return JsonSerializer.Deserialize<T[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidOperationException($"Empty master data: {file}");
    }
}
