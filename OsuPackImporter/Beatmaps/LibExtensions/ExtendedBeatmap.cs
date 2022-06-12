using System;
using System.IO;
using System.Security.Cryptography;
using OsuPackImporter.Interfaces.Serializers;
using OsuParsers.Beatmaps;
using Spectre.Console;

namespace OsuPackImporter.Beatmaps.LibExtensions;

public class ExtendedBeatmap : Beatmap, IOSDBSerializable
{
    public ExtendedBeatmap(Beatmap beatmap)
    {
        Version = beatmap.Version;
        GeneralSection = beatmap.GeneralSection;
        EditorSection = beatmap.EditorSection;
        MetadataSection = beatmap.MetadataSection;
        EventsSection = beatmap.EventsSection;
        ColoursSection = beatmap.ColoursSection;
    }

    public byte[]? Hash { get; set; }

    public byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log(
            $"[ExtendedBeatmap] Serializing beatmap {MetadataSection.ArtistUnicode} - {MetadataSection.TitleUnicode} [{MetadataSection.Version}]...",
            LogLevel.Debug);
        using var md5 = MD5.Create();
        return md5.ComputeHash(Hash!);
    }

    public byte[] SerializeOSDB(ProgressContext? context = null)
    {
        // https://gist.github.com/ItsShamed/c3c6c83903653d72d1f499d7059fe185#beatmap-format

        Logging.Log(
            $"[ExtendedBeatmap] Serializing beatmap {MetadataSection.ArtistUnicode} - {MetadataSection.TitleUnicode} [{MetadataSection.Version}]...",
            LogLevel.Debug);
        using (var memstream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memstream))
            {
                writer.Write(MetadataSection.BeatmapID);
                writer.Write(MetadataSection.BeatmapSetID);
                writer.Write(MetadataSection.Artist);
                writer.Write(MetadataSection.Title);
                writer.Write(MetadataSection.Version);
                writer.Write(BitConverter.ToString(Hash!).Replace("-", string.Empty).ToLowerInvariant());
                writer.Write("");
                writer.Write((byte) GeneralSection.ModeId);
                writer.Write((double) DifficultySection.OverallDifficulty);
            }

            return memstream.ToArray();
        }
    }
}