using System;
using System.IO;
using OsuPackImporter.Beatmaps;
using OsuPackImporter.Collections;
using Spectre.Console;

namespace OsuPackImporter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = args[0];
            ExtendedCollection collection = new ExtendedCollection(path);
            Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("LOCALAPPDATA") +
                                          Path.DirectorySeparatorChar + "osu!");
            CollectionDB collectionDb = new CollectionDB("collection.db");
            collectionDb.Collections.Add(collection);
            if (File.Exists("collection.db.OLD"))
            {
                File.Delete("collection.db.OLD");
            }
            File.Copy("collection.db", "collection.db.OLD");
            using (FileStream stream = File.Create(@"collection.db"))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(collectionDb.Serialize());
                }
            }
        }
    }
}