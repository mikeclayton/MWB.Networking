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

    public static IDisposable? BeginMethodScope(this ILogger logger, object obj, [CallerMemberName] string methodName = "")
    {
        var hasDisplayName = obj as IHasDisplayName;
        var hasId = obj as IHasId;
        return logger.BeginMethodScope(
            className: obj.GetType().Name,
            displayName: hasDisplayName?.DisplayName,
            longId: hasId?.Id.ToString(),
            shortId: hasId?.ShortId,
            methodName: methodName);
    }

    public static IDisposable? BeginMethodScope(this ILogger logger, string className, string? displayName, string? longId, string? shortId, string methodName)
    {
        return logger.BeginScope(
            new OrderedDictionary<string, string?>
            {
                ["ClassName"] = className,
                ["DisplayName"] = displayName,
                ["LongId"] = longId,
                ["ShortId"] = shortId,
                ["MethodName"] = methodName
            }
        );
    }

    //[Conditional("DEBUG")]
    //public static void LogDebug2(this ILogger logger, string format, string? className, string? id, string? method, params object?[] args)
    //{
    //    var fullFormat = "[{ClassName}:{Id}:{MethodName}] " + format;
    //    var fullArgs = new List<object?> { className, id, method }.Concat(args);
    //    logger.LogDebug(fullFormat, fullArgs);
    //}
}
