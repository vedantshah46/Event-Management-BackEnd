namespace EventProject.Modules.Organizations.Core.Domain.Entities;

public sealed class EventOrganizationStatusMaster
{
    public OrganizationStatus EventOrganizationStatusId { get; set; }
    public string EventOrganizationStatus { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}