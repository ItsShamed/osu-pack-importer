using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Interfaces.Serializers;
using Spectre.Console;

namespace OsuPackImporter.Collections;

/// <summary>
///     C# object representing an osu! collection.
/// </summary>
public abstract class Collection : ISerializable
{
    public string? Name { get; protected set; }
    public abstract List<byte[]> BeatmapHashes { get; }

    public abstract int Count { get; }

    /// <summary>
    ///     Serializes the collection as a byte array following the format provided
    ///     <a href="https://github.com/ppy/osu/wiki/Legacy-database-file-structure#collectiondb-format">here</a>.
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>Byte array containing the serialized data</returns>
    public virtual byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log("[Collection] Serializing " + Name + " (" + BeatmapHashes.Count + ")", LogLevel.Debug);

        using var memstream = new MemoryStream();
        using (var writer = new BinaryWriter(memstream))
        {
            var task = context?.AddTask("Serializing collection " + Name);
            task?.MaxValue(BeatmapHashes.Count);
            writer.Write((byte) 0x0b);
            writer.Write(Name ?? "Unnamed collection");
            writer.Write(BeatmapHashes.Count);
            foreach (var hash in BeatmapHashes)
            {
                var beatmapHash = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                writer.Write((byte) 0x0b);
                writer.Write(beatmapHash);
                task?.Increment(1);
            }
        }

        return memstream.ToArray();
    }

    /// <summary>
    ///     Prompts for collection renaming. Stay unchanged if no input is given.
    /// </summary>
    public virtual void Rename()
    {
        var newName = AnsiConsole.Ask($"Rename collection {Name} [gray](leave empty for default)[/]",
            Name!);
        if (!string.IsNullOrWhiteSpace(newName)) Name = newName;
    }
}