using Spectre.Console;

namespace OsuPackImporter.Interfaces.Serializers
{
    public interface ISerializable
    {
        byte[] Serialize(ProgressContext context = null);
    }
}