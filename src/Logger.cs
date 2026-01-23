// Taken from my bittorrent project

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public sealed class Logger
{
    const string TIME_FORMAT = "HH:mm:ss.ffff";
    private readonly static string projectName =
        Assembly.GetCallingAssembly().GetName().Name!;

    public readonly static string DefaultInfoLogFile = $"{projectName}-info.log";
    public readonly static string DefaultErrorLogFile = $"{projectName}-error.log";

    private static Logger? _instance;

    private FileStream _infoLogFile;
    private FileStream _errorLogFile;
    private StreamWriter _infoWriter;
    private StreamWriter _errorWriter;

    private Logger()
    {
        var config = Config.Get();

        _infoLogFile = File.Open(config.InfoLogFile, FileMode.Append);
        _errorLogFile = File.Open(config.ErrorLogFile, FileMode.Append);

        _infoWriter = new StreamWriter(_infoLogFile);
        _errorWriter = new StreamWriter(_errorLogFile);
    }

    private static Logger Get()
    {
        _instance ??= new Logger();
        return _instance!;
    }

    private static string BuildMessage(string message, string member, int lineNumber)
    {
        var frames = new StackTrace().GetFrames();
        var frame = Array.Find(frames,
            fr => fr.GetMethod()?.DeclaringType != typeof(Logger))!;
        var method = frame.GetMethod()!;

        Type? classType = method.IsDefined(typeof(ExtensionAttribute))
            ? method.GetParameters()[0]?.ParameterType
            : method.DeclaringType;

        while (classType?.IsNested ?? false && classType?.DeclaringType is not null)
            classType = classType.DeclaringType;

        var now = DateTime.Now.ToString(TIME_FORMAT);
        var className = classType?.FullName ?? "<unknown>";

        return $"[{now}] {className}.{member}:{lineNumber} - {message}";
    }

    public static void Log(
        LogLevel level,
        string message,
        [CallerMemberName] string member = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var logger = Get();
        var logMessage = BuildMessage(message, member, lineNumber);

        Console.ForegroundColor = level.ToColor();
        Console.WriteLine(logMessage);
        Console.ResetColor();

        var writer = level == LogLevel.Error
            ? logger._errorWriter
            : logger._infoWriter;

        writer.WriteLine(logMessage);
        writer.Flush();
    }

    public static async Task LogAsync(
        LogLevel level,
        string message,
        CancellationTokenSource? tokenSource = null,
        [CallerMemberName] string member = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        tokenSource ??= new CancellationTokenSource();
        var logger = Get();
        var logMessage = BuildMessage(message, member, lineNumber);

        Console.ForegroundColor = level.ToColor();
        Console.Error.WriteLine(logMessage);
        Console.ResetColor();

        var writer = level == LogLevel.Error
            ? logger._errorWriter
            : logger._infoWriter;

        await writer.WriteLineAsync(logMessage.AsMemory(), tokenSource.Token);
        await writer.FlushAsync(tokenSource.Token);
    }

    public static void Debug(string message, [CallerMemberName] string m = "", [CallerLineNumber] int l = 0)
        => Log(LogLevel.Info, message, m, l);

    public static void Info(string message, [CallerMemberName] string m = "", [CallerLineNumber] int l = 0)
        => Log(LogLevel.Info, message, m, l);

    public static void Warn(string message, [CallerMemberName] string m = "", [CallerLineNumber] int l = 0)
        => Log(LogLevel.Warning, message, m, l);

    public static void Error(string message, [CallerMemberName] string m = "", [CallerLineNumber] int l = 0)
        => Log(LogLevel.Error, message, m, l);

    ~Logger()
    {
        _infoWriter.Close();
        _errorWriter.Close();
        _infoLogFile.Close();
        _errorLogFile.Close();
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

public static class LogLevelExtensions
{
    public static ConsoleColor ToColor(this LogLevel level) => level switch
    {
        LogLevel.Debug | LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.White,
    };
}
