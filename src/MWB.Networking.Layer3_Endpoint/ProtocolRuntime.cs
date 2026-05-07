using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer3_Endpoint;

/// <summary>
/// Bundles the fully-wired runtime components returned by
/// <see cref="IProtocolRuntimeFactory.CreateAsync"/>.
///
/// <see cref="SessionEndpoint"/> holds a single instance of this type and
/// disposes it as a unit when the connection is torn down.
///
/// Disposal order is intentional:
///   1. <see cref="Adapter"/> — disconnects event wiring so no further frames
///      are delivered into the session after teardown begins.
///   2. <see cref="Driver"/>  — cancels the I/O loop and releases transport
///      resources.
/// </summary>
public sealed class ProtocolRuntime : IDisposable
{
    /// <summary>
    /// Handle exposing the protocol session's public API.
    /// </summary>
    public required ProtocolSessionHandle Session { get; init; }

    /// <summary>
    /// The frame-conversion adapter that bridges the protocol session and
    /// the network framing layer (e.g. <c>SessionAdapter</c>).
    /// Disposing it unsubscribes all cross-layer event handlers.
    /// </summary>
    public required IDisposable Adapter { get; init; }

    /// <summary>
    /// The transport driver that owns the read-and-decode I/O loop.
    /// </summary>
    public required IProtocolDriver Driver { get; init; }

    /// <inheritdoc />
    public void Dispose()
    {
        // Unwire the session ↔ network bridge before cancelling I/O
        // so that in-flight frames are not delivered after teardown starts.
        Adapter.Dispose();
        Driver.Dispose();
    }
}
