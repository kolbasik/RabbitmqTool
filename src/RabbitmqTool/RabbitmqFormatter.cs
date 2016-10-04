using EasyNetQ.Management.Client.Model;

namespace RabbitmqTool
{
    public static class RabbitmqFormatter
    {
        public static string GetTitle(this Vhost vhost) => vhost != null ? $"'{vhost.Name}'" : string.Empty;
        public static string GetTitle(this Exchange exchange) => exchange != null ? GetTitle(exchange.Vhost, exchange.Name) : string.Empty;
        public static string GetTitle(this Queue queue) => queue != null ? GetTitle(queue.Vhost, queue.Name) : string.Empty;
        public static string GetTitle(this Binding binding) => binding != null ? $"{GetTitle(binding.Vhost, binding.Source)} exchange -> {GetTitle(binding.Vhost, binding.Destination)} {binding.DestinationType}" : string.Empty;
        public static string GetTitle(string vhost, string element) => $"'{vhost?.Trim('/')}/{element}'";
    }
}