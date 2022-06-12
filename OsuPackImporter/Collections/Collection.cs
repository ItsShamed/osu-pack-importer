using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OsuPackImporter.Interfaces.Serializers;
using Spectre.Console;

namespace OsuPackImporter.Collections
{
    public abstract class Collection : ISerializable
    {
        public string Name { get; protected set; }
        public abstract List<byte[]> BeatmapHashes { get; }

        public abstract int Count { get; }

        public virtual byte[] Serialize(ProgressContext context = null)
        {
            Logging.Log("[Collection] Serializing " + Name + " (" + BeatmapHashes.Count + ")", LogLevel.Debug);
            UnicodeEncoding uni = new UnicodeEncoding();
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    var task = context?.AddTask("Serializing collection " + Name);
                    task?.MaxValue(BeatmapHashes.Count);
                    writer.Write((byte) 0x0b);
                    writer.Write(Name ?? "Unnamed collection");
                    writer.Write(BeatmapHashes.Count);
                    foreach (byte[] hash in BeatmapHashes)
                    {
                        string beatmapHash = BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
                        writer.Write((byte) 0x0b);
                        writer.Write(beatmapHash);
                        task?.Increment(1);
                    }
                }

                return memstream.ToArray();
            }
        }
    }
}