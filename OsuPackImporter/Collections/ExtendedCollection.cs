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

namespace OsuPackImporter.Collections
{
    public class ExtendedCollection : Collection, IOSDBSerializable, IParsable
    {
        private Stream _fileStream;

        public List<Collection> SubCollections { get; }
        public List<BeatmapSet> BeatmapSets { get; }

        public List<ExtendedBeatmap> Beatmaps { get; }

        public override List<byte[]> BeatmapHashes
        {
            get
            {
                List<byte[]> hashes = new List<byte[]>();
                foreach (Collection subCollection in SubCollections)
                {
                    hashes.AddRange(subCollection.BeatmapHashes);
                }

                foreach (BeatmapSet beatmapSet in BeatmapSets)
                {
                    hashes.AddRange(beatmapSet.Beatmaps.ConvertAll(b => b.Hash));
                }

                hashes.AddRange(Beatmaps.ConvertAll(b => b.Hash));
                return hashes;
            }
        }

        public ExtendedCollection(Stream stream, string name = null)
        {
            Name = name;
            SubCollections = new List<Collection>();
            BeatmapSets = new List<BeatmapSet>();
            Beatmaps = new List<ExtendedBeatmap>();
            _fileStream = stream;
            Console.WriteLine("[Collection] New collection: " + Name);
            Parse();
        }

        public ExtendedCollection(string path)
            : this(File.OpenRead(path), path.Split(Path.DirectorySeparatorChar).Last().Split('.')[0])
        {
        }

        public byte[] SerializeOSDB()
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    writer.Write(Name ?? "Unnamed collection");
                    writer.Write(0);
                    writer.Write(BeatmapHashes.Count);
                    foreach (BeatmapSet beatmapSet in BeatmapSets)
                    {
                        writer.Write(beatmapSet.SerializeOSDB());
                    }

                    foreach (ExtendedBeatmap beatmap in Beatmaps)
                    {
                        writer.Write(beatmap.SerializeOSDB());
                    }

                    writer.Write(0);
                }

                return memstream.ToArray();
            }
        }

        public IParsable Parse()
        {
            using (var archive = GetArchive(_fileStream))
            {
                foreach (var entry in archive.Entries)
                {
                    Console.WriteLine("[Collection] Parsing " + entry.Key);
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
                        using (MemoryStream memstream = new MemoryStream())
                        {
                            entry.OpenEntryStream().CopyTo(memstream);
                            BeatmapSets.Add(new BeatmapSet(memstream));
                        }
                    }
                    else if (entry.Key.EndsWith(".osu"))
                    {
                        Console.Write("[Collection] Adding beatmap");
                        Beatmaps.Add(ExtendedBeatmapDecoder.Decode(entry.OpenEntryStream()));
                    }
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
                Console.WriteLine("[Collection] RAR Archive");
                return RarArchive.Open(stream);
            }

            stream.Position = 0;

            if (ZipArchive.IsZipFile(stream))
            {
                Console.WriteLine("[Collection] Zip Archive");
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
                Console.WriteLine("[Collection] 7Zip Archive");
                sevenZipMemory.Dispose();
                GZipMemory.Dispose();
                tarMemory.Dispose();
                return SevenZipArchive.Open(stream);
            }

            sevenZipMemory.Dispose();
            stream.Position = 0;

            if (GZipArchive.IsGZipFile(GZipMemory))
            {
                Console.WriteLine("[Collection] GZip Archive");
                GZipMemory.Dispose();
                tarMemory.Dispose();
                return GZipArchive.Open(stream);
            }

            GZipMemory.Dispose();
            stream.Position = 0;

            if (TarArchive.IsTarFile(tarMemory))
            {
                Console.WriteLine("[Collection] Tar Archive");
                tarMemory.Dispose();
                return TarArchive.Open(stream);
            }

            tarMemory.Dispose();
            Console.WriteLine("[Collection] Unknown input file");
            return null;
        }
    }
}