using OsuPackImporter.Interfaces.Serializers;
using OsuParsers.Beatmaps;

namespace OsuPackImporter.Beatmaps.LibExtensions
{
    public class ExtendedBeatmap : Beatmap, ISerializable, IOSDBSerializable
    {
        public byte[] Hash { get; set; }

        public ExtendedBeatmap(Beatmap beatmap)
        {
            Version = beatmap.Version;
            GeneralSection = beatmap.GeneralSection;
            EditorSection = beatmap.EditorSection;
            MetadataSection = beatmap.MetadataSection;
            EventsSection = beatmap.EventsSection;
            ColoursSection = beatmap.ColoursSection;
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