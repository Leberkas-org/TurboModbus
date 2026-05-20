namespace TurboModbus;

using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;

public sealed class PollBuilder
{
    internal readonly List<ReadRequest> Requests = [];

    public PollBuilder Registers(ushort address, ushort count, bool inputRegister = false)
    {
        Requests.Add(new ReadRequest(address, count, inputRegister));
        return this;
    }
}

public static class ModbusClientExtensions
{
    public static Source<RegisterResponse, UniqueKillSwitch> Poll(
        this ModbusClient client, TimeSpan interval, Action<PollBuilder> configure)
    {
        var builder = new PollBuilder();
        configure(builder);

        var requests = builder.Requests.ToArray();

        return Source.Tick(TimeSpan.FromSeconds(1), interval, NotUsed.Instance)
            .SelectMany(_ => requests)
            .SelectAsync(1, async req =>
            {
                try
                {
                    var regs = await client.ReadAsync(req.Address, req.Count, req.InputRegister);
                    return new RegisterResponse(req.Address, regs);
                }
                catch
                {
                    return null;
                }
            })
            .Where(r => r != null)
            .Select(r => r!)
            .ViaMaterialized(KillSwitches.Single<RegisterResponse>(), Keep.Right);
    }
}
