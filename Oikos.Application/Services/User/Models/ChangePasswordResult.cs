namespace Oikos.Application.Services.User.Models;

public enum ChangePasswordError
{
    None = 0,
    UserNotFound = 1,
    InvalidCurrentPassword = 2
}

public record ChangePasswordResult(bool Success, ChangePasswordError Error)
{
    public static ChangePasswordResult Ok() => new(true, ChangePasswordError.None);

    public static ChangePasswordResult Fail(ChangePasswordError error) => new(false, error);
}
