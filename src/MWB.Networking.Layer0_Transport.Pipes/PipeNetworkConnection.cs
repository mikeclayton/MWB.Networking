using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Stack.Abstractions;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using System.Buffers;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.Pipes;

public sealed class PipeNetworkConnection : INetworkConnection, IDisposable
{
    private bool _started;
    private volatile bool _disposed;

    public PipeNetworkConnection(ILogger logger, PipeReader reader, PipeWriter writer, ObservableConnectionStatus status)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    private ILogger Logger
    {
        get;
    }

    private PipeReader Reader
    {
        get;
    }

    private PipeWriter Writer
    {
        get;
    }

    private ObservableConnectionStatus Status
    {
        get;
    }


    /// <summary>
    /// Called by the provider once wiring is complete.
    /// Pipes are immediately usable, so we report readiness here.
    /// </summary>
    internal void OnStarted()
    {
        if (_started)
        {
            return;
        }

        _started = true;

        this.Status.OnConnecting();
        this.Status.OnConnected();
    }

    /// <summary>
    /// Reads raw bytes from the pipe into the provided buffer.
    /// Returns 0 to indicate end-of-stream.
    /// </summary>
    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct = default)
    {
        try
        {
            while (true)
            {
                var result = await this.Reader.ReadAsync(ct).ConfigureAwait(false);
                var sequence = result.Buffer;

                if (sequence.Length > 0)
                {
                    // Copy as much as fits into the provided buffer
                    var toCopy = (int)Math.Min(sequence.Length, buffer.Length);
                    sequence.Slice(0, toCopy).CopyTo(buffer.Span);

                    // Advance reader by consumed bytes
                    this.Reader.AdvanceTo(sequence.GetPosition(toCopy));
                    return toCopy;
                }

                if (result.IsCompleted)
                {
                    // EOF
                    this.Reader.AdvanceTo(sequence.End);
                    this.Status.OnDisconnected(
                        new TransportDisconnectedEventArgs(
                            "Pipe completed (remote closed connection)."));
                    return 0;
                }

                // Otherwise: no data yet, keep waiting
                this.Reader.AdvanceTo(sequence.Start, sequence.End);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.Status.OnFaulted(
                new TransportFaultedEventArgs(
                    "Pipe read failed.",
                    ex));
            throw;
        }
    }

    /// <summary>
    /// Writes raw byte segments to the pipe.
    /// </summary>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        try
        {
            foreach (var segment in segments.Segments)
            {
                if (!segment.IsEmpty)
                {
                    segment.Span.CopyTo(
                        this.Writer.GetSpan(segment.Length));
                    this.Writer.Advance(segment.Length);
                }
            }

            var result = await this.Writer.FlushAsync(ct).ConfigureAwait(false);

            if (result.IsCompleted && !ct.IsCancellationRequested)
            {
                throw new IOException("Pipe closed during write.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.Status.OnFaulted(
                new TransportFaultedEventArgs(
                    "Pipe write failed.",
                    ex));
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        try
        {
            // existing dispose logic
            this.Reader?.Complete();
            this.Writer?.Complete(new OperationCanceledException());
        }
        catch (Exception ex) when (
            ex is IOException or ObjectDisposedException)
        {
            // Normal shutdown paths
            this.Logger.LogDebug(ex, "Pipe already closed during dispose.");
        }
        finally
        {
            // Disposal is an observable, orderly termination:
            // the connection is no longer usable.
            this.Status.OnDisconnected(
                new TransportDisconnectedEventArgs(
                    "Pipe transport disposed."));
        }
    }
}