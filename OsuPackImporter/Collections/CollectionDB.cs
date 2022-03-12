using System.Collections.Generic;
using System.IO;
using OsuPackImporter.Interfaces.Parsers;
using OsuPackImporter.Interfaces.Serializers;

namespace OsuPackImporter.Collections
{

    public class CollectionDB : IParsable, ISerializable
    {

        private int _version;
        private List<Collection> _collections;

        public IParsable Parse()
        {
            throw new System.NotImplementedException();
        }

        public byte[] Serialize()
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memstream))
                {
                    writer.Write(_version);
                    writer.Write(_collections.Count);
                    foreach (Collection collection in _collections)
                    {
                        writer.Write(collection.Serialize());
                    }
                }

                return memstream.ToArray();
            }
        }
    }
}