using api_lo_etiq.Services.api_data;
using api_lo_etiq.Services.Labels;
using api_lo_etiq.Services.Printer;
using api_lo_etiq.Services.Template;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File("logs/logs-api-lo-etiq.log")
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(); // Utiliser CORS

app.UseAuthorization();

app.MapControllers();

// Log that the application has started successfully
Log.Information("Application has started successfully");

app.Run();

// Ensure to flush and close the logger when the application shuts down
Log.CloseAndFlush();
