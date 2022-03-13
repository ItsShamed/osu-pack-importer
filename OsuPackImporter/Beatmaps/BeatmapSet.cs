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
        private Stream _fileStream;
        public List<ExtendedBeatmap> Beatmaps { get; }

        public BeatmapSet(Stream fileStream)
        {
            Beatmaps = new List<ExtendedBeatmap>();
            _fileStream = fileStream;
            Parse();
        }

        public BeatmapSet(string path) : this(File.OpenRead(path))
        {}

        public IParsable Parse()
        {
            try
            {
                using (ZipArchive archive = ZipArchive.Open(_fileStream))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Key.EndsWith(".osu"))
                        {
                            MemoryStream memstream = new MemoryStream();
                            entry.OpenEntryStream().CopyTo(memstream);
                            Beatmaps.Add(ExtendedBeatmapDecoder.Decode(memstream));
                            memstream.Dispose();
                        }
                    }
                }

                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return this;
            }
        }

        public IParsable Parse(Stream stream)
        {
            _fileStream = stream;
            return Parse();
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