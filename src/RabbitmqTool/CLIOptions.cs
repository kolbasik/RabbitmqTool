using CommandLine;

namespace RabbitmqTool
{
    public sealed class CLIOptions
    {
        [ParserState]
        public IParserState State { get; set; }

        [Option('s', "subject", Required = true)]
        public string Subject { get; set; }

        [Option('c', "command", Required = true)]
        public string Command { get; set; }

        [Option("debug")]
        public bool Debug { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }

        [Option('?', "help")]
        public bool Help { get; set; }

        public override string ToString() => $"{nameof(Subject)}: {Subject}, {nameof(Command)}: {Command}";
    }
}