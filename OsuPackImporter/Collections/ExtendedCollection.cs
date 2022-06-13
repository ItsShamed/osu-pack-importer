using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsuPackImporter.Beatmaps;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using Spectre.Console;

namespace OsuPackImporter.Collections;

/// <summary>
/// This collection class uses <see cref="ExtendedBeatmap"/> and <see cref="BeatmapSets"/> objects to store
/// beatmaps and is serializable to the OSDB format.
/// </summary>
public class ExtendedCollection : Collection, IOSDBSerializable, IParsable
{
    private readonly Stream _fileStream;

    /// <summary>
    /// Initializes a new instance from archive stream.
    /// </summary>
    /// <param name="stream">Archive stream</param>
    /// <param name="name">Name of the collection</param>
    /// <param name="context">Terminal context, used to update progress bars.</param>
    public ExtendedCollection(Stream stream, string? name = null, ProgressContext? context = null)
    {
        Name = name;
        SubCollections = new List<Collection>();
        BeatmapSets = new List<BeatmapSet>();
        Beatmaps = new List<ExtendedBeatmap>();
        _fileStream = stream;
        Logging.Log("[Collection] New collection: " + Name, LogLevel.Debug);
        Parse(context);
    }

    /// <summary>
    /// Initializes a new instance from archive file.
    /// </summary>
    /// <param name="path">Path of the archive file</param>
    /// <param name="context">Terminal context, used to update progress bars.</param>
    public ExtendedCollection(string path, ProgressContext? context = null)
        : this(File.OpenRead(path), path.Split(Path.DirectorySeparatorChar).Last().Split('.')[0], context)
    {
    }

    public List<Collection> SubCollections { get; }
    public List<BeatmapSet> BeatmapSets { get; }

    public List<ExtendedBeatmap> Beatmaps { get; }

    public override int Count => ComputeCount(out _, out _);

    /// <summary>
    /// Number of sub-<see cref="LegacyCollection"/>s in this collection.
    /// </summary>
    public int LegacyCount
    {
        get
        {
            ComputeCount(out var legacyCount, out _);
            return legacyCount;
        }
    }

    /// <summary>
    /// Number of sub-<see cref="ExtendedCollection"/>s in this collection.
    /// </summary>
    public int ExtendedCount
    {
        get
        {
            ComputeCount(out _, out var extendedCount);
            return extendedCount;
        }
    }

    public override List<byte[]> BeatmapHashes
    {
        get
        {
            var hashes = new List<byte[]>();

            foreach (var beatmapSet in BeatmapSets) hashes.AddRange(beatmapSet.Beatmaps.ConvertAll(b => b.Hash!));

            hashes.AddRange(Beatmaps.ConvertAll(b => b.Hash!));
            return hashes;
        }
    }

    /// <summary>
    /// Serializes this collection to the OSDB format documented
    /// <a href="https://gist.github.com/ItsShamed/c3c6c83903653d72d1f499d7059fe185#collection-format">here</a>.
    /// </summary>
    /// <param name="context">Terminal context, used to update</param>
    /// <returns>The serialized data</returns>
    public byte[] SerializeOSDB(ProgressContext? context = null)
    {
        Logging.Log("[ExtendedCollection] Serializing " + Name + " (" + ExtendedCount + ")", LogLevel.Debug);
        using (var memstream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memstream))
            {
                var task = context?.AddTask("Serializing " + Name);
                task?.MaxValue(BeatmapHashes.Count + SubCollections.Count);
                writer.Write(Name ?? "Unnamed collection");
                writer.Write(0);
                writer.Write(BeatmapHashes.Count);
                foreach (var beatmapSet in BeatmapSets)
                {
                    writer.Write(beatmapSet.SerializeOSDB());
                    task?.Increment(1);
                }

                foreach (var beatmap in Beatmaps)
                {
                    writer.Write(beatmap.SerializeOSDB());
                    task?.Increment(1);
                }

                writer.Write(0);

                foreach (var subCollection in SubCollections)
                {
                    if (subCollection is ExtendedCollection collection)
                        writer.Write(collection.SerializeOSDB());
                    task?.Increment(1);
                }
            }

