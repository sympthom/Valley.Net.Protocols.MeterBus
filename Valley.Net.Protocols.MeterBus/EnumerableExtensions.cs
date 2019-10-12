using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> GroupAdjacentBy<T>(this IEnumerable<T> source, Func<T, bool> separator)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    var list = new List<T> { e.Current };

                    while (e.MoveNext())
                    {
                        if (!separator(e.Current))
                        {
                            list.Add(e.Current);
                        }
                        else
                        {
                            yield return list;

                            list = new List<T> { e.Current };
                        }
                    }

                    yield return list;
                }
            }
        }
    }
}
