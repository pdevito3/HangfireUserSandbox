using Hangfire;
using Hangfire.MemoryStorage;
using HangfireUserSandbox;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IJobContextAccessor, JobContextAccessor>();
builder.Services.TryAddSingleton<IJobWithUserContext, JobWithUserContext>();
builder.Services.AddHangfire(hangfireConfig => hangfireConfig
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseColouredConsoleLogProvider()
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()
    .UseActivator(new JobWithUserContextActivator(builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()))
);
builder.Services.AddHangfireServer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("EnqueueHangfireJob", (IBackgroundJobClient backgroundJobClient) => 
{
    backgroundJobClient.Enqueue<MyJob>(x => x.Handle(new MyJob.Data
    {
        User = "Test User",
        SpecialProp = "Special Value"
    }));
    return Results.Ok();
});

app.Run();

public class MyJob
{
    private readonly IJobContextAccessor _jobContextAccessor;

    public MyJob(IJobContextAccessor jobContextAccessor)
    {
        _jobContextAccessor = jobContextAccessor;
    }
    
    public class Data : IJobWithUserContext
    {
        public string User { get; set; }
        public string SpecialProp { get; set; }
    }

    [CurrentUserJobFilter]
    public void Handle(Data jobData)
    {
        Console.WriteLine($"Hello world from '{jobData?.User}' (injectable as '{_jobContextAccessor?.UserContext?.User}') using special prop: '{jobData?.SpecialProp}'");
    }
}