            return memstream.ToArray();
        }
    }

    /// <summary>
    /// Serializes this collections to the legacy collection.db format documented
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars</param>
    /// <returns>The serialized data.</returns>
    public override byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log("[ExtendedCollection] Serializing " + Name + " (" + BeatmapHashes.Count + ")", LogLevel.Debug);
        using var memstream = new MemoryStream();
        using (var writer = new BinaryWriter(memstream))
        {
            var task = context?.AddTask("Serializing " + Name);
            task?.MaxValue(BeatmapHashes.Count + SubCollections.Count);

            writer.Write((byte) 0x0b);
            writer.Write(Name ?? "Unnamed collection");
            writer.Write(BeatmapHashes.Count);
            foreach (var hash in BeatmapHashes)
            {
                var stringHash = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                writer.Write((byte) 0x0b);
                writer.Write(stringHash);
                task?.Increment(1);
            }

            foreach (var collection in SubCollections)
            {
                writer.Write(collection.Serialize());
                task?.Increment(1);
            }
        }

        return memstream.ToArray();
    }
    
    public override void Rename()
    {
        base.Rename();
        foreach (var collection in SubCollections)
            collection.Rename();
    }

    /// <summary>
    /// Parses the currently loaded archive stream and builds the collection.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>This instance of <see cref="ExtendedCollection"/></returns>
    /// <exception cref="NotSupportedException">if the loaded archive is not supported by SharpCompress</exception>
    public IParsable Parse(ProgressContext? context = null)
    {
        using (var archive = GetArchive(_fileStream))
        {
            var task = context?.AddTask("Importing collection " + Name);
            task?.MaxValue(archive?.Entries.Count() ?? 0);

            if (archive?.Entries != null)
                foreach (var entry in archive.Entries)
                {
                    Logging.Log("[ExtendedCollection] Parsing " + entry.Key, LogLevel.Debug);
                    if (entry.Key.EndsWith(".zip") || entry.Key.EndsWith(".7z") || entry.Key.EndsWith(".rar") ||
                        entry.Key.EndsWith(".gz"))
                    {
                        using (var memstream = new MemoryStream())
                        {
                            entry.OpenEntryStream().CopyTo(memstream);
                            SubCollections.Add(new ExtendedCollection(memstream,
                                entry.Key.Split(Path.DirectorySeparatorChar).Last().Split('.')[0]));
                        }
                    }
                    else if (entry.Key.EndsWith(".osz"))
                    {
                        var path = Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                                   $@"\osu!\Songs\{entry.Key.Split('/').Last()}";
                        if (Program.AutoImport)
                        {
                            Logging.Log("[ExtendedCollection] Importing to " + path, LogLevel.Debug);
                            entry.WriteToFile(path);
                        }

                        using (var memstream = new MemoryStream())
                        {
                            entry.OpenEntryStream().CopyTo(memstream);
                            BeatmapSets.Add(new BeatmapSet(memstream));
                        }
                    }
                    else if (entry.Key.EndsWith(".osu"))
                    {
                        Logging.Log("[Collection] Adding beatmap", LogLevel.Debug);
                        Beatmaps.Add(ExtendedBeatmapDecoder.Decode(entry.OpenEntryStream()));
                    }

                    task?.Increment(1);
                }
            else
                throw new NotSupportedException("This archive is not supported.");
        }

        return this;
    }

    private IArchive? GetArchive(Stream stream)
    {
        MemoryStream zipMemory;
        if (stream is MemoryStream mem)
        {
            zipMemory = new MemoryStream(mem.ToArray());
        }
        else
        {
            zipMemory = new MemoryStream();
            stream.CopyTo(zipMemory);
            stream.Position = 0;
        }

        var GZipMemory = new MemoryStream(zipMemory.ToArray());
        var sevenZipMemory = new MemoryStream(GZipMemory.ToArray());
        var tarMemory = new MemoryStream(sevenZipMemory.ToArray());


        if (RarArchive.IsRarFile(stream, new ReaderOptions {LeaveStreamOpen = true}))
        {
            Logging.Log("[ExtendedCollection] RAR Archive");
            return RarArchive.Open(stream);
        }

        stream.Position = 0;

        if (ZipArchive.IsZipFile(stream))
        {
            Logging.Log("[ExtendedCollection] Zip Archive");
            zipMemory.Dispose();
            sevenZipMemory.Dispose();
            GZipMemory.Dispose();
            tarMemory.Dispose();
            return ZipArchive.Open(stream);
        }

        zipMemory.Dispose();
        stream.Position = 0;

        if (SevenZipArchive.IsSevenZipFile(sevenZipMemory))
        {
            Logging.Log("[ExtendedCollection] 7Zip Archive", LogLevel.Debug);
            sevenZipMemory.Dispose();
            GZipMemory.Dispose();
            tarMemory.Dispose();
            return SevenZipArchive.Open(stream);
        }

        sevenZipMemory.Dispose();
        stream.Position = 0;

        if (GZipArchive.IsGZipFile(GZipMemory))
        {
            Logging.Log("[ExtendedCollection] GZip Archive", LogLevel.Debug);
            GZipMemory.Dispose();
            tarMemory.Dispose();
            return GZipArchive.Open(stream);
        }

        GZipMemory.Dispose();
        stream.Position = 0;

        if (TarArchive.IsTarFile(tarMemory))
        {
            Logging.Log("[ExtendedCollection] Tar Archive", LogLevel.Debug);
            tarMemory.Dispose();
            return TarArchive.Open(stream);
        }

        tarMemory.Dispose();
        Logging.Log("[ExtendedCollection] Unknown input file", LogLevel.Debug);
        return null;
    }

    private int ComputeCount(out int legacyCount, out int extendedCount)
    {
        extendedCount = 1;
        legacyCount = 0;
        foreach (var collection in SubCollections)
            if (collection is LegacyCollection)
                legacyCount++;
            else
                extendedCount += ((ExtendedCollection) collection).Count;

        return legacyCount + extendedCount;
    }
}