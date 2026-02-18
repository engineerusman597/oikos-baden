namespace Oikos.Application.Services.Role.Models;

public class RoleSearchCriteria
{
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
