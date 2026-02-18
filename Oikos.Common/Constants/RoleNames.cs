namespace Oikos.Common.Constants;

public enum RoleNames
{
    Admin,
    User,
    User_Bonix
}

public static class RoleNamesExtensions
{
    /// <summary>
    /// Converts the RoleNames enum to its string representation for database queries
    /// </summary>
    public static string ToRoleName(this RoleNames role)
    {
        return role switch
        {
            RoleNames.Admin => "Admin",
            RoleNames.User => "User",
            RoleNames.User_Bonix => "User_Bonix",
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown role: {role}")
        };
    }
    
    /// <summary>
    /// Converts a role name string to its corresponding RoleNames enum value
    /// </summary>
    public static RoleNames? FromRoleName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) 
            return null;
        
        return roleName switch
        {
            "Admin" => RoleNames.Admin,
            "User" => RoleNames.User,
            "User_Bonix" => RoleNames.User_Bonix,
            _ => null
        };
    }
}
