using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MWB.Networking.Logging;

public static class ILoggerExtensions
{
    //[Conditional("DEBUG")]
    //public static void EnterMethod(this ILogger logger, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "")
    //{
    //    var className = Path.GetFileNameWithoutExtension(file);
    //    logger.LogDebug("Entering {ClassName}.{MemberName}", className, memberName);
    //}

    //[Conditional("DEBUG")]
    //public static void LeaveMethod(this ILogger logger, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "")
    //{
    //    var className = Path.GetFileNameWithoutExtension(file);
    //    logger.LogDebug("Leaving {ClassName}.{MemberName}", className, memberName);
    //}

    //public static IDisposable? BeginMethodScope(this ILogger logger, object classInstance, [CallerMemberName] string methodName = "")
    //{
    //    var hasDisplayName = classInstance as IHasDisplayName;
    //    var hasId = classInstance as IHasId;
    //    return logger.BeginMethodScope(
    //        className: classInstance.GetType().Name,
    //        displayName: hasDisplayName?.DisplayName,
    //        longId: hasId?.Id.ToString(),
    //        shortId: hasId?.ShortId,
    //        methodName: methodName);
    //}

    //public static IDisposable? BeginMethodScope(this ILogger logger, string className, string? displayName, string? longId, string? shortId, [CallerMemberName] string methodName = "")
    //{
    //    return logger.BeginScope(
    //        new OrderedDictionary<string, string?>
    //        {
    //            ["ClassName"] = className,
    //            ["DisplayName"] = displayName,
    //            ["LongId"] = longId,
    //            ["ShortId"] = shortId,
    //            ["MethodName"] = methodName
    //        }
    //    );
    //}

    public static ExecutionScope BeginMethodLoggingScope(
            this ILogger logger,
            object classInstance,
            [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(classInstance);

        return ExecutionScope.StartScope(
            onStartScope: () => logger.EnterMethod(classInstance, methodName),
            onEndScope: () => logger.LeaveMethod()
        );
    }

    internal static IDisposable? EnterMethod(this ILogger logger, object classInstance, [CallerMemberName] string methodName = "")
    {
        var hasDisplayName = classInstance as IHasDisplayName;
        var hasId = classInstance as IHasId;
        var loggerScope = logger.BeginScope(
            new OrderedDictionary<string, string?>
            {
                ["ClassName"] = classInstance.GetType().Name,
                ["DisplayName"] = hasDisplayName?.DisplayName,
                ["LongId"] = hasId?.Id.ToString(),
                ["ShortId"] = hasId?.ShortId,
                ["MethodName"] = methodName
            }
        );
        logger.LogDebug("Entering method");
        return loggerScope;
    }

    internal static void LeaveMethod(this ILogger logger)
    {
        logger.LogDebug("Leaving method");
    }
}
