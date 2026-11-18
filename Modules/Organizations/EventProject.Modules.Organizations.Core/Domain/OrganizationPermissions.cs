namespace EventProject.Modules.Organizations.Core.Domain;

public static class OrganizationPermissions
{
    public const string ViewProfile = "organization.profile.read";
    public const string ManageStaff = "organization.staff.manage";
    public const string ManageEvents = "events.manage";
    public const string ManageStaffing = "staffing.manage";
    public const string RecordAttendance = "attendance.record";
    public const string ApproveAttendance = "attendance.approve";
    public const string ViewFinance = "finance.read";
    public const string ManagePaymentDetails = "organization.payment-details.manage";
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ViewProfile, ManageStaff, ManageEvents, ManageStaffing, RecordAttendance,
        ApproveAttendance, ViewFinance, ManagePaymentDetails
    };
}
