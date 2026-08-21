using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentPartTime.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}

public class RegisterStudentViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name must not exceed 100 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(150, ErrorMessage = "Email must not exceed 150 characters.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    public string? University { get; set; }

    public string? Major { get; set; }

    public int? GraduationYear { get; set; }
}

public class RegisterEmployerViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name must not exceed 100 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(150, ErrorMessage = "Email must not exceed 150 characters.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Position is required.")]
    [StringLength(100, ErrorMessage = "Position must not exceed 100 characters.")]
    public string Position { get; set; } = null!;

    // Company Fields
    [Required(ErrorMessage = "Company Name is required.")]
    [StringLength(200, ErrorMessage = "Company Name must not exceed 200 characters.")]
    public string CompanyName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    [Url(ErrorMessage = "Invalid website URL.")]
    public string? Website { get; set; }

    [EmailAddress(ErrorMessage = "Invalid company email format.")]
    public string? CompanyEmail { get; set; }

    [Phone(ErrorMessage = "Invalid company phone number.")]
    public string? CompanyPhone { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    [Required(ErrorMessage = "Province is required.")]
    public int ProvinceId { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = null!;
}

public class ResetPasswordViewModel
{
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "New Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirm New Password is required.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = null!;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current Password is required.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "New Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirm New Password is required.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = null!;
}

public class ProfileViewModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Full Name is required.")]
    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }

    // Student specific
    public string? University { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? Experience { get; set; }
    public string? SkillSummary { get; set; }

    // Employer specific
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyDescription { get; set; }
    public string? CompanyAddress { get; set; }
    public int? CompanyProvinceId { get; set; }
}

// =====================================================================
// FEATURE 3.4 - CATEGORY MANAGEMENT
// (Categories / Job Types / Provinces)
// =====================================================================

public class CategoryViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên danh mục phải từ 2 đến 100 ký tự.")]
    public string CategoryName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }
}

public class JobTypeViewModel
{
    public int JobTypeId { get; set; }

    [Required(ErrorMessage = "Tên loại công việc là bắt buộc.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên loại công việc phải từ 2 đến 100 ký tự.")]
    public string TypeName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }
}

public class ProvinceViewModel
{
    public int ProvinceId { get; set; }

    [Required(ErrorMessage = "Tên tỉnh/thành là bắt buộc.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên tỉnh/thành phải từ 2 đến 100 ký tự.")]
    public string ProvinceName { get; set; } = null!;
}

/// <summary>
/// Shared model for the reusable _Pagination.cshtml partial.
/// Action refers to the controller action to link back to (e.g. "Categories").
/// Search is round-tripped in the querystring so filters survive page changes.
/// </summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public string Action { get; set; } = null!;
    public string Controller { get; set; } = "Admin";
}

// =====================================================================
// FEATURE 3.5 - SYSTEM ADMINISTRATION
// (Account Management / Company Management / Job Moderation /
//  Notifications / Audit Logs)
// =====================================================================

/// <summary>
/// Read-only detail view for a single user (Admin > Account Management).
/// Role-specific fields are populated only when the user has that role.
/// </summary>
public class UserDetailViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();

    // Student specific
    public string? University { get; set; }
    public string? Major { get; set; }

    // Employer specific
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyStatus { get; set; }
}

/// <summary>
/// Read-only detail view for a company (Admin > Company Management),
/// with a rejection reason field used only when submitting a Reject action.
/// </summary>
public class CompanyDetailViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ProvinceName { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int EmployerCount { get; set; }
}

/// <summary>
/// Binds the filter form on Admin > Audit Logs. All fields optional.
/// </summary>
public class AuditLogFilterViewModel
{
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

// =====================================================================
// FEATURE 3.6 - DASHBOARD & STATISTICS
// =====================================================================

/// <summary>
/// Shared label/value pair used to feed Chart.js data across all
/// statistics sub-modules (bar/pie/doughnut charts and the tables
/// displayed alongside them).
/// </summary>
public class ChartDataPointViewModel
{
    public string Label { get; set; } = null!;
    public int Value { get; set; }
}

public class MonthlyCountViewModel
{
    public string MonthLabel { get; set; } = null!;
    public int Count { get; set; }
}

public class CompanyJobCountViewModel
{
    public string CompanyName { get; set; } = null!;
    public int JobCount { get; set; }
}

public class JobStatisticsViewModel
{
    public List<ChartDataPointViewModel> ByCategory { get; set; } = new();
    public List<ChartDataPointViewModel> ByProvince { get; set; } = new();
    public List<ChartDataPointViewModel> ByJobType { get; set; } = new();
    public List<ChartDataPointViewModel> ByStatus { get; set; } = new();
}

public class ApplicationStatisticsViewModel
{
    public int TotalApplications { get; set; }
    public List<ChartDataPointViewModel> ByStatus { get; set; } = new();
    public List<MonthlyCountViewModel> ByMonth { get; set; } = new();
}

public class CompanyStatisticsViewModel
{
    public int TotalCompanies { get; set; }
    public List<ChartDataPointViewModel> ByStatus { get; set; } = new();
    public List<CompanyJobCountViewModel> TopCompanies { get; set; } = new();
}

public class StudentStatisticsViewModel
{
    public int TotalStudents { get; set; }
    public List<ChartDataPointViewModel> ByMajor { get; set; } = new();
    public List<ChartDataPointViewModel> ByUniversity { get; set; } = new();
    public List<ChartDataPointViewModel> TopAppliedCategories { get; set; } = new();
}
