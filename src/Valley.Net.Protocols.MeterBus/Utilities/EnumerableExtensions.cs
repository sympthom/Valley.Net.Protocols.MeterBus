namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Enumerable extensions for grouping adjacent elements.
/// </summary>
internal static class EnumerableExtensions
{
    public static IEnumerable<IReadOnlyList<T>> GroupAdjacentBy<T>(this IEnumerable<T> source, Func<T, bool> isSeparator)
    {
        var group = new List<T>();
        foreach (var item in source)
        {
            if (isSeparator(item) && group.Count > 0)
            {
                yield return group;
                group = [];
            }
            group.Add(item);
        }
        if (group.Count > 0)
            yield return group;
    }
}
