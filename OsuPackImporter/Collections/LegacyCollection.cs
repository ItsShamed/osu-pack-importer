using System.Collections.Generic;

namespace OsuPackImporter.Collections
{
    public class LegacyCollection : Collection
    {
        public sealed override List<byte[]> BeatmapHashes { get; }

        public LegacyCollection(string name)
        {
            Name = name;
            BeatmapHashes = new List<byte[]>();
        }
    }
}