﻿using System;
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

        public byte[] Serialize()
        {
            UnicodeEncoding uni = new UnicodeEncoding();
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    writer.Write(Name);
                    writer.Write(BeatmapHashes.Count);
                    foreach (byte[] hash in BeatmapHashes)
                    {
                        writer.Write(BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant());
                    }
                }

                return memstream.ToArray();
            }
        }
    }
}