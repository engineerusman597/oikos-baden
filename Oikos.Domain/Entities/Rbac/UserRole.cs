using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Rbac;

[Comment("User role")]
public class UserRole
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User ID")]
    public int UserId { get; set; }

    [Comment("Role ID")]
    public int RoleId { get; set; }
}
