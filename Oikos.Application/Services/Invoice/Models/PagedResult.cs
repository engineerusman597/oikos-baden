namespace Oikos.Application.Services.Invoice.Models;

public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => TotalCount > 0 
        ? (int)Math.Ceiling(TotalCount / (double)PageSize) 
        : 1;
}
