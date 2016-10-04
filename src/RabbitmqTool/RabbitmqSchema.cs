using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using log4net;

namespace RabbitmqTool
{
    public sealed class RabbitmqSchema
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public RabbitmqSchema()
        {
            VHosts = new List<Vhost>();
            Exchanges = new List<Exchange>();
            Queues = new List<Queue>();
            Bindings = new List<Binding>();
        }

        public List<Vhost> VHosts { get; set; }
        public List<Exchange> Exchanges { get; set; }
        public List<Queue> Queues { get; set; }
        public List<Binding> Bindings { get; set; }

        public void Restore(IManagementClient client)
        {
            RestoreVHosts(client);
            RestoreExchanges(client);
            RestoreQueues(client);
            RestoreBindings(client);
        }

        private void RestoreVHosts(IManagementClient client)
        {
            foreach (var vhost in VHosts.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                try
                {
                    try
                    {
                        var aliveVHost = client.GetVhost(vhost.Name);
                        if (aliveVHost != null)
                        {
                            Log.Debug($"{vhost.GetTitle()} vhost is alive.");
                        }
                    }
                    catch (UnexpectedHttpStatusCodeException)
                    {
                        client.CreateVirtualHost(vhost.Name);
                        Log.Info($"{vhost.GetTitle()} vhost is created.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not handle the {vhost.GetTitle()} vhost: {ex.Message}", ex);
                }
            }
        }

        private void RestoreExchanges(IManagementClient client)
        {
            foreach (var exchange in Exchanges.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                try
                {
                    var vhost = new Vhost { Name = exchange.Vhost };
                    try
                    {
                        var aliveExchange = client.GetExchange(exchange.Name, vhost);
                        if (aliveExchange != null)
                        {
                            Log.Debug($"{exchange.GetTitle()} exchange is alive.");
                        }
                    }
                    catch (UnexpectedHttpStatusCodeException)
                    {
                        var exchangeInfo = new ExchangeInfo(
                            exchange.Name,
                            exchange.Type,
                            exchange.AutoDelete,
                            exchange.Durable,
                            exchange.Internal,
                            exchange.Arguments);
                        client.CreateExchange(exchangeInfo, vhost);
                        Log.Info($"{exchange.GetTitle()} exchange is created.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not handle the {exchange.GetTitle()} exchange: {ex.Message}", ex);
                }
            }
        }

        private void RestoreQueues(IManagementClient client)
        {
            foreach (var queue in Queues.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                try
                {
                    var vhost = new Vhost { Name = queue.Vhost };
                    try
                    {
                        var aliveQueue = client.GetQueue(queue.Name, vhost);
                        if (aliveQueue != null)
                        {
                            Log.Debug($"{queue.GetTitle()} queue is alive.");
                        }
                    }
                    catch (UnexpectedHttpStatusCodeException)
                    {
                        var queueInfo = new QueueInfo(queue.Name, queue.AutoDelete, queue.Durable, new InputArguments());
                        client.CreateQueue(queueInfo, vhost);
                        Log.Info($"{queue.GetTitle()} queue is created.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not handle the {queue.GetTitle()} queue: {ex.Message}", ex);
                }
            }
        }

        private void RestoreBindings(IManagementClient client)
        {
            var bindingComparer = RabbitmqBindingEqualityComparer.Instance;
            foreach (var binding in Bindings.Where(x => !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Destination)))
                try
                {
                    var srcExchange = new Exchange { Name = binding.Source, Vhost = binding.Vhost };
                    switch (binding.DestinationType)
                    {
                        case "queue":
                        {
                            var dstQueue = new Queue { Name = binding.Destination, Vhost = binding.Vhost };
                            var aliveBindings = client.GetBindingsForQueue(dstQueue).Where(x => bindingComparer.Equals(x, binding)).ToList();
                            if (aliveBindings.Count == 0)
                            {
                                var bindingInfo = new BindingInfo(binding.RoutingKey);
                                client.CreateBinding(srcExchange, dstQueue, bindingInfo);
                                Log.Info($"{binding.GetTitle()} binding is created.");
                            }
                            else
                            {
                                Log.Debug($"{binding.GetTitle()} binding is alive.");
                            }
                            break;
                        }
                        case "exchange":
                        {
                            var dstExchange = new Exchange { Name = binding.Destination, Vhost = binding.Vhost };
                            var aliveBindings = client.GetBindingsWithDestination(dstExchange).Where(x => bindingComparer.Equals(x, binding)).ToList();
                            if (aliveBindings.Count == 0)
                            {
                                var bindingInfo = new BindingInfo(binding.RoutingKey);
                                client.CreateBinding(srcExchange, dstExchange, bindingInfo);
                                Log.Info($"{binding.GetTitle()} binding is created.");
                            }
                            else
                            {
                                Log.Debug($"{binding.GetTitle()} binding is alive.");
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not handle the {binding.GetTitle()} binding: {ex.Message}", ex);
                }
        }

        public static bool IsAlive(IManagementClient client, string vhost)
        {
            var isAlive = client.IsAlive(new Vhost { Name = vhost });
            return isAlive;
        }

        public static RabbitmqSchema Fetch(IManagementClient client, string vhost)
        {
            var schema = new RabbitmqSchema();
            schema.VHosts.AddRange(
                client.GetVHosts()
                .Where(x => string.Equals(x.Name, vhost, StringComparison.OrdinalIgnoreCase)));

            schema.Exchanges.AddRange(
                client.GetExchanges()
                    .Where(x => !x.AutoDelete)
                    .Where(x => string.Equals(x.Vhost, vhost, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Vhost)
                    .ThenBy(x => x.Name));

            schema.Queues.AddRange(
                client.GetQueues()
                    .Where(x => !x.AutoDelete)
                    .Where(x => string.Equals(x.Vhost, vhost, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Vhost)
                    .ThenBy(x => x.Name));

            var exchanges = new HashSet<string>(schema.Exchanges.Select(x => x.Name));
            var queues = new HashSet<string>(schema.Queues.Select(x => x.Name));

            schema.Bindings.AddRange(
                client.GetBindings()
                    .Where(x => string.Equals(x.Vhost, vhost, StringComparison.OrdinalIgnoreCase))
                    .Where(x => exchanges.Contains(x.Source))
                    .Where(
                        x =>
                            (string.Equals(x.DestinationType, "queue", StringComparison.OrdinalIgnoreCase) && queues.Contains(x.Destination)) ||
                            (string.Equals(x.DestinationType, "exchange", StringComparison.OrdinalIgnoreCase) && exchanges.Contains(x.Destination)))
                    .OrderBy(x => x.Vhost)
                    .ThenBy(x => x.Source)
                    .ThenBy(x => x.Destination));

            return schema;
        }

        public static Diffs Diff(RabbitmqSchema oneSchema, RabbitmqSchema twoSchema)
        {
            var diffs = new Diffs();
            diffs.Exchanges = DiffR.Diff(oneSchema.Exchanges, twoSchema.Exchanges, x => x.GetTitle(),
                (a, b) =>
                {
                    return a != null && b != null && a.GetType() == b.GetType()
                        && string.Equals(a.Vhost, b.Vhost, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Type, b.Type, StringComparison.OrdinalIgnoreCase)
                        && b.Durable == a.Durable && a.AutoDelete == b.AutoDelete && a.Internal == b.Internal;
                });
            diffs.Queues = DiffR.Diff(oneSchema.Queues, twoSchema.Queues, x => x.GetTitle(),
                (a, b) =>
                {
                    return a != null && b != null && a.GetType() == b.GetType()
                        && string.Equals(a.Vhost, b.Vhost, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                        && b.Durable == a.Durable && a.AutoDelete == b.AutoDelete;
                });
            diffs.Bindings = DiffR.Diff(oneSchema.Bindings, twoSchema.Bindings, x => x.GetTitle(),
                (a, b) =>
                {
                    return a != null && b != null && a.GetType() == b.GetType()
                        && string.Equals(a.Vhost, b.Vhost, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Source, b.Source, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Destination, b.Destination, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.DestinationType, b.DestinationType, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.RoutingKey, b.RoutingKey, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.PropertiesKey, b.PropertiesKey, StringComparison.OrdinalIgnoreCase);
                });
            return diffs;
        }

        public sealed class Diffs
        {
            public DiffList<Exchange> Exchanges { get; set; }
            public DiffList<Queue> Queues { get; set; }
            public DiffList<Binding> Bindings { get; set; }
        }
    }
}