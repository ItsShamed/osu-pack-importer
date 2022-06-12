using Spectre.Console;

namespace OsuPackImporter
{
    public static class Logging
    {
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            message = message.Replace("[", "[[").Replace("]", "]]");
            switch (level)
            {
                case LogLevel.Debug:
                    if (Program.Verbose) AnsiConsole.MarkupLine("[italics][gray]DEBUG: [/][dim]" + message + "[/][/]");
                    break;
                case LogLevel.Info:
                    AnsiConsole.MarkupLine("[underline]INFO:[/] " + message);
                    break;
                case LogLevel.Warn:
                    AnsiConsole.MarkupLine("[yellow][underline]WARN:[/] " + message + "[/]");
                    break;
                case LogLevel.Error:
                    AnsiConsole.MarkupLine("[red][underline bold]ERROR:[/] " + message + "[/]");
                    break;
                case LogLevel.Fatal:
                    AnsiConsole.MarkupLine("[red bold][invert underline]FATAL:[/] " + message + "[/]");
                    break;
            }
        }
    }
}