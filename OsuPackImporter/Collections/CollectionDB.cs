using System;
using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;
using Spectre.Console;

namespace OsuPackImporter.Collections;

/// <summary>
///     C# representing the collection.db
/// </summary>
public class CollectionDB : IParsable, ISerializable
{
    public static CollectionDB? GetInstance;
    private readonly Stream _fileStream;

    /// <summary>
    ///     Parses a stream containing the collection.db file and generates a <see cref="CollectionDB" /> object from it.
    /// </summary>
    /// <param name="stream">Stream containing a collection.db file</param>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    public CollectionDB(Stream stream, ProgressContext? context = null)
    {
        Collections = new List<Collection>();
        _fileStream = stream;
        Parse(context);
        GetInstance = this;
    }

    /// <summary>
    ///     Parses the collection.db files and generates a <see cref="CollectionDB" /> object from it.
    /// </summary>
    /// <param name="path">Location of the collection.db file</param>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    public CollectionDB(string path, ProgressContext? context = null) : this(File.OpenRead(path), context)
    {
    }

    /// <summary>
    ///     The version of the osu! instance that generated the collection.db file.
    /// </summary>
    public int Version { get; private set; }

    public List<Collection> Collections { get; }

    /// <summary>
    ///     Parses the currently loaded collection.db file and builds the <see cref="CollectionDB" /> object.
    ///     It follows the
    ///     <a href="https://github.com/ppy/osu/wiki/Legacy-database-file-structure#collectiondb-format">following structure</a>
    ///     .
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>This instance of <see cref="CollectionDB" /></returns>
    /// <exception cref="FormatException">
    ///     If the file does not follow the data structure osu! follows, most likely
    ///     the file is corrupted.
    /// </exception>
    public IParsable Parse(ProgressContext? context = null)
    {
        Logging.Log("[CollectionDB] Parsing collection.db", LogLevel.Debug);
        try
        {
            using (var reader = new BinaryReader(_fileStream))
            {
                var task = context?.AddTask("Parsing collection.db");

                Version = reader.ReadInt32();
                var iterations = reader.ReadInt32();
                task?.MaxValue(iterations);

                Logging.Log("[CollectionDB] There are " + iterations + "collections", LogLevel.Debug);
                for (var i = 0; i < iterations; i++)
                {
                    if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string, corrupted file");
                    var collection = new LegacyCollection(reader.ReadString());
                    var beatmapIterations = reader.ReadInt32();
                    Logging.Log("[CollectionDB] Parsing collection " + collection.Name + " (" +
                                beatmapIterations + ")", LogLevel.Debug);

                    task?.MaxValue(task.MaxValue + beatmapIterations);
                    for (var j = 0; j < beatmapIterations; j++)
                    {
                        if (reader.ReadByte() != 0x0b) throw new FormatException("Invalid string, corrupted file");
                        var hex = reader.ReadString();
                        collection.BeatmapHashes.Add(StringToByteArray(hex));
                        task?.Increment(1);
                    }

                    Collections.Add(collection);
                    task?.Increment(1);
                }
            }

            return this;
        }
        catch (Exception e)
        {
            throw new OperationCanceledException("An error occured while parsing your collection.db, it might be corrupted: " + e.Message, e);
        }
    }

    /// <summary>
    ///     Serializes this instance of <see cref="CollectionDB" /> to a byte array using the
    ///     <a href="https://github.com/ppy/osu/wiki/Legacy-database-file-structure#collectiondb-format">
    ///         following
    ///         structure
    ///     </a>
    ///     .
    /// </summary>
    /// <param name="context">Terminal context, used to update the progress bars.</param>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize(ProgressContext? context = null)
    {
        Logging.Log("[CollectionDB] Serializing collection.db", LogLevel.Debug);
        using (var memstream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memstream))
            {
                var task = context?.AddTask("Serializing collection.db");
                task?.MaxValue(Collections.Count);

                writer.Write(Version);
                writer.Write(ComputeCount());
                foreach (var collection in Collections)
                {
                    writer.Write(collection.Serialize());
                    task?.Increment(1);
                }
            }

            return memstream.ToArray();
        }
    }

    // Create a 'CollectionExists' method similar to the one in ExtendedCollection.cs:
    // Check if in the Collection list, there is a collection with the same name as the one given.
    // If a collection is an instance of ExtendedCollection also check if the collection exists in the extended collection with the CollectionExists method
    public bool CollectionExists(string name)
    {
        foreach (var collection in Collections)
        {
            if (collection.Name == name) return true;

            if (collection is ExtendedCollection extendedCollection)
                if (extendedCollection.CollectionExists(name))
                    return true;
        }

        return false;
    }

    ~CollectionDB()
    {
        _fileStream.Dispose();
    }

    /// <summary>
    ///     Converts a hex string to a byte array.
    /// </summary>
    /// <param name="hex">Input hex string</param>
    /// <returns>The byte array constructed from the hex string.</returns>
    private byte[] StringToByteArray(string hex)
    {
        var charCount = hex.Length;
        var bytes = new byte[charCount / 2];
        for (var i = 0; i < charCount; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    private int ComputeCount()
    {
        var count = 0;
        foreach (var collection in Collections) count += collection.Count;

        return count;
    }
}