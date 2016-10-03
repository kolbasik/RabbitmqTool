using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
                if (cli.Parser.ParseArguments(cli.Arguments, cli.Options))
                {
                    if (cli.Options.Verbose)
                        Console.WriteLine($"Options: {cli.Options}");

                    switch (cli.Options.Subject)
                    {
                        case "schema":
                        {
                            switch (cli.Options.Command)
                            {
                                case "is-alive":
                                {
                                    var rabbitConfig = RabbitmqConfig.Parse(cli.Parser, cli.Arguments);
                                    if (cli.Options.Help)
                                    {
                                        Console.WriteLine(CLI.GetUsage(rabbitConfig));
                                    }
                                    else
                                    {
                                        if (cli.Options.Verbose)
                                            Console.WriteLine(rabbitConfig);
                                        var isAlive = RabbitmqSchema.IsAlive(rabbitConfig.CreateClient(), rabbitConfig.VHost);
                                        Console.WriteLine(isAlive);
                                    }
                                    break;
                                }

                                case "fetch":
                                {
                                    var rabbitConfig = RabbitmqConfig.Parse(cli.Parser, cli.Arguments);
                                    if (cli.Options.Help)
                                    {
                                        Console.WriteLine(CLI.GetUsage(rabbitConfig));
                                    }
                                    else
                                    {
                                        if (cli.Options.Verbose)
                                            Console.WriteLine(rabbitConfig);
                                        var schema = RabbitmqSchema.Fetch(rabbitConfig.CreateClient(), rabbitConfig.VHost);
                                        var json = JsonConvert.SerializeObject(schema, Formatting.Indented);
                                        Console.WriteLine(json);
                                    }
                                    break;
                                }

                                case "restore":
                                {
                                    var rabbitConfig = RabbitmqConfig.Parse(cli.Parser, cli.Arguments);
                                    if (cli.Options.Help)
                                    {
                                        Console.WriteLine(CLI.GetUsage(rabbitConfig));
                                    }
                                    else
                                    {
                                        if (cli.Options.Verbose)
                                            Console.WriteLine(rabbitConfig);
                                        if (Console.IsInputRedirected)
                                        {
                                            var json = new StreamReader(Console.OpenStandardInput()).ReadToEnd();
                                            var schema = JsonConvert.DeserializeObject<RabbitmqSchema>(json);
                                            schema.Restore(rabbitConfig.CreateClient());
                                        }
                                        else
                                        {
                                            var commandLine = $"{Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)} {string.Join(" ", args)}";
                                            Console.WriteLine($"Please try the following: $> type schema.json | {commandLine}");
                                        }
                                    }
                                    break;
                                }

                                case "diff":
                                {
                                    var diffConfig = new RabbitmqDiffConfig();
                                    if (cli.Options.Help || !cli.Parser.ParseArguments(cli.Arguments, diffConfig))
                                    {
                                        Console.WriteLine(CLI.GetUsage(diffConfig));
                                    }
                                    else
                                    {
                                        if (cli.Options.Verbose)
                                            Console.WriteLine(diffConfig);

                                        var left = JsonConvert.DeserializeObject<RabbitmqSchema>(File.ReadAllText(diffConfig.Left));
                                        var right = JsonConvert.DeserializeObject<RabbitmqSchema>(File.ReadAllText(diffConfig.Right));
                                        var diffs = RabbitmqSchema.Diff(left, right);

                                        Console.WriteLine($"Exchanges: {diffs.Exchanges.Count}");
                                        if (diffs.Exchanges.Count > 0)
                                            foreach (var exchange in diffs.Exchanges)
                                                Console.WriteLine(
                                                    $"type: {exchange.Type}, left: {exchange.Left.GetTitle()}, right: {exchange.Right.GetTitle()}");
                                        Console.WriteLine($"Queues: {diffs.Queues.Count}");
                                        if (diffs.Queues.Count > 0)
                                            foreach (var queue in diffs.Queues)
                                                Console.WriteLine($"Queue type: {queue.Type}, left: {queue.Left.GetTitle()}, right: {queue.Right.GetTitle()}");
                                        Console.WriteLine($"Bindings: {diffs.Bindings.Count}");
                                        if (diffs.Bindings.Count > 0)
                                            foreach (var binding in diffs.Bindings)
                                                Console.WriteLine(
                                                    $"Binding type: {binding.Type}, left: {binding.Left.GetTitle()}, right: {binding.Right.GetTitle()}");
                                        Console.WriteLine("Done.");
                                    }
                                    break;
                                }
                            }
                            break;
                        }

                        case "masstransit":
                        {
                            switch (cli.Options.Command)
                            {
                                case "validate":
                                {
                                    var rabbitConfig = RabbitmqConfig.Parse(cli.Parser, cli.Arguments);
                                    if (cli.Options.Help)
                                    {
                                        Console.WriteLine(CLI.GetUsage(rabbitConfig));
                                    }
                                    else
                                    {
                                        if (cli.Options.Verbose)
                                            Console.WriteLine(rabbitConfig);
                                        var schema = RabbitmqSchema.Fetch(rabbitConfig.CreateClient(), rabbitConfig.VHost);
                                        MasstransitSchema.Validate(schema);
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(CLI.GetUsage(cli.Options));
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("The program could not handle the command.", ex);
            }
        }

        private sealed class CLI
        {
            public CLI(string[] arguments)
            {
                Parser = new Parser(
                    settings =>
                    {
                        settings.CaseSensitive = false;
                        settings.MutuallyExclusive = false;
                        settings.IgnoreUnknownArguments = true;
                        settings.ParsingCulture = CultureInfo.InvariantCulture;
                    });
                Arguments = arguments;
                Options = new Options();
            }

            public Parser Parser { get; }
            public string[] Arguments { get; }
            public Options Options { get; }

            public static string GetUsage(object options)
            {
                var helpText = HelpText.AutoBuild(options, current => HelpText.DefaultParsingErrorsHandler(options, current));
                return helpText.ToString();
            }
        }

        private sealed class Options
        {
            [ParserState]
            public IParserState State { get; set; }

            [Option('s', "subject", Required = true)]
            public string Subject { get; set; }

            [Option('c', "command", Required = true)]
            public string Command { get; set; }

            [Option('v', "verbose", Required = false, DefaultValue = false)]
            public bool Verbose { get; set; }

            [Option('?', Required = false, DefaultValue = false)]
            public bool Help { get; set; }

            public override string ToString() => $"{nameof(Subject)}: {Subject}, {nameof(Command)}: {Command}";
        }

        private sealed class RabbitmqConfig
        {
            [ParserState]
            public IParserState State { get; set; }

            [Option("host", DefaultValue = @"http://localhost")]
            public string Host { get; set; }

            [Option("port", DefaultValue = 15672)]
            public int Port { get; set; }

            [Option("vhost", DefaultValue = "/")]
            public string VHost { get; set; }

            [Option('u', "username", DefaultValue = "guest")]
            public string UserName { get; set; }

            [Option('p', "password", DefaultValue = "guest")]
            public string Password { get; set; }

            public ManagementClient CreateClient() => new ManagementClient(Host, UserName, Password, Port);

            public static RabbitmqConfig Parse(Parser parser, string[] args)
            {
                var config = new RabbitmqConfig();
                parser.ParseArguments(args, config);
                config = Merge(FromEnv(), config);
                return config;
            }

            public static RabbitmqConfig FromEnv()
            {
                var config = new RabbitmqConfig();
                config.Host = Environment.GetEnvironmentVariable("RABBITMQTOOL_HOST");
                config.Port =
                    (int)
                    (TypeDescriptor.GetConverter(typeof(int)).ConvertFromInvariantString(Environment.GetEnvironmentVariable("RABBITMQTOOL_PORT") ?? "0") ?? 0);
                config.VHost = Environment.GetEnvironmentVariable("RABBITMQTOOL_VHOST");
                config.UserName = Environment.GetEnvironmentVariable("RABBITMQTOOL_USERNAME");
                config.Password = Environment.GetEnvironmentVariable("RABBITMQTOOL_PASSWORD");
                return config;
            }

            public static RabbitmqConfig Merge(RabbitmqConfig a, RabbitmqConfig b)
            {
                var config = new RabbitmqConfig();
                config.Host = a.Host ?? b.Host;
                config.Port = a.Port != 0 ? a.Port : b.Port;
                config.VHost = a.VHost ?? b.VHost;
                config.UserName = a.UserName ?? b.UserName;
                config.Password = a.Password ?? b.Password;
                return config;
            }

            public override string ToString() => $"{nameof(Host)}: {Host}, {nameof(Port)}: {Port}, {nameof(VHost)}: {VHost}";
        }

        private sealed class RabbitmqDiffConfig
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