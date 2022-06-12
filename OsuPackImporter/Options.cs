using CommandLine;

namespace OsuPackImporter
{
    public class Options
    {
        [Value(0)] public string InputPath { get; set; }

        [Option('v', "verbose", Default = false,
            HelpText = "Log more stuff to the console. Useful to debug the program and catch errors.",
            Required = false)]
        public bool Verbose { get; set; }

        [Option("osdb", HelpText = "Whether to export or not the parsed archive as an OSDB file.", Required = false)]
        public string OSDBPath { get; set; }

        [Option("osudir", Required = false)] public string OsuPath { get; set; }

        // TODO: Add further options
    }
}