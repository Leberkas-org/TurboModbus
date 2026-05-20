# API Reference

## ModbusClient

The main entry point for Modbus TCP communication.

```csharp
public sealed class ModbusClient : IDisposable
{
    ModbusClient(ActorSystem system, ModbusClientOptions options)

    bool IsConnected { get; }
    event Action? Disconnected;

    void Connect()
    void Dispose()

    Task<ushort[]> ReadAsync(ushort address, ushort count, bool inputRegister = false)
    Task WriteAsync(ushort address, params ushort[] values)
}
```

| Method | Description |
|--------|-------------|
| `Connect()` | Creates the stream owner actor and TCP connection |
| `ReadAsync` | Reads holding registers (FC3) or input registers (FC4) |
| `WriteAsync` | Writes single (FC6) or multiple (FC16) registers |
| `Dispose()` | Stops the stream owner actor and cleans up |

## ModbusClientOptions

```csharp
public sealed record ModbusClientOptions
{
    required string Host { get; init; }
    int Port { get; init; }             // default: 502
    byte UnitId { get; init; }          // default: 1
    TimeSpan Timeout { get; init; }     // default: 5s
    int QueueSize { get; init; }        // default: 256
}
```

## Poll Extension

```csharp
public static Source<RegisterResponse, UniqueKillSwitch> Poll(
    this ModbusClient client,
    TimeSpan interval,
    Action<PollBuilder> configure)
```

## PollBuilder

```csharp
public sealed class PollBuilder
{
    PollBuilder Registers(ushort address, ushort count, bool inputRegister = false)
}
```

## Types

```csharp
public sealed record ReadRequest(ushort Address, ushort Count, bool InputRegister = false);
public sealed record WriteRequest(ushort Address, ushort[] Values);
public sealed record RegisterResponse(ushort Address, ushort[] Registers);

public interface IWithModbusAddress
{
    ushort Address { get; }
}
```

## ModbusCodec

```csharp
public static class ModbusCodec
{
    // Pre-wired read flow connected to a device
    static Flow<ReadRequest, RegisterResponse, NotUsed> ReadFlow(
        ActorSystem system, string host, int port, byte unitId = 1)
}
```

## ModbusException

```csharp
public sealed class ModbusException : Exception
{
    byte FunctionCode { get; }
    byte ErrorCode { get; }
}
```

| Error Code | Meaning |
|-----------|---------|
| 1 | Illegal Function |
| 2 | Illegal Data Address |
| 3 | Illegal Data Value |
| 4 | Slave Device Failure |
