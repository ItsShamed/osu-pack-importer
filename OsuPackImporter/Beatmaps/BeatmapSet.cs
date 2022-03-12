using System.Collections.Generic;
using OsuPackImporter.Beatmaps.LibExtensions;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;

namespace OsuPackImporter.Beatmaps
{
    public class BeatmapSet : IParsable, ISerializable, IOSDBSerializable
    {

        public List<ExtendedBeatmap> Beatmaps { get; private set; }
        public string Path { get; set; }

        public BeatmapSet()
        {
            Beatmaps = new List<ExtendedBeatmap>();
        }

        public BeatmapSet(string path) : this()
        {
            Path = path;
        }

        public IParsable Parse()
        {
            throw new System.NotImplementedException();
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