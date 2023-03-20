using Hangfire;
using Hangfire.AspNetCore;
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
// builder.Services.Replace(new ServiceDescriptor(typeof(AspNetCoreJobActivator), typeof(JobWithUserContextActivator), ServiceLifetime.Singleton));

// builder.Services.RemoveAll(typeof(AspNetCoreJobActivator));
// builder.Services.AddSingleton<AspNetCoreJobActivator, JobWithUserContextActivator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("EnqueueHangfireJob", (IBackgroundJobClient backgroundJobClient) => 
{
    backgroundJobClient.Enqueue<DoMyJob>(x => x.Handle(new MyJob
    {
        User = "Test User",
        SpecialProp = "Special Value"
    }));
    return Results.Ok();
});

app.Run();

public class MyJob : IJobWithUserContext
{
    public string User { get; set; }
    public string SpecialProp { get; set; }
}

public class DoMyJob
{
    private readonly IJobContextAccessor _jobContextAccessor;

    public DoMyJob(IJobContextAccessor jobContextAccessor)
    {
        _jobContextAccessor = jobContextAccessor;
    }

    [CurrentUserJobFilter]
    public void Handle(MyJob job)
    {
        Console.WriteLine($"Hello world from '{job?.User}' (injectable as '{_jobContextAccessor?.UserContext?.User}') using special prop: '{job?.SpecialProp}'");
    }
}