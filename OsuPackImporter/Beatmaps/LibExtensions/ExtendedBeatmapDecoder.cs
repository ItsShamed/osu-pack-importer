using System.IO;
using System.Security.Cryptography;
using OsuParsers.Decoders;

namespace OsuPackImporter.Beatmaps.LibExtensions;

/// <summary>
/// This class contains the decoding methods for the <see cref="ExtendedBeatmap"/> class.
/// </summary>
public static class ExtendedBeatmapDecoder
{
    /// <summary>
    /// Creates a new <see cref="ExtendedBeatmap"/> from an .osu file at the given path.
    /// </summary>
    /// <param name="path">Path of the .osu beatmap file.</param>
    /// <returns>The <see cref="ExtendedBeatmap"/> instance generated from the .osu file.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exists.</exception>
    public static ExtendedBeatmap Decode(string path)
    {
        if (File.Exists(path))
        {
            var beatmap = new ExtendedBeatmap(BeatmapDecoder.Decode(path));
            using (var md5 = MD5.Create())
            {
                using (Stream stream = File.OpenRead(path))
                {
                    beatmap.Hash = md5.ComputeHash(stream);
                }
            }

            return beatmap;
        }

        throw new FileNotFoundException();
    }

    /// <summary>
    /// Creates a new <see cref="ExtendedBeatmap"/> from a stream containing the contents of a .osu file.
    /// </summary>
    /// <param name="stream">Stream containing a .osu file</param>
    /// <returns>The <see cref="ExtendedBeatmap"/> instance generated from the .osu file</returns>
    public static ExtendedBeatmap Decode(Stream stream)
    {
        var cachedStream = new MemoryStream();
        if (stream is MemoryStream memStream)
            cachedStream = new MemoryStream(memStream.ToArray());
        else
            stream.CopyTo(cachedStream);
        stream.Position = 0;

        var beatmap = new ExtendedBeatmap(BeatmapDecoder.Decode(stream));
        using (var md5 = MD5.Create())
        {
            beatmap.Hash = md5.ComputeHash(cachedStream.ToArray());
        }

        cachedStream.Dispose();
        return beatmap;
    }
}