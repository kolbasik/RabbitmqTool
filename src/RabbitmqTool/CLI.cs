using System;
using System.Collections.Generic;
using System.Globalization;
using CommandLine;
using CommandLine.Text;

namespace RabbitmqTool
{
    public sealed class CLI
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
            Options = new CLIOptions();
            Commands = new Dictionary<string, Dictionary<string, ICommand>>(StringComparer.OrdinalIgnoreCase);
        }

        public Parser Parser { get; }
        public string[] Arguments { get; }
        public CLIOptions Options { get; }
        public Dictionary<string, Dictionary<string, ICommand>> Commands { get; }

        public void Register(string subjectName, string commandName, ICommand command)
        {
            Dictionary<string, ICommand> commands;
            if (!Commands.TryGetValue(subjectName, out commands))
            {
                Commands[subjectName] = commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
            }
            command.CLI = this;
            commands[commandName] = command;
        }

        public ICommand Resolve(string subjectName, string commandName)
        {
            Dictionary<string, ICommand> commands;
            if (Commands.TryGetValue(subjectName, out commands))
            {
                ICommand command;
                if (commands.TryGetValue(commandName, out command))
                {
                    return command;
                }
            }
            throw new NotSupportedException("Could not supported the current command.");
        }

        public string GetUsage(object options)
        {
            var helpText = HelpText.AutoBuild(options, current => HelpText.DefaultParsingErrorsHandler(options, current));
            return helpText.ToString();
        }

        public interface ICommand
        {
            CLI CLI { get; set; }
            void Handle();
        }

        public abstract class CommandBase<TConfig> : ICommand
        {
            public CLI CLI { get; set; }

            public abstract TConfig Config();
            public abstract void Execute(TConfig config);

            public void Handle()
            {
                var config = Config();
                if (CLI.Options.Help || !CLI.Parser.ParseArguments(CLI.Arguments, config))
                {
                    Console.WriteLine(CLI.GetUsage(config));
                }
                else
                {
                    if (CLI.Options.Verbose)
                    {
                        Console.WriteLine($"Command: {CLI.Options}");
                        Console.WriteLine($"Configuration: {config}");
                    }
                    Execute(config);
                }
            }
        }

        public sealed class Command<TConfig> : CommandBase<TConfig>
            where TConfig : new()
        {
            private readonly Action<TConfig> execute;

            public Command(Action<TConfig> execute)
            {
                this.execute = execute;
            }

            public override TConfig Config() => new TConfig();

            public override void Execute(TConfig config) => execute(config);
        }
    }
}