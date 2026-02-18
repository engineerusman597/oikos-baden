using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Setting;

[Comment("System setting")]
public class Setting
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Key")]
    public string Key { get; set; } = null!;

    [Comment("Value")]
    public string Value { get; set; } = null!;
}
