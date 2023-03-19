using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(hangfireConfig => hangfireConfig
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseColouredConsoleLogProvider()
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()
);
builder.Services.AddHangfireServer();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("EnqueueHangfireJob", () => 
    {
    BackgroundJob.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));
    return Results.Ok();
});

app.Run();
