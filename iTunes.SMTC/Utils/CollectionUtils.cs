using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace iTunes.SMTC.Utils
{
    public static class CollectionUtils
    {
        public static T Random<T>(this IEnumerable<T> e)
        {
            return e.ElementAt(new Random().Next(0, e.Count()));
        }

        public static T RandomOrDefault<T>(this IEnumerable<T> e)
        {
            return e.ElementAtOrDefault(new Random().Next(0, e.Count()));
        }

        public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> e, Func<T, bool> predicate)
        {
            return e.Where(it => !predicate(it));
        }

        public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
        {
            foreach (T item in e)
            {
                action(item);
            }
        }

        public static void ForEach(this IEnumerable e, Action<object> action)
        {
            foreach (object item in e)
            {
                action(item);
            }
        }

        public static int IndexOf(this IEnumerable e, object value)
        {
            int index = 0;

            foreach (object item in e)
            {
                if (Equals(item, value))
                    return index;

                index++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> e, T value)
        {
            int index = 0;

            var comparer = EqualityComparer<T>.Default;

            foreach (T item in e)
            {
                if (comparer.Equals(item, value))
                    return index;

                index++;
            }

            return -1;
        }

        public static void ForEachIndexed<T>(this IEnumerable<T> e, Action<int, T> action)
        {
            var i = 0;
            foreach (T item in e)
            {
                action(i, item);
                i++;
            }
        }

        public static void ForEachIndexed(this IEnumerable e, Action<int, object> action)
        {
            var i = 0;
            foreach (object item in e)
            {
                action(i, item);
                i++;
            }
        }

#nullable enable
        public static object? GetValueOrDefault(this IPropertySet dictionary, string key) =>
            dictionary.GetValueOrDefault(key, default!);

        public static object GetValueOrDefault(this IPropertySet dictionary, string key, object defaultValue)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            return dictionary.TryGetValue(key, out object? value) ? value : defaultValue;
        }
#nullable restore

        public static void RemoveRange<T>(this IList<T> list, int start, int count)
        {
            var end = start + count;

            if (start < 0 || count < 0 || end > list.Count)
            {
                throw new IndexOutOfRangeException();
            }

            for (int i = end - 1; i >= start; i--)
            {
                list.RemoveAt(i);
            }
        }

        public static string ToString(this IEnumerable list)
        {
            return list.Cast<object>().ToString<object>();
        }

        public static string ToString(this IEnumerable list, Func<object, string> toStringConv)
        {
            return list.Cast<object>().ToString(toStringConv);
        }

        public static string ToString<T>(this IEnumerable<T> list)
        {
            return $"[{string.Join(',', list.Select(x => x.ToString()))}]";
        }

        public static string ToString<T>(this IEnumerable<T> list, Func<T, string> toStringConv)
        {
            return $"[{string.Join(',', list.Select(toStringConv))}]";
        }

        public static bool IsEmpty(this IEnumerable enumerable)
        {
            if (enumerable is ICollection collection)
            {
                return collection.Count == 0;
            }

            var e = enumerable.GetEnumerator();
            return !e.MoveNext();
        }
    }
}
