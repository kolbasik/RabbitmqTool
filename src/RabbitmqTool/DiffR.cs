using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RabbitmqTool
{
    public static class DiffR
    {
        public static DiffList<TValue> Diff<TKey, TValue>(IEnumerable<TValue> left, IEnumerable<TValue> right, Func<TValue, TKey> getKey, Func<TValue, TValue, bool> equal)
        {
            var oneDic = left.ToLookup(getKey, x => x).ToDictionary(x => x.Key, x => x.First());
            var twoDic = right.ToLookup(getKey, x => x).ToDictionary(x => x.Key, x => x.First());

            return Diff(oneDic, twoDic, equal);
        }

        public static DiffList<TValue> Diff<TKey, TValue>(IDictionary<TKey, TValue> left, IDictionary<TKey, TValue> right, Func<TValue, TValue, bool> equal)
        {
            var diffs = new DiffList<TValue>();
            var uniques = new HashSet<TKey>();
            foreach (var kvp in left)
            {
                uniques.Add(kvp.Key);

                TValue one = kvp.Value, two;
                if (right.TryGetValue(kvp.Key, out two))
                {
                    if (!equal(one, two))
                    {
                        diffs.Append(new DiffItem<TValue>(DiffType.Changed, one, two));
                    }
                }
                else
                {
                    diffs.Append(new DiffItem<TValue>(DiffType.Removed, one, two));
                }
            }
            foreach (var kvp in right)
            {
                if (!uniques.Contains(kvp.Key))
                {
                    diffs.Append(new DiffItem<TValue>(DiffType.Added, default(TValue), kvp.Value));
                }
            }
            return diffs;
        }
    }

    public sealed class DiffList<T> : IEnumerable<DiffItem<T>>
    {
        private readonly List<DiffItem<T>> items;

        public DiffList()
        {
            items = new List<DiffItem<T>>();
        }

        public int Count => items.Count;

        public void Append(DiffItem<T> item)
        {
            items.Add(item);
        }

        public IEnumerator<DiffItem<T>> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    public sealed class DiffItem<T>
    {
        public DiffItem(DiffType type, T left, T right)
        {
            Type = type;
            Left = left;
            Right = right;
        }

        public DiffType Type { get; }
        public T Left { get; set; }
        public T Right { get; set; }
    }

    public enum DiffType
    {
        None,
        Added,
        Removed,
        Changed
    }
}