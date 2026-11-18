using System.Text.Json.Serialization;
using Shared.Core.Exceptions;

namespace EventProject.Modules.Organizations.Core.Domain.Entities;

[JsonConverter(typeof(JsonNumberEnumConverter<OrganizationStatus>))]
public enum OrganizationStatus { PendingApproval = 1, Approved = 2, Rejected = 3, Suspended = 4 }

public sealed class EventOrganization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public OrganizationStatus EventOrganizationStatusId { get; private set; } = OrganizationStatus.PendingApproval;
    public EventOrganizationStatusMaster EventOrganizationStatus { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public void Review(OrganizationStatus outcome, DateTimeOffset now)
    {
        if (EventOrganizationStatusId != OrganizationStatus.PendingApproval)
            throw new BusinessException(BusinessError.Conflict, "Only pending organizations can be reviewed.");
        if (outcome is not (OrganizationStatus.Approved or OrganizationStatus.Rejected))
            throw new BusinessException(BusinessError.Validation, "Invalid review outcome.");
        EventOrganizationStatusId = outcome;
        UpdatedAtUtc = now;
    }
}

public sealed class OrganizationMembership
{
    public Guid Id { get; set; }
    public Guid EventOrganizationId { get; set; }
    public Guid UserId { get; set; }
    public int OrganizationRoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAtUtc { get; set; }
}

public sealed class OrganizationRole
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class OrganizationRolePermission
{
    public int OrganizationRoleId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
}

public sealed class OrganizationPaymentDetails
{
    public Guid EventOrganizationId { get; set; }
    public string UpiId { get; set; } = string.Empty;
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OrganizationStatusHistory
{
    public Guid Id { get; set; }
    public Guid EventOrganizationId { get; set; }
    public OrganizationStatus? PreviousEventOrganizationStatusId { get; set; }
    public OrganizationStatus NewEventOrganizationStatusId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string? Reason { get; set; }
}
