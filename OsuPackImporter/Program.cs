using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using CommandLine;
using CommandLine.Text;
using OsuPackImporter.Collections;
using SharpCompress.Archives.GZip;
using SharpCompress.Common;
using SharpCompress.Writers;
using Spectre.Console;

namespace OsuPackImporter
{
    public static class Program
    {
        public static bool Verbose;
        public static bool AutoImport;
        private static ParserResult<Options> _parserResult;

        public static void Main(string[] args)
        {
            _parserResult = Parser.Default.ParseArguments<Options>(args);

            _parserResult
                .WithParsed(o => Environment.Exit(Run(o)))
                .WithNotParsed(e => Environment.Exit(FailRun(e, _parserResult)));
        }

        private static int Run(Options options)
        {
            if (options.InputPath == null)
            {
                Logging.Log("Missing input path", LogLevel.Error);
                Console.WriteLine(HelpText.RenderUsageText(_parserResult));
                return 1;
            }

            Verbose = options.Verbose;

            var osuPath = options.OsuPath ?? Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                Path.DirectorySeparatorChar + "osu!";
            var useOsdb = !string.IsNullOrWhiteSpace(options.OSDBPath);
            AutoImport = !options.NoAutoImport && !useOsdb;
            ExtendedCollection extendedCollection = null;
            Logging.Log("Importing archive " + options.InputPath);
            Progress(ctx => { extendedCollection = new ExtendedCollection(options.InputPath, ctx); });
            try
            {
                return useOsdb
                    ? RunOsdbConversion(extendedCollection, options.OSDBPath)
                    : RunLegacyConversion(extendedCollection, osuPath);
            }
            catch (Exception e)
            {
                Logging.Log("An unknown error occured: ", LogLevel.Fatal);
                AnsiConsole.WriteException(e);
                return 1;
            }
        }

        private static int FailRun(IEnumerable<Error> errors, ParserResult<Options> parserResult)
        {
            foreach (Error error in errors)
            {
                Logging.Log(error.ToString(), LogLevel.Error);
            }
            Console.Error.WriteLine(HelpText.RenderUsageText(parserResult));
            return 1;
        }

        private static int RunLegacyConversion(ExtendedCollection inputCollection, string osuPath)
        {
            Logging.Log("Will now start converting the pack into a collection and dumping it into collection.db");

            Directory.SetCurrentDirectory(osuPath);
            CollectionDB collectionDb = null;

            Logging.Log("Parsing collection.db...");

            Progress(ctx => { collectionDb = new CollectionDB("collection.db", ctx); });
            collectionDb.Collections.Add(inputCollection);
            if (File.Exists("collection.db.OLD"))
            {
                Logging.Log("Deleting old collection.db backup...", LogLevel.Warn);
                File.Delete("collection.db.OLD");
            }

            Logging.Log("Backing up existing collection.db...");

            File.Copy("collection.db", "collection.db.OLD");
            using (var stream = File.Create(@"collection.db"))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    byte[] buffer = { };
                    Logging.Log("Re-serializing collection.db");
                    Progress(ctx => { buffer = collectionDb.Serialize(ctx); });
                    if (buffer.Length == 0) throw new DataException("No data was serialized");
                    writer.Write(buffer);
                }
            }

            Logging.Log("Done! Your beatmap pack now appears as a collection in the game.");

            return 0;
        }

        private static int RunOsdbConversion(ExtendedCollection inputCollection, string outputPath)
        {
            Logging.Log("Will now start converting the pack into an osdb file.");
            using (var inputStream = File.OpenRead(WriteOsdb(inputCollection)))
            using (var stream = File.Create(outputPath))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write("o!dm8");
                    CompressStream(inputStream, stream);
                }
            }

            Logging.Log(
                "Done! Your beatmap pack has now been converted into an osdb file usable with Piotrekol's Collection Manager.");
            Logging.Log("If you don't have Collection Manager, you can download it here: " + 
                        "https://github.com/Piotrekol/CollectionManager/releases");

            return 0;
        }

        private static void CompressStream(Stream inputStream, Stream outputStream)
        {
            Logging.Log("Compressing file...");
            using (var archive = GZipArchive.Create())
            {
                long inputLength = inputStream.Length;
                archive.AddEntry("collection.osdb", inputStream, inputLength, DateTime.UtcNow);
                archive.SaveTo(outputStream, new WriterOptions(CompressionType.GZip));
            }
        }

        private static string WriteOsdb(ExtendedCollection inputCollection)
        {
            Logging.Log("Generating collection.osdb file...");
            string outputPath = Path.GetTempPath() + Path.PathSeparator + "collection" +
                                inputCollection.GetHashCode().ToString("x") + ".osdb";

            byte[] buffer = { };
            Progress(ctx => buffer = inputCollection.SerializeOSDB(ctx));
            if (buffer.Length == 0) throw new DataException("No data was serialized");

            using (var stream = File.Create(outputPath))
            {
                using (var binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.Write("o!dm8");
                    binaryWriter.Write(DateTime.Now.ToOADate());
                    binaryWriter.Write(Environment.UserName);
                    binaryWriter.Write(inputCollection.ExtendedCount);
                    binaryWriter.Write(buffer);
                    binaryWriter.Write("By Piotrekol");
                }
            }

            return outputPath;
        }

        private static void Progress(Action<ProgressContext> action)
        {
            AnsiConsole.Progress()
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(),
                    new SpinnerColumn(), new ElapsedTimeColumn()).Start(action);
        }
    }
}