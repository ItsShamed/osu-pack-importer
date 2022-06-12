using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using SharpCompress.Archives.Zip;
using Spectre.Console;

namespace OsuPackImporter.Beatmaps;

public class BeatmapSet : IParsable, IOSDBSerializable
{
    private Stream _fileStream;

    public BeatmapSet(Stream fileStream, ProgressContext? context = null)
    {
        Beatmaps = new List<ExtendedBeatmap>();
        _fileStream = fileStream;
        Parse(context);
    }

    public BeatmapSet(string path, ProgressContext? context = null) : this(File.OpenRead(path), context)
    {
    }

    public List<ExtendedBeatmap> Beatmaps { get; }

    public byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log("[BeatmapSet] Serializing...", LogLevel.Debug);
        using (var memstream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memstream))
            {
                var task = Program.Verbose ? context?.AddTask("Serializing beatmapset") : null;
                task?.MaxValue(Beatmaps.Count);
                foreach (var beatmap in Beatmaps)
                {
                    writer.Write(beatmap.Serialize());
                    task?.Increment(1);
                }
            }

            return memstream.ToArray();
        }
    }

    public byte[] SerializeOSDB(ProgressContext? context = null)
    {
        Logging.Log("[BeatmapSet] Serializing...", LogLevel.Debug);
        using (var memstream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memstream))
            {
                var task = Program.Verbose ? context?.AddTask("Serializing beatmapset") : null;
                task?.MaxValue(Beatmaps.Count);
                foreach (var beatmap in Beatmaps)
                {
                    writer.Write(beatmap.SerializeOSDB());
                    task?.Increment(1);
                }
            }

            return memstream.ToArray();
        }
    }

    public IParsable Parse(ProgressContext? context = null)
    {
        try
        {
            using (var archive = ZipArchive.Open(_fileStream))
            {
                var task = Program.Verbose
                    ? context?.AddTask("Importing beatmapset (" + archive.Entries.Count + ")")
                    : null;
                task?.MaxValue(archive.Entries.Count);
                foreach (var entry in archive.Entries)
                {
                    if (entry.Key.EndsWith(".osu"))
                    {
                        Logging.Log("[Beatmapset] Detected " + entry.Key, LogLevel.Debug);
                        var memstream = new MemoryStream();
                        entry.OpenEntryStream().CopyTo(memstream);
                        Beatmaps.Add(ExtendedBeatmapDecoder.Decode(memstream));
                        memstream.Dispose();
                    }

                    task?.Increment(1);
                }
            }

            return this;
        }
        catch (Exception e)
        {
            Logging.Log("An unknown error occured while parsing a beatmapset:", LogLevel.Error);
            AnsiConsole.WriteException(e);
            return this;
        }
    }

    public IParsable Parse(Stream stream, ProgressContext? context = null)
    {
        _fileStream = stream;
        return Parse(context);
    }
}