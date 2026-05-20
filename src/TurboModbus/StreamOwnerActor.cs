namespace TurboModbus;

using System.Collections.Concurrent;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

internal sealed class StreamOwnerActor : ReceiveActor
{
    private readonly ModbusClientOptions _options;
    private readonly ConcurrentDictionary<ushort, IActorRef> _pending = new();
    private ISourceQueueWithComplete<ModbusRequest>? _queue;
    private int _transactionId;

    public StreamOwnerActor(ModbusClientOptions options)
    {
        _options = options;

        Receive<ReadRequest>(HandleRead);
        Receive<WriteRequest>(HandleWrite);
        Receive<ModbusResponse>(HandleResponse);
        Receive<StreamCompleted>(_ => Context.Stop(Self));
    }

    protected override void PreStart()
    {
        var modbusFlow = ModbusCodec.RawTcpFlow(Context.System, _options.Host, _options.Port);
        var self = Self;

        var (queue, done) = Source.Queue<ModbusRequest>(_options.QueueSize, OverflowStrategy.Backpressure)
            .ViaMaterialized(modbusFlow, Keep.Left)
            .ToMaterialized(Sink.ForEach<ModbusResponse>(r => self.Tell(r)), Keep.Both)
            .Run(Context.Materializer());

        _queue = queue;

        done.ContinueWith(_ => self.Tell(StreamCompleted.Instance));
    }

    protected override void PostStop()
    {
        _queue?.Complete();
        foreach (var (_, replyTo) in _pending)
            replyTo.Tell(new Status.Failure(new OperationCanceledException("Stream owner stopped")));
        _pending.Clear();
    }

    private void HandleRead(ReadRequest req)
    {
        var fc = req.InputRegister ? FunctionCode.ReadInputRegisters : FunctionCode.ReadHoldingRegisters;
        Enqueue(new ModbusRequest(NextTxId(), _options.UnitId, fc, req.Address, [req.Count]));
    }

    private void HandleWrite(WriteRequest req)
    {
        var fc = req.Values.Length == 1 ? FunctionCode.WriteSingleRegister : FunctionCode.WriteMultipleRegisters;
        Enqueue(new ModbusRequest(NextTxId(), _options.UnitId, fc, req.Address, req.Values));
    }

    private void Enqueue(ModbusRequest request)
    {
        var sender = Sender;
        _pending[request.TransactionId] = sender;
        _queue?.OfferAsync(request).ContinueWith(t =>
        {
            if (t.Result != QueueOfferResult.Enqueued.Instance)
            {
                _pending.TryRemove(request.TransactionId, out _);
                sender.Tell(new Status.Failure(
                    new InvalidOperationException($"Queue rejected request: {t.Result}")));
            }
        });
    }

    private void HandleResponse(ModbusResponse response)
    {
        if (!_pending.TryRemove(response.TransactionId, out var replyTo)) return;

        if (response.IsError)
            replyTo.Tell(new Status.Failure(
                new ModbusException((byte)response.Function, response.ErrorCode)));
        else
            replyTo.Tell(new RegisterResponse(response.Registers.Length > 0 ? response.Registers[0] : (ushort)0, response.Registers));
    }

    private ushort NextTxId() => (ushort)(Interlocked.Increment(ref _transactionId) & 0xFFFF);

    private sealed record StreamCompleted
    {
        public static readonly StreamCompleted Instance = new();
    }
}
