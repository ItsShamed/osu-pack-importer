namespace OsuPackImporter.Interfaces.Serializers
{
    public interface IOSDBSerializable : ISerializable
    {
        byte[] SerializeOSDB();
    }
}