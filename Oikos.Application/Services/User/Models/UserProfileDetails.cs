namespace Oikos.Application.Services.User.Models;

public record UserProfileDetails(
    string? Name,
    string? RealName,
    string? AcademicTitle,
    string? Gender,
    string? Company,
    string? Email,
    string? PhoneNumber,
    string? CustomerNumber,
    string? Avatar);
