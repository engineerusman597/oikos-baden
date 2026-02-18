namespace Oikos.Common.Extensions;

public static class QueryableExtension
{
    public static IList<T> AndIf<T>(this IList<T> queryable, bool condition, Func<T, bool> predicate) where T : class
        => condition ? queryable.Where(predicate).ToList() : queryable;
}