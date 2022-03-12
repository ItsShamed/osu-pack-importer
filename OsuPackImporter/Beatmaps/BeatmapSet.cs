using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace OsuPackImporter.Beatmaps
{
    public class BeatmapSet : IParsable, ISerializable, IOSDBSerializable
    {

        public List<ExtendedBeatmap> Beatmaps { get; private set; }
        public string Path { get; set; }

        private BeatmapSet()
        {
            Beatmaps = new List<ExtendedBeatmap>();
        }

        public BeatmapSet(string path) : this()
        {
            Path = path;
            Parse();
        }

        public IParsable Parse()
        {
            if (Path == null)
            {
                throw new NullReferenceException("No path has been assigned yet.");
            }
            try
            {
                using (ZipArchive archive = ZipArchive.Open(Path))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Key.EndsWith(".osu"))
                        {
                            Console.WriteLine(entry.Key);
                            MemoryStream memstream = new MemoryStream();
                            entry.OpenEntryStream().CopyTo(memstream);
                            Beatmaps.Add(ExtendedBeatmapDecoder.Decode(memstream));
                        }
                    }
                }

                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public byte[] Serialize()
        {
            throw new System.NotImplementedException();
        }

        public byte[] SerializeOSDB()
        {
            throw new System.NotImplementedException();
        }
    }
}