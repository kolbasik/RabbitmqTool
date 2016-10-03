using System;
using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;

namespace RabbitmqTool
{
    internal sealed class RabbitmqBindingEqualityComparer : IEqualityComparer<Binding>
    {
        public static readonly IEqualityComparer<Binding> Instance = new RabbitmqBindingEqualityComparer();

        public bool Equals(Binding x, Binding y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.Source, y.Source, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Vhost, y.Vhost, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Destination, y.Destination, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.DestinationType, y.DestinationType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.RoutingKey, y.RoutingKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.PropertiesKey, y.PropertiesKey, StringComparison.OrdinalIgnoreCase) &&
                RabbitmqArgumentsEqualityComparer.Instance.Equals(x.Arguments, y.Arguments);
        }

        public int GetHashCode(Binding obj)
        {
            unchecked
            {
                var hashCode = (obj.Source != null ? obj.Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Vhost != null ? obj.Vhost.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Destination != null ? obj.Destination.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.DestinationType != null ? obj.DestinationType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.RoutingKey != null ? obj.RoutingKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Arguments != null ? obj.Arguments.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.PropertiesKey != null ? obj.PropertiesKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}