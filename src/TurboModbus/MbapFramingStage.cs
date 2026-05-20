namespace TurboModbus;

using System.Buffers.Binary;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Stage;

internal sealed class MbapFramingStage : GraphStage<FlowShape<ByteString, ByteString>>
{
    private readonly Inlet<ByteString> _in = new("MbapFraming.in");
    private readonly Outlet<ByteString> _out = new("MbapFraming.out");

    public override FlowShape<ByteString, ByteString> Shape => new(_in, _out);

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes)
        => new Logic(this);

    private sealed class Logic : InAndOutGraphStageLogic
    {
        private readonly MbapFramingStage _stage;
        private ByteString _buffer = ByteString.Empty;

        public Logic(MbapFramingStage stage) : base(stage.Shape)
        {
            _stage = stage;
            SetHandler(stage._in, this);
            SetHandler(stage._out, this);
        }

        public override void OnPush()
        {
            _buffer += Grab(_stage._in);
            TryEmit();
        }

        public override void OnPull()
        {
            TryEmit();
        }

        public override void OnUpstreamFinish()
        {
            if (_buffer.IsEmpty) CompleteStage();
        }

        private void TryEmit()
        {
            while (true)
            {
                if (_buffer.Count < 7)
                {
                    if (!HasBeenPulled(_stage._in)) Pull(_stage._in);
                    return;
                }

                var lengthBytes = _buffer.Slice(4, 2).ToArray();
                var pduLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
                var frameLength = 6 + pduLength;

                if (_buffer.Count < frameLength)
                {
                    if (!HasBeenPulled(_stage._in)) Pull(_stage._in);
                    return;
                }

                var frame = _buffer.Slice(0, frameLength);
                _buffer = _buffer.Slice(frameLength);

                if (IsAvailable(_stage._out))
                    Push(_stage._out, frame);
                else
                    return;
            }
        }
    }
}
