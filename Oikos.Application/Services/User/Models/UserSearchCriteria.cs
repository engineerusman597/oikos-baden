namespace Oikos.Application.Services.User.Models;

public class UserSearchCriteria
{
    public string? SearchText { get; set; }
    public string? SearchRealName { get; set; }
    public int? RoleId { get; set; }
    public List<int>? RoleIds { get; set; }
    public int? ExcludeRoleId { get; set; }
    public int? PartnerId { get; set; }
    public bool? HasActiveSubscription { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
