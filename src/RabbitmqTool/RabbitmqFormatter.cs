using EasyNetQ.Management.Client.Model;

namespace RabbitmqTool
{
    public static class RabbitmqFormatter
    {
        public static string GetTitle(this Vhost vhost) => $"'{vhost.Name}'";
        public static string GetTitle(this Exchange exchange) => GetTitle(exchange.Vhost, exchange.Name);
        public static string GetTitle(this Queue queue) => GetTitle(queue.Vhost, queue.Name);
        public static string GetTitle(this Binding binding) => $"{GetTitle(binding.Vhost, binding.Source)} exchange -> {GetTitle(binding.Vhost, binding.Destination)} {binding.DestinationType}";
        public static string GetTitle(string vhost, string element) => $"'{vhost?.Trim('/')}/{element}'";
    }
}