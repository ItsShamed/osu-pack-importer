using System.Collections.Generic;

namespace OsuPackImporter.Collections;

/// <summary>
///     This class is the representation of the legacy collection format.
/// </summary>
public class LegacyCollection : Collection
{
    /// <summary>
    ///     Create a new legacy collection.
    /// </summary>
    /// <param name="name"></param>
    public LegacyCollection(string? name)
    {
        Name = name;
        BeatmapHashes = new List<byte[]>();
    }

    public sealed override List<byte[]> BeatmapHashes { get; }

    public override int Count => 1;
}