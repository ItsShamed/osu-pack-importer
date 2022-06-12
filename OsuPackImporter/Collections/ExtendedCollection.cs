using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

namespace OsuPackImporter.Collections
{
    public class ExtendedCollection : Collection, IOSDBSerializable, IParsable
    {
        private Stream _fileStream;

        public List<Collection> SubCollections { get; }
        public List<BeatmapSet> BeatmapSets { get; }

        public List<ExtendedBeatmap> Beatmaps { get; }

        public override int Count => ComputeCount(out _, out _);

        public int LegacyCount
        {
            get
            {
                ComputeCount(out int legacyCount, out _);
                return legacyCount;
            }
        }

        public int ExtendedCount
        {
            get
            {
                ComputeCount(out _, out int extendedCount);
                return extendedCount;
            }
        }

        public override List<byte[]> BeatmapHashes
        {
            get
            {
                List<byte[]> hashes = new List<byte[]>();

                foreach (BeatmapSet beatmapSet in BeatmapSets)
                {
                    hashes.AddRange(beatmapSet.Beatmaps.ConvertAll(b => b.Hash));
                }

                hashes.AddRange(Beatmaps.ConvertAll(b => b.Hash));
                return hashes;
            }
        }

        public ExtendedCollection(Stream stream, string name = null, ProgressContext context = null)
        {
            Name = name;
            SubCollections = new List<Collection>();
            BeatmapSets = new List<BeatmapSet>();
            Beatmaps = new List<ExtendedBeatmap>();
            _fileStream = stream;
            Logging.Log("[Collection] New collection: " + Name, LogLevel.Debug);
            Parse(context);
        }

        public ExtendedCollection(string path, ProgressContext context = null)
            : this(File.OpenRead(path), path.Split(Path.DirectorySeparatorChar).Last().Split('.')[0], context)
        {
        }

        public byte[] SerializeOSDB(ProgressContext context = null)
        {
            Logging.Log("[ExtendedCollection] Serializing " + Name + " (" + ExtendedCount + ")", LogLevel.Debug);
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    var task = context?.AddTask("Serializing " + Name);
                    task?.MaxValue(BeatmapHashes.Count + SubCollections.Count);
                    writer.Write(Name ?? "Unnamed collection");
                    writer.Write(0);
                    writer.Write(BeatmapHashes.Count);
                    foreach (BeatmapSet beatmapSet in BeatmapSets)
                    {
                        writer.Write(beatmapSet.SerializeOSDB());
                        task?.Increment(1);
                    }

                    foreach (ExtendedBeatmap beatmap in Beatmaps)
                    {
                        writer.Write(beatmap.SerializeOSDB());
                        task?.Increment(1);
                    }

                    writer.Write(0);

                    foreach (Collection subCollection in SubCollections)
                    {
                        if (subCollection is ExtendedCollection collection)
                            writer.Write(collection.SerializeOSDB());
                        task?.Increment(1);
                    }
                }

                return memstream.ToArray();
            }
        }

        public override byte[] Serialize(ProgressContext context = null)
        {
            Logging.Log("[ExtendedCollection] Serializing " + Name + " (" + BeatmapHashes.Count + ")", LogLevel.Debug);
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    var task = context?.AddTask("Serializing " + Name);
                    task?.MaxValue(BeatmapHashes.Count + SubCollections.Count);
                    
                    writer.Write((byte) 0x0b);
                    writer.Write(Name ?? "Unnamed collection");
                    writer.Write(BeatmapHashes.Count);
                    foreach (byte[] hash in BeatmapHashes)
                    {
                        string stringHash = BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
                        writer.Write((byte) 0x0b);
                        writer.Write(stringHash);
                        task?.Increment(1);
                    }

                    foreach (Collection collection in SubCollections)
                    {
                        writer.Write(collection.Serialize());
                        task?.Increment(1);
                    }
                }

                return memstream.ToArray();
            }
        }

        public IParsable Parse(ProgressContext context = null)
        {
            using (var archive = GetArchive(_fileStream))
            {
                var task = context?.AddTask("Importing collection " + Name);
                task?.MaxValue(archive.Entries.Count());

                foreach (var entry in archive.Entries)
                {
                    Logging.Log("[ExtendedCollection] Parsing " + entry.Key, LogLevel.Debug);
                    if (entry.Key.EndsWith(".zip") || entry.Key.EndsWith(".7z") || entry.Key.EndsWith(".rar") ||
                        entry.Key.EndsWith(".gz"))
                    {
                        using (MemoryStream memstream = new MemoryStream())
                        {
                            entry.OpenEntryStream().CopyTo(memstream);
                            SubCollections.Add(new ExtendedCollection(memstream,
                                entry.Key.Split(Path.DirectorySeparatorChar).Last().Split('.')[0]));
                        }
                    }
                    else if (entry.Key.EndsWith(".osz"))
                    {
                        string path = Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                                      $@"\osu!\Songs\{entry.Key.Split('/').Last()}";
                        Logging.Log("[ExtendedCollection] Importing to " + path, LogLevel.Debug);
                        entry.WriteToFile(path);
                        using (MemoryStream memstream = new MemoryStream())
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
            }

            return this;
        }

        private IArchive GetArchive(Stream stream)
        {
            MemoryStream zipMemory;
            if (stream is MemoryStream mem)
                zipMemory = new MemoryStream(mem.ToArray());
            else
            {
                zipMemory = new MemoryStream();
                stream.CopyTo(zipMemory);
                stream.Position = 0;
            }

            MemoryStream GZipMemory = new MemoryStream(zipMemory.ToArray());
            MemoryStream sevenZipMemory = new MemoryStream(GZipMemory.ToArray());
            MemoryStream tarMemory = new MemoryStream(sevenZipMemory.ToArray());


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
            foreach (Collection collection in SubCollections)
            {
                if (collection is LegacyCollection)
                    legacyCount++;
                else
                    extendedCount += ((ExtendedCollection) collection).Count;
            }

            return legacyCount + extendedCount;
        }
    }
}