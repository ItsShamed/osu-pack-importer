using System.Collections.Generic;

namespace OsuPackImporter.Collections;

public class LegacyCollection : Collection
{
    public LegacyCollection(string? name)
    {
        Name = name;
        BeatmapHashes = new List<byte[]>();
    }

    public sealed override List<byte[]> BeatmapHashes { get; }

    public override int Count => 1;
}