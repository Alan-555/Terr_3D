namespace Terr3D.Utils;

/// <summary>
/// Class that provides utility functions for logging to the console
/// </summary>
public static class Diagnostics
{
    public static void Info(string message, params object[] args) =>
        Log(LogLevel.INFO, message, args);

    public static void Warn(string message, params object[] args) =>
        Log(LogLevel.WARN, message, args);

    public static void Error(string message, params object[] args) =>
        Log(LogLevel.ERROR, message, args);

    public static void Debug(string message, params object[] args)
    {
        if(Program.DEBUG_FLAG)
            Log(LogLevel.DEBUG, message, args);
    }



    private static void Log(LogLevel level, string message, params object[] args)
    {

        TextWriter output = Console.Out;
        if(level == LogLevel.ERROR)
        {
            output = Console.Error;
        }
        var time = DateTime.Now.ToLongTimeString();
        output.WriteLine($"{time} [{level}] -> {message}");
        if(args.Length>0)
            output.WriteLine(args);
    }


}


enum LogLevel
{
    INFO,
    WARN,
    ERROR,
    DEBUG
}