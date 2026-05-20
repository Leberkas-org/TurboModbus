namespace TurboModbus;

using Akka.Streams;
using Akka.Streams.Stage;

internal sealed class ChangeDetectionStage : GraphStage<FlowShape<RegisterResponse, RegisterResponse>>
{
    private readonly Inlet<RegisterResponse> _in = new("ChangeDetection.in");
    private readonly Outlet<RegisterResponse> _out = new("ChangeDetection.out");

    public override FlowShape<RegisterResponse, RegisterResponse> Shape => new(_in, _out);

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

    private sealed class Logic : InAndOutGraphStageLogic
    {
        private readonly ChangeDetectionStage _stage;
        private readonly Dictionary<ushort, ushort[]> _previous = new();

        public Logic(ChangeDetectionStage stage) : base(stage.Shape)
        {
            _stage = stage;
            SetHandler(stage._in, this);
            SetHandler(stage._out, this);
        }

        public override void OnPush()
        {
            var response = Grab(_stage._in);
            var changed = !_previous.TryGetValue(response.Address, out var prev)
                          || !prev.AsSpan().SequenceEqual(response.Registers);

            if (changed)
            {
                _previous[response.Address] = response.Registers.ToArray();
                Push(_stage._out, response);
            }
            else
            {
                Pull(_stage._in);
            }
        }

        public override void OnPull() => Pull(_stage._in);
    }
}
