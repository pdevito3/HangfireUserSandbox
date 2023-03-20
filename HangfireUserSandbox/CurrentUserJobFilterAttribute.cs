namespace HangfireUserSandbox;

using Hangfire;
using Hangfire.Annotations;
using Hangfire.AspNetCore;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

public interface IJobWithUserContext
{
    public string? User { get; set; }
}
public class JobWithUserContext : IJobWithUserContext
{
    public string? User { get; set; }
}
public interface IJobContextAccessor
{
    JobWithUserContext? UserContext { get; set; }
}
public class JobContextAccessor : IJobContextAccessor
{
    public JobWithUserContext? UserContext { get; set; }
}

public class CurrentUserJobFilterAttribute : JobFilterAttribute, IClientFilter, IServerFilter
{
    public void OnCreating(CreatingContext context)
    {
        var argue = context.Job.Args.FirstOrDefault(x => x is IJobWithUserContext);
        if (argue == null)
            throw new Exception($"This job does not implement the {nameof(IJobWithUserContext)} interface");
        
        var jobParameters = argue as IJobWithUserContext;
        var user = jobParameters?.User;
        
        if(user == null)
            throw new Exception($"A User could not be established");

        context.SetJobParameter("User", user);
    }
    
    public void OnCreated(CreatedContext context)
    {
    }

    public void OnPerforming(PerformingContext filterContext)
    {
    }

    public void OnPerformed(PerformedContext filterContext)
    {
    }
}

internal class ServiceJobActivatorScope : JobActivatorScope
{
    private readonly IServiceScope _serviceScope;

    public ServiceJobActivatorScope([NotNull] IServiceScope serviceScope)
    {
        _serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
    }

    public override object Resolve(Type type)
    {
        return ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
    }

    public override void DisposeScope()
    {
        _serviceScope.Dispose();
    }
}

public class JobWithUserContextActivator : AspNetCoreJobActivator
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public JobWithUserContextActivator([NotNull] IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    public override JobActivatorScope BeginScope(JobActivatorContext context)
    {
        var user = context.GetJobParameter<string>("User");
        
        if (user == null)
        {
            return base.BeginScope(context);
        }
        
        var serviceScope = _serviceScopeFactory.CreateScope();

        var userContextForJob = serviceScope.ServiceProvider.GetRequiredService<IJobContextAccessor>();
        userContextForJob.UserContext = new JobWithUserContext {User = user};

        return new ServiceJobActivatorScope(serviceScope);
    }
}


