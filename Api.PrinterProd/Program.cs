using Api.PrinterProd.Configuration;
using Api.PrinterProd.Services.api_data;
using Api.PrinterProd.Services.Labels;
using Api.PrinterProd.Services.Printer;
using Api.PrinterProd.Services.Template;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory); // S'assure que le répertoire existe

var logFilePrefix = builder.Configuration.GetRequiredString("Application:LogFilePrefix");
var retainedFileCountLimit = builder.Configuration.GetRequiredInt("Application:LogRetainedFileCount");
var logFileName = $"{logFilePrefix}_{DateTime.Now:yyyy-MM-dd}.log";
var logFilePath = Path.Combine(logDirectory, logFileName);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(
        path: logFilePath,
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: retainedFileCountLimit,
        shared: true
    )
    .CreateLogger();


builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<IApiData, ApiData>();
builder.Services.AddSingleton<ILabelTemplateProcessor, LabelTemplateProcessor>();
builder.Services.AddSingleton<ILabelsCreator, LabelsCreator>();
builder.Services.AddSingleton<IPrinter, Printer>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// Log that the application is starting
Log.Information("Application is starting");

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors(); // Utiliser CORS

app.UseAuthorization();

app.MapControllers();

// Log that the application has started successfully
Log.Information("Application has started successfully");

app.Run();

// Ensure to flush and close the logger when the application shuts down
Log.CloseAndFlush();
