using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Setting;

[Comment("User setting")]
public class UserSetting
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User ID")]
    public int UserId { get; set; }

    [Comment("Key")]
    public string Key { get; set; } = null!;

    [Comment("Value")]
    public string Value { get; set; } = null!;
}
