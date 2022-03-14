using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OsuPackImporter.Interfaces.Serializers;

namespace OsuPackImporter.Collections
{
    public abstract class Collection : ISerializable
    {
        public string Name { get; protected set; }
        public abstract List<byte[]> BeatmapHashes { get; }

        public abstract int Count { get; }

        public virtual byte[] Serialize()
        {
            Console.WriteLine("[Collection] Serializing " + Name + " (" + BeatmapHashes.Count + ")");
            UnicodeEncoding uni = new UnicodeEncoding();
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    writer.Write((byte) 0x0b);
                    writer.Write(Name ?? "Unnamed collection");
                    writer.Write(BeatmapHashes.Count);
                    foreach (byte[] hash in BeatmapHashes)
                    {
                        writer.Write((byte) 0x0b);
                        writer.Write(BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant());
                    }
                }

                return memstream.ToArray();
            }
        }
    }
}