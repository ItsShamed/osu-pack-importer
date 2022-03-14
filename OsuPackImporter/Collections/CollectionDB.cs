using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;

namespace OsuPackImporter.Collections
{
    public class CollectionDB : IParsable, ISerializable
    {
        private int _version;
        private Stream _fileStream;

        public int Version => _version;
        public List<Collection> Collections { get; }

        public CollectionDB(Stream stream)
        {
            Collections = new List<Collection>();
            _fileStream = stream;
            Parse();
        }

        public CollectionDB(string path) : this(File.OpenRead(path))
        {
        }

        ~CollectionDB()
        {
            _fileStream.Dispose();
        }

        public IParsable Parse()
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(_fileStream))
                {
                    _version = reader.ReadInt32();
                    int iterations = reader.ReadInt32();
                    Console.WriteLine(iterations + " iterations");
                    for (int i = 0; i < iterations; i++)
                    {
                        if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string");
                        LegacyCollection collection = new LegacyCollection(reader.ReadString());
                        Console.WriteLine(collection.Name);
                        int beatmapIterations = reader.ReadInt32();
                        Console.WriteLine(beatmapIterations + " beatmap iterations");
                        for (int j = 0; j < beatmapIterations; j++)
                        {
                            if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string");
                            string hex = reader.ReadString();
                            Console.WriteLine(hex);
                            collection.BeatmapHashes.Add(StringToByteArray(hex));
                        }

                        Collections.Add(collection);
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

        public byte[] Serialize()
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    writer.Write(_version);
                    writer.Write(ComputeCount());
                    foreach (Collection collection in Collections)
                    {
                        writer.Write(collection.Serialize());
                    }
                }

                return memstream.ToArray();
            }
        }

        private byte[] StringToByteArray(string hex)
        {
            int charCount = hex.Length;
            byte[] bytes = new byte[charCount / 2];
            for (int i = 0; i < charCount; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private int ComputeCount()
        {
            int count = 0;
            foreach (Collection collection in Collections)
            {
                count += collection.Count;
            }

            return count;
        }
    }
}