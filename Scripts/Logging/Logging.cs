using System;
using System.IO;
using System.Runtime.CompilerServices;
using UniRx;


namespace ReactiveConsole
{
    public enum LogLevel
    {
        Error, // red
        Warning, // yellow
        Info, // white
        Debug // gray
    }

    [Serializable]
    public struct LogEntry
    {
        public double UnixTime;
        public LogLevel LogLevel;
        public string Message;
        public string CallerFile;
        public int CallerLine;
        public string CallerMember;

        public LogEntry(LogLevel logLevel,
            string message,
            string callerFile,
            int callerLine,
            string callerMember)
        {
            UnixTime = Now();
            LogLevel = logLevel;
            Message = message;
            CallerFile = callerFile;
            CallerLine = callerLine;
            CallerMember = callerMember;
        }

        public const long TicksPerSecond = 10000000;
        static double Now()
        {
            return (double)(DateTimeOffset.UtcNow.Ticks / TicksPerSecond);
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}: {2}: {3} => {4}",
                LogLevel,
                Path.GetFileName(CallerFile),
                CallerLine,
                CallerMember,
                Message);
        }       
    }

    public static class Logging
    {
        static Subject<LogEntry> s_subject = new Subject<LogEntry>();
        public static IObservable<LogEntry> Observable
        {
            get { return s_subject.AsObservable(); }
        }

        public static void Dispose()
        {
            s_subject.Dispose();
        }

        public static void Exception(Exception ex,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = ""
            )
        {
            Error(ex.Message);
        }

        public static void Error(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = ""
            )
        {
            s_subject.OnNext(new LogEntry(LogLevel.Error, message, file, line, member));
        }

        public static void Warning(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = ""
            )
        {
            s_subject.OnNext(new LogEntry(LogLevel.Warning, message, file, line, member));
        }

        public static void Info(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = ""
            )
        {
            s_subject.OnNext(new LogEntry(LogLevel.Info, message, file, line, member));
        }

        public static void Debug(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = ""
            )
        {
            s_subject.OnNext(new LogEntry(LogLevel.Debug, message, file, line, member));
        }
    }
}
