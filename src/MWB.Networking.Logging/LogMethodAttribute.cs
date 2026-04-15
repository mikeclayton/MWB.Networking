using MethodDecorator.Fody.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MWB.Networking.Logging;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Module | AttributeTargets.Assembly)]
public sealed class LogMethodAttribute : Attribute, IMethodDecorator
{
    public void Init(
        object? instance,
        MethodBase method,
        object[]? args)
    {
        this.Instance = instance;
        this.Method = method;
        this.DeclaringType = method.DeclaringType ?? throw new InvalidOperationException();
        this.HasLogger = instance as IHasLogger;
        this.HasId = instance as IHasId;
        this.HasDisplayName = instance as IHasDisplayName;
    }

    private object? Instance
    {
        get;
        set;
    }

    private IHasLogger? HasLogger
    {
        get;
        set;
    }

    private IHasId? HasId
    {
        get;
        set;
    }

    private IHasDisplayName? HasDisplayName
    {
        get;
        set;
    }

    private Type? DeclaringType
    {
        get;
        set;
    }

    private MethodBase? Method
    {
        get;
        set;
    }

    private string? GetLongId()
    {
        var value = this.HasDisplayName?.DisplayName
            ?? this.HasId?.Id.ToString();
        return value;
    }

    private string? GetShortId()
    {
        var value = this.HasDisplayName?.DisplayName
            ?? this.HasId?.ShortId.ToString();
        return value;
    }

    public void OnEntry()
    {
        if (this.HasLogger?.Logger is not ILogger logger)
        {
            return;
        }
        using var scope = logger.BeginMethodScope(
            this.DeclaringType?.Name ?? throw new InvalidOperationException(),
            this.HasDisplayName?.DisplayName,
            this.GetLongId(),
            this.GetShortId(),
            this.Method?.Name ?? throw new InvalidOperationException());
        logger.LogDebug("Entering method");
    }

    public void OnExit()
    {
        if (this.HasLogger?.Logger is not ILogger logger)
        {
            return;
        }
        using var scope = logger.BeginMethodScope(
            this.DeclaringType?.Name ?? throw new InvalidOperationException(),
            this.HasDisplayName?.DisplayName,
            this.GetLongId(),
            this.GetShortId(),
            this.Method?.Name ?? throw new InvalidOperationException());
        logger.LogDebug("Leaving method");
    }

    public void OnException(Exception exception)
    {
        if (this.HasLogger?.Logger is not ILogger logger)
        {
            return;
        }
        using var scope = logger.BeginMethodScope(
            this.DeclaringType?.Name ?? throw new InvalidOperationException(),
            this.HasDisplayName?.DisplayName,
            this.GetLongId(),
            this.GetShortId(),
            this.Method?.Name ?? throw new InvalidOperationException());
        logger.LogDebug("{Exception}", exception.ToString());
    }
}
