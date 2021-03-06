# signalr-serilog-sinks-extension



- Configuration


```csharp
// Program.cs
...
builder.Host.UseSerilog();
builder.Services.AddSignalR(hubOptions => { hubOptions.EnableDetailedErrors = true; });
...

// Configure the HTTP request pipeline.
app.UseSerilogSignalRSink(app.Configuration);
app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "index.html", "default.html" } });
app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<LogHub>("/log");
});
app.Run();

```


- Add Custom Serilog ILogEventSink

```csharp
// SerilogSignalRSinkExtensions.cs

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
```


- Use wwwroot/log.html to see logs.