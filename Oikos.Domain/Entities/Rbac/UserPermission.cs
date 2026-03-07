using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Rbac;

[Comment("Employee permission entry")]
public class UserPermission
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User id")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Comment("Permission key (see EmployeePermissions constants)")]
    public string Permission { get; set; } = null!;
}
