
using Serilog.Core;
using Serilog.Events;
using Microsoft.AspNetCore.SignalR;
using Serilog.Configuration;
using Example.API.Controllers;
using Example.API.Configuration;

namespace Example.API.Configuration;
public static class SerilogSignalRSinkExtensions
{
    public static LoggerConfiguration SignalRLogger(this LoggerSinkConfiguration loggerConfiguration, Func<IHubContext<LogHub>> hubContextProvider, IFormatProvider formatProvider = null!)
    {
        return loggerConfiguration.Sink(new SignalRLogger(hubContextProvider));
    }

    internal static IApplicationBuilder UseSerilogSignalRSink(this IApplicationBuilder app, IConfiguration configuration)
    {       

        // Re-initialize serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, sectionName: "Serilog")
            .WriteTo.Sink(new SignalRLogger(app.ApplicationServices.GetRequiredService<IHubContext<LogHub>>, "Information"))            
            .CreateLogger();

        return app;
    }
}

public class SignalRLogger : ILogEventSink
{
    private LogEventLevel _level { get; set; }

    private readonly Func<IHubContext<LogHub>> _hubContextProvider;

    public SignalRLogger(Func<IHubContext<LogHub>> hubContextProvider, string level = "Error")
    {
        _hubContextProvider = hubContextProvider;
        _level = LevelFromString(level);
    }

    public void Emit(LogEvent logEvent)
    {
        if ((int)logEvent.Level < (int)_level) return;

        IHubContext<LogHub> _hubContext = _hubContextProvider.Invoke();

        if (_hubContext == null) return;

        var t = logEvent.Timestamp;

        LogMsg msg = new LogMsg()
        {
            Text = logEvent.RenderMessage(),
            Lvl = LevelToSeverity(logEvent),
            TimeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")
        };

        _hubContext.Clients.All.SendAsync("logMessage", $"$ {msg.TimeStamp}-{msg.Lvl} {msg.Text} ");
    }

    /// <summary>
    /// Verbose -> Debug -> Info -> Warn -> Error -> Fatal
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    static string LevelToSeverity(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Debug:
                return "[DBG]";
            case LogEventLevel.Error:
                return "[ERR]";
            case LogEventLevel.Fatal:
                return "[FTL]";
            case LogEventLevel.Verbose:
                return "VERBOSE";
            case LogEventLevel.Warning:
                return "WARNING";
            default:
                return "[INF]";
        }
    }

    static LogEventLevel LevelFromString(string level)
    {
        switch (level.Trim().ToUpperInvariant())
        {
            case "DEBUG":
                return LogEventLevel.Debug;
            case "INFORMATION":
                return LogEventLevel.Information;
            case "FATAL":
                return LogEventLevel.Fatal;
            case "VERBOSE":
                return LogEventLevel.Verbose;
            case "WARNING":
                return LogEventLevel.Warning;
            default:
                return LogEventLevel.Error;
        }
    }
}

struct LogMsg
{
    public string Lvl;
    public string Text;
    public string TimeStamp;
}

