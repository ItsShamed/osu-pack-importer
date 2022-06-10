using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;
using OsuPackImporter.Collections;
using SharpCompress.Archives.GZip;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace OsuPackImporter
{
    public class Program
    {
        public static bool Verbose;
        private static ParserResult<Options> _parserResult;

        public static void Main(string[] args)
        {
            _parserResult = Parser.Default.ParseArguments<Options>(args);

            _parserResult
                .WithParsed(o => Environment.Exit(Run(o)))
                .WithNotParsed(e => Environment.Exit(FailRun(e, _parserResult)));

            /*
            ExtendedCollection collection = new ExtendedCollection(path);
            Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                                          Path.DirectorySeparatorChar + "osu!");
            CollectionDB collectionDb = new CollectionDB("collection.db");
            collectionDb.Collections.Add(collection);
            if (File.Exists("collection.db.OLD"))
            {
                File.Delete("collection.db.OLD");
            }

            File.Copy("collection.db", "collection.db.OLD");
            using (FileStream stream = File.Create(@"collection.db"))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(collectionDb.Serialize());
                }
            }
        */
        }

        private static int Run(Options options)
        {
            if (options.InputPath == null)
            {
                Console.Error.Write("Missing input path");
                Console.WriteLine(HelpText.RenderUsageText(_parserResult));
                return 1;
            }

            var osuPath = options.OsuPath ?? Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                Path.DirectorySeparatorChar + "osu!";
            var useOsdb = !string.IsNullOrWhiteSpace(options.OSDBPath);
            var extendedCollection = new ExtendedCollection(options.InputPath);
            try
            {
                return useOsdb
                    ? RunOsdbConversion(extendedCollection, options.OSDBPath)
                    : RunLegacyConversion(extendedCollection, osuPath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static int FailRun(IEnumerable<Error> errors, ParserResult<Options> parserResult)
        {
            Console.Error.WriteLine(HelpText.RenderUsageText(parserResult));
            return 1;
        }

        private static int RunLegacyConversion(ExtendedCollection inputCollection, string osuPath)
        {
            Directory.SetCurrentDirectory(osuPath);
            var collectionDb = new CollectionDB("collection.db");
            collectionDb.Collections.Add(inputCollection);
            if (File.Exists("collection.db.OLD")) File.Delete("collection.db.OLD");

            File.Copy("collection.db", "collection.db.OLD");
            using (var stream = File.Create(@"collection.db"))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(collectionDb.Serialize());
                }
            }

            return 0;
        }

        private static int RunOsdbConversion(ExtendedCollection inputCollection, string outputPath)
        {
            using (var inputStream = File.OpenRead(WriteOsdb(inputCollection)))
            using (var stream = File.Create(outputPath))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write("o!dm8");
                    CompressStream(inputStream, stream);
                }
            }

            return 0;
        }

        private static void CompressStream(Stream inputStream, Stream outputStream)
        {
            using (var archive = GZipArchive.Create())
            {
                long inputLength = inputStream.Length;
                archive.AddEntry("collection.osdb", inputStream, inputLength, DateTime.UtcNow);
                archive.SaveTo(outputStream, new WriterOptions(CompressionType.GZip));
            }
        }

        private static string WriteOsdb(ExtendedCollection inputCollection)
        {
            string outputPath = Path.GetTempPath() + Path.PathSeparator + "collection" +
                                inputCollection.GetHashCode().ToString("x") + ".osdb";

            using (var stream = File.Create(outputPath))
            {
                using (var binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.Write("o!dm8");
                    binaryWriter.Write(DateTime.Now.ToOADate());
                    binaryWriter.Write(Environment.UserName);
                    binaryWriter.Write(inputCollection.ExtendedCount);
                    binaryWriter.Write(inputCollection.SerializeOSDB());
                    binaryWriter.Write("By Piotrekol");
                }
            }

            return outputPath;
        }
    }
}