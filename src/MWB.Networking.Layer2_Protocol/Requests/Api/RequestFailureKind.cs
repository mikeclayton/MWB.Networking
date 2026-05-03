namespace MWB.Networking.Layer2_Protocol.Requests.Api;

public enum RequestFailureKind
{
    /// <summary>
    /// Protocol violation, out of memory, etc
    /// </summary>
    InternalError,

    /// <summary>
    /// Application-defined error.
    /// System is working normally, but the application refused to process the request.
    /// </summary>
    ApplicationError
}
