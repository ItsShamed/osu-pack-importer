using System.Collections.Generic;
using OsuPackImporter.Beatmaps;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;

namespace OsuPackImporter.Collections
{
    public class ExtendedCollection : Collection, IOSDBSerializable, IParsable
    {

        public string Path { get; }

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

        public ExtendedCollection()
        {
            SubCollections = new List<Collection>();
            BeatmapSets = new List<BeatmapSet>();
            Beatmaps = new List<ExtendedBeatmap>();
        }

        public ExtendedCollection(string path) : this()
        {
            Path = path;
        }

        public byte[] SerializeOSDB()
        {
            throw new System.NotImplementedException();
        }

        public IParsable Parse()
        {
            throw new System.NotImplementedException();
        }
    }
}