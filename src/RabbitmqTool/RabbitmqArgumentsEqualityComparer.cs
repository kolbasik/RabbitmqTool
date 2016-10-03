using System;
using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;

namespace RabbitmqTool
{
    internal sealed class RabbitmqArgumentsEqualityComparer : IEqualityComparer<Arguments>
    {
        public static readonly IEqualityComparer<Arguments> Instance = new RabbitmqArgumentsEqualityComparer();

        public bool Equals(Arguments x, Arguments y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            if (x.Count != y.Count)
                return false;

            foreach (var kvp in x)
            {
                string value;
                if (y.TryGetValue(kvp.Key, out value) && string.Equals(kvp.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public int GetHashCode(Arguments obj)
        {
            unchecked
            {
                var hashCode = obj != null ? obj.GetHashCode() : 0;
                return hashCode;
            }
        }
    }
}