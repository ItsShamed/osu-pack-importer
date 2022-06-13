using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using SharpCompress.Archives.Zip;
using Spectre.Console;

namespace OsuPackImporter.Beatmaps;

/// <summary>
/// This class represents an osu! beatmap set.
/// </summary>
public class BeatmapSet : IParsable, IOSDBSerializable
{
    private Stream _fileStream;

    /// <summary>
    /// Creates a new beatmap set from an archive stream.
    /// </summary>
    /// <param name="fileStream">The stream containing the archive data.</param>
    /// <param name="context">Terminal context used to update the progress bar.</param>
    public BeatmapSet(Stream fileStream, ProgressContext? context = null)
    {
        Beatmaps = new List<ExtendedBeatmap>();
        _fileStream = fileStream;
        Parse(context);
    }

    /// <summary>
    /// Create a new beatmap set from an archive located at the given path.
    /// </summary>
    /// <param name="path">Path of the archive.</param>
    /// <param name="context">Terminal context used to update the progress bar.</param>
    public BeatmapSet(string path, ProgressContext? context = null) : this(File.OpenRead(path), context)
    {
    }

    /// <summary>
    /// The list of beatmaps in this set.
    /// </summary>
    public List<ExtendedBeatmap> Beatmaps { get; }

    /// <summary>
    /// Serializes the beatmap set as a list of beatmap hashes.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>Byte array containing the serialized data</returns>
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

    /// <summary>
    /// Serializes all the beatmaps of this set in the OSDB format.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bar.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Parses the currently loaded archive stream and builds the beatmap set.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>This instance of the beatmap set.</returns>
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

    /// <summary>
    /// Parses a given archive stream and builds the beatmap set.
    /// </summary>
    /// <param name="stream">Archive stream</param>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>This instance of the beatmap set.</returns>
    public IParsable Parse(Stream stream, ProgressContext? context = null)
    {
        _fileStream = stream;
        return Parse(context);
    }
}