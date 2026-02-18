using Oikos.Application.Services.User.Models;

namespace Oikos.Application.Services.User;

public interface IUserManagementService
{
    Task<PaginatedResult<UserDto>> GetUsersAsync(UserSearchCriteria criteria);
    Task<UserDetailDto?> GetUserDetailAsync(int userId);
    Task<bool> CreateUserAsync(CreateUserRequest request);
    Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(int userId);
    Task<bool> ChangeUserStatusAsync(int userId, bool isEnabled);
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
    Task<bool> ValidateCustomerNumberAsync(string customerNumber, int? excludeUserId = null);
    Task<CurrentUserDto?> GetUserAsync(int userId);
}
