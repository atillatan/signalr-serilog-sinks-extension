global using Serilog;
using Example.API.Configuration;
using Example.API.Controllers;

var appSettings = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(appSettings, sectionName: "Serilog").CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR(hubOptions => { hubOptions.EnableDetailedErrors = true; });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogSignalRSink(app.Configuration);
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "index.html", "default.html" } });
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<LogHub>("/log");
});

app.Run();
