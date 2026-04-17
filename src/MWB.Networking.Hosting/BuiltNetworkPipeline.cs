using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Hosting;

public sealed record BuiltNetworkPipeline(
    INetworkConnection Connection,
    NetworkFrameWriter FrameWriter,
    NetworkFrameReader FrameReader,
    IFrameDecoder RootDecoder);
