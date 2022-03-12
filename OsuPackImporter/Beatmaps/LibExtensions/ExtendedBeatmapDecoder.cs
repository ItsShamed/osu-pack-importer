using System.IO;
using System.Security.Cryptography;
using OsuParsers.Decoders;

namespace OsuPackImporter.Beatmaps.LibExtensions
{
    public static class ExtendedBeatmapDecoder
    {
        public static ExtendedBeatmap Decode(string path)
        {
            if (File.Exists(path))
            {
                ExtendedBeatmap beatmap = new ExtendedBeatmap(BeatmapDecoder.Decode(path));
                using (MD5 md5 = MD5.Create())
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
    }
}