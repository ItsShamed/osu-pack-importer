using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using Spectre.Console;

namespace OsuPackImporter.Collections
{
    public class CollectionDB : IParsable, ISerializable
    {
        private int _version;
        private Stream _fileStream;

        public int Version => _version;
        public List<Collection> Collections { get; }

        public CollectionDB(Stream stream, ProgressContext context = null)
        {
            Collections = new List<Collection>();
            _fileStream = stream;
            Parse(context);
        }

        public CollectionDB(string path, ProgressContext context = null) : this(File.OpenRead(path), context)
        {
        }

        ~CollectionDB()
        {
            _fileStream.Dispose();
        }

        public IParsable Parse(ProgressContext context = null)
        {
            Logging.Log("[CollectionDB] Parsing collection.db", LogLevel.Debug);
            try
            {
                using (BinaryReader reader = new BinaryReader(_fileStream))
                {
                    var task = context?.AddTask("Parsing collection.db");
                    
                    _version = reader.ReadInt32();
                    int iterations = reader.ReadInt32();
                    task?.MaxValue(iterations);
                    
                    Logging.Log("[CollectionDB] There are " + iterations + "collections", LogLevel.Debug);
                    for (int i = 0; i < iterations; i++)
                    {
                        if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string, corrupted file");
                        LegacyCollection collection = new LegacyCollection(reader.ReadString());
                        int beatmapIterations = reader.ReadInt32();
                        Logging.Log("[CollectionDB] Parsing collection " + collection.Name + " (" +
                                          beatmapIterations + ")", LogLevel.Debug);
                        
                        task?.MaxValue(task.MaxValue + beatmapIterations);
                        for (int j = 0; j < beatmapIterations; j++)
                        {
                            if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string, corrupted file");
                            string hex = reader.ReadString();
                            collection.BeatmapHashes.Add(StringToByteArray(hex));
                            task?.Increment(1);
                        }

                        Collections.Add(collection);
                        task?.Increment(1);
                    }
                }

                return this;
            }
            catch (Exception e)
            {
                Logging.Log("An error occured while parsing your collection.db, it might be corrupted: ");
                AnsiConsole.WriteException(e);
                return this;
            }
        }

        public byte[] Serialize(ProgressContext context = null)
        {
            Logging.Log("[CollectionDB] Serializing collection.db", LogLevel.Debug);
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    var task = context?.AddTask("Serializing collection.db");
                    task?.MaxValue(Collections.Count);
                    
                    writer.Write(_version);
                    writer.Write(ComputeCount());
                    foreach (Collection collection in Collections)
                    {
                        writer.Write(collection.Serialize());
                        task?.Increment(1);
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