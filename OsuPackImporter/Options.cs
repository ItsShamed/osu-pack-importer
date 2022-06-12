using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace OsuPackImporter
{
    public class Options
    {
        [Value(0, Required = true, MetaName = "input path", HelpText = "Location of the beatmap pack archive.")]
        public string InputPath { get; set; }

        [Option('v', "verbose", Default = false,
            HelpText = "Log more stuff to the console. Useful to debug the program and catch errors.",
            Required = false)]
        public bool Verbose { get; set; }

        [Option("no-import", Default = false,
            HelpText = "Prevent automatic import of beatmaps in the game when dumping into collection.db.",
            Required = false)]
        public bool NoAutoImport { get; set; }

        [Option("osdb", HelpText = "Whether to export or not the parsed archive as an OSDB file.", Required = false)]
        public string OSDBPath { get; set; }

        [Option("osudir", Required = false,
            HelpText = "The location of your osu!stable installation.")]
        public string OsuPath { get; set; }

        [Usage(ApplicationAlias = "OsuPackImporter")]
        public static IEnumerable<Example> Examples => new[]
        {
            new Example("Convert and import a beatmap pack into a collection in the game",
                new Options
                {
                    InputPath = "BeatmapPack.7z"
                }),
            new Example("Convert without importing a beatmap pack into a collection in the game",
                new Options
                {
                    InputPath = "BeatmapPack.7z",
                    NoAutoImport = true
                }),
            new Example("Convert a beatmap pack into an .osdb file", new Options
            {
                InputPath = "BeatmapPack.zip",
                OSDBPath = "collection.osdb"
            })
        };
    }
}