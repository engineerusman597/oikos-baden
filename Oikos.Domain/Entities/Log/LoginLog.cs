using Microsoft.EntityFrameworkCore;
using Oikos.Domain.Attributes;

namespace Oikos.Domain.Entities.Log;

[Comment("Login log")]
[Index(nameof(UserName))]
[Index(nameof(Time))]
[IgnoreAudit]
public class LoginLog
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Login name")]
    public string UserName { get; set; } = null!;

    [Comment("Login time")]
    public DateTime Time { get; set; }

    [Comment("Login client")]
    public string? Agent { get; set; }

    [Comment("Login IP")]
    public string? Ip { get; set; }

    [Comment("Was successful")]
    public bool IsSuccessd { get; set; }
}
