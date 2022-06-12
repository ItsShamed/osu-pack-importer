using System.IO;
using System.Security.Cryptography;
using OsuParsers.Decoders;

namespace OsuPackImporter.Beatmaps.LibExtensions;

public static class ExtendedBeatmapDecoder
{
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