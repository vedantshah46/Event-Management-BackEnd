using System.ComponentModel.DataAnnotations;

namespace EventProject.Modules.Organizations.Core.Application.Interfaces;

public sealed class CreateStaffRequest
{
    [Required, StringLength(150)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [Required, Phone, StringLength(20)] public string PhoneNumber { get; init; } = string.Empty;
    [Required, StringLength(256, MinimumLength = 12)] public string TemporaryPassword { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int OrganizationRoleId { get; init; }
}
public sealed record StaffResponse(Guid MembershipId, string FullName, string Email, string? PhoneNumber,
    int OrganizationRoleId, string RoleName, bool IsActive, bool IsFirstLogin, bool IsOwner);
public sealed record StaffRoleResponse(int OrganizationRoleId, string Name);
public sealed record ChangeStaffRoleRequest([property: Range(1, int.MaxValue)] int OrganizationRoleId);
public sealed record ChangeStaffAccessRequest(bool IsActive);
public sealed class ChangeOrganizerPasswordRequest
{
    [Required, StringLength(256)] public string CurrentPassword { get; init; } = string.Empty;
    [Required, StringLength(256, MinimumLength = 12)] public string NewPassword { get; init; } = string.Empty;
}
public interface IOrganizationStaffService
{
    Task<StaffResponse[]> ListAsync(Guid ownerId, CancellationToken ct);
    Task<StaffRoleResponse[]> RolesAsync(Guid ownerId, CancellationToken ct);
    Task<StaffResponse> CreateAsync(Guid ownerId, CreateStaffRequest request, CancellationToken ct);
    Task SetRoleAsync(Guid ownerId, Guid membershipId, int roleId, CancellationToken ct);
    Task SetAccessAsync(Guid ownerId, Guid membershipId, bool active, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangeOrganizerPasswordRequest request, CancellationToken ct);
}