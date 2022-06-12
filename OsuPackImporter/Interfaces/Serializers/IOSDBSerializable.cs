using Spectre.Console;

namespace OsuPackImporter.Interfaces.Serializers
{
    public interface IOSDBSerializable : ISerializable
    {
        byte[] SerializeOSDB(ProgressContext context = null);
    }
}