using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using EventProject.Modules.Organizations.Core.Domain.Entities;

namespace EventProject.Modules.Organizations.Core.Application.DTOs;

public sealed class RegisterOrganizationRequest
{
    [Display(Name = "Full name")]
    [Required(ErrorMessage = "Full name is required."), StringLength(150)]
    public string OwnerFullName { get; init; } = string.Empty;

    [Display(Name = "Login email")]
    [Required(ErrorMessage = "Login email is required."), EmailAddress(ErrorMessage = "Enter a valid login email address."), StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required."), StringLength(256, MinimumLength = 12)]
    [RegularExpression(@"(?s)^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, a number and a special character.")]
    public string Password { get; init; } = string.Empty;

    [Display(Name = "Confirm password")]
    [Required(ErrorMessage = "Confirm password is required."), StringLength(256, MinimumLength = 12)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Display(Name = "Company name")]
    [Required(ErrorMessage = "Company name is required."), StringLength(200)]
    public string OrganizationName { get; init; } = string.Empty;

    [Display(Name = "Company email")]
    [Required(ErrorMessage = "Company email is required."), EmailAddress(ErrorMessage = "Enter a valid company email address."), StringLength(256)]
    public string ContactEmail { get; init; } = string.Empty;

    [Display(Name = "Company phone")]
    [Required(ErrorMessage = "Company phone is required."), StringLength(20)]
    [RegularExpression(@"^\+?(?=(?:[^0-9]*[0-9]){7,15}[^0-9]*$)[0-9 ().-]+$",
        ErrorMessage = "Company phone must contain 7–15 digits, with an optional leading +, spaces, parentheses, dots or hyphens.")]
    public string ContactPhone { get; init; } = string.Empty;

    [Display(Name = "Company address")]
    [Required(ErrorMessage = "Company address is required."), StringLength(300)]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = "City is required."), StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required(ErrorMessage = "State is required."), StringLength(100)]
    public string State { get; init; } = string.Empty;

    [Display(Name = "Postal code")]
    [Required(ErrorMessage = "Postal code is required.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Postal code must contain exactly six digits.")]
    public string PostalCode { get; init; } = string.Empty;
}

public sealed class OrganizerLoginRequest
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(256)] public string Password { get; init; } = string.Empty;
}

public sealed class RejectOrganizationRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = string.Empty;
}

public sealed record OrganizationResponse(Guid Id, string Name, string ContactEmail, string ContactPhone,
    string Address, string City, string State, string PostalCode, [property: JsonConverter(typeof(JsonNumberEnumConverter<OrganizationStatus>))] OrganizationStatus EventOrganizationStatusId,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    string EventOrganizationStatusCode, string EventOrganizationStatusName);
public sealed record MembershipResponse(Guid Id, Guid OrganizationId, string Role, string[] Permissions, bool IsFirstLogin = false);
public sealed record RegistrationResponse(Guid OrganizationId, Guid UserId, [property: JsonConverter(typeof(JsonNumberEnumConverter<OrganizationStatus>))] OrganizationStatus EventOrganizationStatusId);
public sealed record OrganizerLoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc,
    OrganizationResponse Organization, MembershipResponse Membership, string TokenType = "Bearer");
public sealed record OrganizationAccess(OrganizationResponse Organization, MembershipResponse Membership);
public sealed record OrganizationPage(OrganizationResponse[] Items, int Page, int PageSize, int TotalCount);

public sealed record EventOrganizationStatusResponse([property: JsonConverter(typeof(JsonNumberEnumConverter<OrganizationStatus>))] OrganizationStatus Id, string Code, string Name, bool IsActive, int SortOrder);
