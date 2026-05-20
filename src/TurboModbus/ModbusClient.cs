namespace TurboModbus;

using Akka.Actor;

public sealed class ModbusClient : IDisposable
{
    private readonly ActorSystem _system;
    private readonly ModbusClientOptions _options;
    private IActorRef? _streamOwner;
    private IActorRef? _watcher;

    public ModbusClient(ActorSystem system, ModbusClientOptions options)
    {
        _system = system;
        _options = options;
    }

    internal ActorSystem System => _system;
    public bool IsConnected => _streamOwner != null;

    public void Connect()
    {
        if (IsConnected) return;

        _streamOwner = _system.ActorOf(
            Props.Create(() => new StreamOwnerActor(_options)),
            $"modbus-{_options.Host}-{_options.Port}-{Guid.NewGuid():N}");

        _watcher = _system.ActorOf(
            Props.Create(() => new DeathWatchActor(_streamOwner!, OnStreamOwnerDied)));
    }

    public async Task<ushort[]> ReadAsync(ushort address, ushort count, bool inputRegister = false)
    {
        var response = await AskAsync(new ReadRequest(address, count, inputRegister));
        return response.Registers;
    }

    public Task WriteAsync(ushort address, params ushort[] values)
        => AskAsync(new WriteRequest(address, values));

    public void Dispose()
    {
        if (_streamOwner != null)
        {
            _system.Stop(_streamOwner);
            _streamOwner = null;
        }
        if (_watcher != null)
        {
            _system.Stop(_watcher);
            _watcher = null;
        }
    }

    private void OnStreamOwnerDied()
    {
        _streamOwner = null;
        _watcher = null;
    }

    private async Task<RegisterResponse> AskAsync(IWithModbusAddress request)
    {
        if (_streamOwner == null)
            throw new InvalidOperationException("Not connected");

        return await _streamOwner.Ask<RegisterResponse>(request, _options.Timeout);
    }
}

internal sealed class DeathWatchActor : ReceiveActor
{
    public DeathWatchActor(IActorRef target, Action onTerminated)
    {
        Context.Watch(target);
        Receive<Terminated>(_ =>
        {
            onTerminated();
            Context.Stop(Self);
        });
    }
}
