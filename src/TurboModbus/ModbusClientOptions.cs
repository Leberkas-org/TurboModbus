namespace TurboModbus;

public sealed record ModbusClientOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 502;
    public byte UnitId { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    public int QueueSize { get; init; } = 256;
}
