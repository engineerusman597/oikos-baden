using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oikos.Domain.Entities.Rbac;

[Comment("Role")]
public class Role
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Role name")]
    public string? Name { get; set; }

    [Comment("Is enabled")]
    public bool IsEnabled { get; set; }

    [Comment("Is deleted")]
    public bool IsDeleted { get; set; }
}
