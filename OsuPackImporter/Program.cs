using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using OsuPackImporter.Collections;
using SharpCompress.Archives.GZip;
using SharpCompress.Common;
using SharpCompress.Writers;
using Spectre.Console;

namespace OsuPackImporter;

public static class Program
{
    public static bool Verbose;
    public static bool AutoImport;
    public static bool SkipDuplicateCheck;
    private static ParserResult<Options>? _parserResult;

    public static void Main(string[] args)
    {
        if (args.Length == 0 && AnsiConsole.Confirm("Did you meant to run the program without arguments? " +
                                                    "(e.g: you opened the program from the file explorer)"))
        {
            Logging.Log("You'll now go through providing the necessary information for the program to" +
                        " work.");
            AnsiConsole.MarkupLine("[italic]Press any key to continue...[/]");
            Console.ReadKey(true);
            GuidedRun(ref args);
        }

        _parserResult = Parser.Default.ParseArguments<Options>(args);

        _parserResult
            .WithParsed(o => Environment.Exit(Run(o)))
            .WithNotParsed(e => Environment.Exit(FailRun(e, _parserResult)));
    }

    private static void GuidedRun(ref string[] args)
    {
        List<string> tempArgs = new List<string>();
        string inputPath = AnsiConsole.Ask<string>("Enter the path to the beatmap pack " +
                                                   "[gray](you can also drag the file in the console)[/]:");
        if (String.IsNullOrWhiteSpace(inputPath))
            return;
        tempArgs.Add(inputPath);

        if (AnsiConsole.Confirm("Do you want to export it as a .osdb file?", false))
        {
            string osdbFolderPath = AnsiConsole.Ask(
                "Enter the path of the folder where the .osdb " +
                "file will be saved [gray](you can also drag the folder in the console)[/]",
                Directory.GetCurrentDirectory());
            while (!Directory.Exists(osdbFolderPath))
            {
                Logging.Log("Could not find the specified folder.", LogLevel.Error);
                osdbFolderPath = AnsiConsole.Ask(
                    "Enter the path of the folder where the .osdb " +
                    "file will be saved [gray](you can also drag the folder in the console)[/]",
                    Directory.GetCurrentDirectory());
            }

            string osdbFileName = AnsiConsole.Ask("Enter the name of the file", "collection.osdb");
            if (!osdbFileName.EndsWith(".osdb")) osdbFileName += ".osdb";
            string osdbPath = osdbFolderPath + "\\" + osdbFileName;
            while (File.Exists(osdbPath))
            {
                Logging.Log("This file already exists.");
                if (!AnsiConsole.Confirm("Do you want to overwrite this file?", false))
                {
                    osdbFileName = AnsiConsole.Ask("Enter the name of the file", "collection.osdb");
                    if (!osdbFileName.EndsWith(".osdb")) osdbFileName += ".osdb";
                    osdbPath = osdbFolderPath + "\\" + osdbFileName;
                }
            }

            tempArgs.Add("--osdb");
            tempArgs.Add(osdbPath);
        }
        else
        {
            if (!AnsiConsole.Confirm("Do you want to automatically import the beatmaps in osu!?"))
            {
                tempArgs.Add("--no-import");
            }
        }

        args = tempArgs.ToArray();
        Logging.Log("The program will now start with the following arguments:");
        Logging.Log("OsuPackImporter " + String.Join(' ', args));
        if (!AnsiConsole.Confirm("Are you okay with this?"))
        {
            Logging.Log("Aborting...", LogLevel.Error);
            Environment.Exit(1);
        }
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
        SkipDuplicateCheck = options.SkipDuplicateCheck;

        var osuPath = options.OsuPath ?? Environment.GetEnvironmentVariable("LOCALAPPDATA") +
            Path.DirectorySeparatorChar + "osu!";
        var useOsdb = !string.IsNullOrWhiteSpace(options.OSDBPath);
        AutoImport = !options.NoAutoImport && !useOsdb;
        ExtendedCollection? extendedCollection = null;
        Logging.Log("Importing archive " + options.InputPath);
        Progress(ctx => { extendedCollection = new ExtendedCollection(options.InputPath, ctx); });
        if (extendedCollection == null)
        {
            Logging.Log("Failed to parse the archive. Aborting...", LogLevel.Fatal);
            return 1;
        }

        if (!options.NoRename)
            if (AnsiConsole.Confirm("Do you want to rename the imported collections?"))
                extendedCollection.Rename();

        try
        {
            return useOsdb
                ? RunOsdbConversion(extendedCollection, options.OSDBPath!)
                : RunLegacyConversion(extendedCollection, osuPath);
        }
        catch (OperationCanceledException e)
        {
            Logging.Log("The program was aborted: " + e.Message, LogLevel.Error);
            return 1;
        }
        catch (Exception e)
        {
            Logging.Log("An unknown error occured: ", LogLevel.Fatal);
            AnsiConsole.WriteException(e);
            return 1;
        }
    }

    private static int FailRun(IEnumerable<Error> errors, ParserResult<Options>? parserResult)
    {
        return 1;
    }

    private static int RunLegacyConversion(ExtendedCollection inputCollection, string osuPath)
    {
        Logging.Log("Will now start converting the pack into a collection and dumping it into collection.db");

        Directory.SetCurrentDirectory(osuPath);
        CollectionDB? collectionDb = null;

        Logging.Log("Checking if osu! is open...", LogLevel.Debug);

        if (Process.GetProcessesByName("osu!").Length > 0)
        {
            Logging.Log("osu! is currently open. Please close the game before continuing.", LogLevel.Warn);
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow bold"))
                .Start("Waiting for osu! to be closed...", _ =>
                {
                    var processes = Process.GetProcessesByName("osu!");
                    while (processes.Length > 0)
                    {
                        processes = Process.GetProcessesByName("osu!");
                        Thread.Sleep(100);
                    }
                });
        }

        Logging.Log("Parsing collection.db...");

        Progress(ctx => { collectionDb = new CollectionDB("collection.db", ctx); });
        if (collectionDb == null)
        {
            Logging.Log("collection.db parsing failed. Aborting...");
            return 1;
        }

        List<Collection>? duplicates = null;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow bold"))
            .Start("Checking for duplicates...", _ =>
            {
                if (SkipDuplicateCheck)
                {
                    Logging.Log("Duplicate checks are skipped. Duplicate collections may be created.", LogLevel.Warn);
                    return;
                }

                duplicates = inputCollection.CheckForDuplicates(collectionDb);
            });

        if (duplicates!.Count > 0 && !SkipDuplicateCheck)
        {
            Logging.Log("Duplicates were found. The following collections will be created if continuing:",
                LogLevel.Warn);
            foreach (var duplicate in duplicates) Logging.Log("- " + duplicate.Name, LogLevel.Warn);

            if (!AnsiConsole.Confirm("Do you want to continue?", false))
                throw new OperationCanceledException("Duplicates were found.");
        }

        collectionDb.Collections.Add(inputCollection);

        Logging.Log("Backing up existing collection.db...");

        File.Copy("collection.db", "collection.db.OLD_"
                                   + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
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
            var inputLength = inputStream.Length;
            archive.AddEntry("collection.osdb", inputStream, inputLength, DateTime.UtcNow);
            archive.SaveTo(outputStream, new WriterOptions(CompressionType.GZip));
        }
    }

    private static string WriteOsdb(ExtendedCollection inputCollection)
    {
        Logging.Log("Generating collection.osdb file...");
        var outputPath = Path.GetTempPath() + Path.PathSeparator + "collection" +
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