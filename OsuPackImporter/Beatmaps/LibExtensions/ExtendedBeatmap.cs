using System;
using System.IO;
using System.Security.Cryptography;
using OsuPackImporter.Interfaces.Serializers;
using OsuParsers.Beatmaps;
using Spectre.Console;

namespace OsuPackImporter.Beatmaps.LibExtensions;

/// <summary>
///     This class is the serializable version of the <see cref="Beatmap" /> class.
/// </summary>
public class ExtendedBeatmap : Beatmap, IOSDBSerializable
{
    /// <summary>
    ///     Instantiates a new ExtendedBeatmap object from an existing Beatmap object.
    /// </summary>
    /// <param name="beatmap">Original beatmap object</param>
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

    /// <summary>
    ///     Serialize the beatmap as its MD5 hash.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>The beatmap's MD5 hash.</returns>
    public byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log(
            $"[ExtendedBeatmap] Serializing beatmap {MetadataSection.ArtistUnicode} - {MetadataSection.TitleUnicode} [{MetadataSection.Version}]...",
            LogLevel.Debug);
        using var md5 = MD5.Create();
        return md5.ComputeHash(Hash!);
    }

    /// <summary>
    ///     Serializes the beatmap in the OSDB format documented
    ///     <a href="https://gist.github.com/ItsShamed/c3c6c83903653d72d1f499d7059fe185#beatmap-format">here</a>.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>The serialized data.</returns>
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