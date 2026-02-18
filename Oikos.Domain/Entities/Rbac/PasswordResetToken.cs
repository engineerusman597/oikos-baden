using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Rbac;

[Comment("Password reset tokens")]
public class PasswordResetToken
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User identifier")]
    public int UserId { get; set; }

    [Comment("Token hash")]
    public string TokenHash { get; set; } = null!;

    [Comment("Expiration time (UTC)")]
    public DateTime ExpiresAt { get; set; }

    [Comment("Creation time (UTC)")]
    public DateTime CreatedAt { get; set; }

    [Comment("Consumption time (UTC)")]
    public DateTime? ConsumedAt { get; set; }

    public User? User { get; set; }
}
