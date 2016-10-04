using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using EasyNetQ.Management.Client;
using log4net;
using log4net.Config;
using Newtonsoft.Json;

namespace RabbitmqTool
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            try
            {
                var cli = new CLI(args);

                cli.Register("schema", "is-alive",
                    new CLI.Command<RabbitmqConfig>(
                        config =>
                        {
                            var isAlive = RabbitmqSchema.IsAlive(config.CreateClient(), config.VHost);
                            Console.WriteLine(isAlive);
                        }));

                cli.Register("schema", "fetch",
                    new CLI.Command<RabbitmqConfig>(
                        config =>
                        {
                            var schema = RabbitmqSchema.Fetch(config.CreateClient(), config.VHost);
                            var json = JsonConvert.SerializeObject(schema, Formatting.Indented);
                            Console.WriteLine(json);
                        }));

                cli.Register("schema", "restore",
                    new CLI.Command<RabbitmqConfig>(
                        config =>
                        {
                            if (Console.IsInputRedirected)
                            {
                                var json = new StreamReader(Console.OpenStandardInput()).ReadToEnd();
                                var schema = JsonConvert.DeserializeObject<RabbitmqSchema>(json);
                                schema.Restore(config.CreateClient());
                            }
                            else
                            {
                                var commandLine = $"{Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)} {string.Join(" ", args)}";
                                Console.WriteLine($"Please try the following: $> type schema.json | {commandLine}");
                            }
                        }));

                cli.Register("schema", "diff",
                    new CLI.Command<RabbitmqDiffConfig>(
                        config =>
                        {
                            var left = JsonConvert.DeserializeObject<RabbitmqSchema>(File.ReadAllText(config.Left));
                            var right = JsonConvert.DeserializeObject<RabbitmqSchema>(File.ReadAllText(config.Right));
                            var diffs = RabbitmqSchema.Diff(left, right);

                            Console.WriteLine($"Exchanges: {diffs.Exchanges.Count}");
                            if (diffs.Exchanges.Count > 0)
                                foreach (var exchange in diffs.Exchanges)
                                    Console.WriteLine($"type: {exchange.Type}, left: {exchange.Left.GetTitle()}, right: {exchange.Right.GetTitle()}");

                            Console.WriteLine($"Queues: {diffs.Queues.Count}");
                            if (diffs.Queues.Count > 0)
                                foreach (var queue in diffs.Queues)
                                    Console.WriteLine($"Queue type: {queue.Type}, left: {queue.Left.GetTitle()}, right: {queue.Right.GetTitle()}");

                            Console.WriteLine($"Bindings: {diffs.Bindings.Count}");
                            if (diffs.Bindings.Count > 0)
                                foreach (var binding in diffs.Bindings)
                                    Console.WriteLine($"Binding type: {binding.Type}, left: {binding.Left.GetTitle()}, right: {binding.Right.GetTitle()}");

                            Console.WriteLine("Done.");
                        }));

                cli.Register("masstransit", "validate",
                    new CLI.Command<RabbitmqConfig>(
                        config =>
                        {
                            var schema = RabbitmqSchema.Fetch(config.CreateClient(), config.VHost);
                            MasstransitSchema.Validate(schema);
                        }));

                if (cli.Parser.ParseArguments(cli.Arguments, cli.Options))
                {
                    if (cli.Options.Debug)
                    {
                        Debugger.Launch();
                        Debugger.Break();
                    }

                    var command = cli.Resolve(cli.Options.Subject, cli.Options.Command);
                    command.Handle();
                }
                else
                {
                    Console.WriteLine(cli.GetUsage(cli.Options));
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("The program could not handle the command.", ex);
            }
        }

        public class RabbitmqConfig
        {
            public RabbitmqConfig()
            {
                Host = Environment.GetEnvironmentVariable("RABBITMQTOOL_HOST") ?? @"http://localhost";
                Port = Parse<int>(Environment.GetEnvironmentVariable("RABBITMQTOOL_PORT") ?? "15672");
                VHost = Environment.GetEnvironmentVariable("RABBITMQTOOL_VHOST") ?? "/";
                UserName = Environment.GetEnvironmentVariable("RABBITMQTOOL_USERNAME") ?? "guest";
                Password = Environment.GetEnvironmentVariable("RABBITMQTOOL_PASSWORD") ?? "guest";
            }

            [ParserState]
            public IParserState State { get; set; }

            [Option("host", Required = false, MetaValue = @"http://localhost")]
            public string Host { get; set; }

            [Option("port", Required = false, MetaValue = "15672")]
            public int Port { get; set; }

            [Option("vhost", Required = false, MetaValue = "/")]
            public string VHost { get; set; }

            [Option('u', "username", Required = false, MetaValue = "guest")]
            public string UserName { get; set; }

            [Option('p', "password", Required = false, MetaValue = "guest")]
            public string Password { get; set; }

            public static T Parse<T>(string option) => (T) (TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(option) ?? default(T));

            public ManagementClient CreateClient() => new ManagementClient(Host, UserName, Password, Port);

            public override string ToString() => $"{nameof(Host)}: {Host}, {nameof(Port)}: {Port}, {nameof(VHost)}: {VHost}";
        }

        public class RabbitmqDiffConfig
        {
            [ParserState]
            public IParserState State { get; set; }

            [Option("left", Required = true)]
            public string Left { get; set; }

            [Option("right", Required = true)]
            public string Right { get; set; }

            public override string ToString() => $"{nameof(Left)}: {Left}, {nameof(Right)}: {Right}";
        }
    }
}