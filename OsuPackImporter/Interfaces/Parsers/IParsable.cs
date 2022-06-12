using Spectre.Console;

namespace OsuPackImporter.Interfaces.Parsers;

public interface IParsable
{
    IParsable Parse(ProgressContext? context = null);
}