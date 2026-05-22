
# TurboModbus
![turboModbus](logo.svg)

High-performance Modbus TCP client built on [Akka.Streams](https://getakka.net/articles/streams/introduction.html). Clean async API for simple use cases, raw stream access for reactive pipelines.

## Install

```
dotnet add package TurboModbus
```

## Quick Start

```csharp
using Akka.Actor;
using TurboModbus;

var system = ActorSystem.Create("my-app");
var client = new ModbusClient(system, new ModbusClientOptions
{
    Host = "192.168.1.100",
    Port = 502,
    UnitId = 1,
});

client.Connect();

// Read
ushort[] registers = await client.ReadAsync(1000, 4);
ushort[] inputRegs = await client.ReadAsync(4122, 2, inputRegister: true);

// Write
await client.WriteAsync(1005, 1);          // single register
await client.WriteAsync(1032, 0x00, 0x2E); // multiple registers

client.Dispose();
```

## Polling with Change Detection

Subscribe to register changes with a fluent polling API:

```csharp
Source<RegisterResponse, UniqueKillSwitch> source = client.Poll(
    TimeSpan.FromSeconds(10),
    poll => poll
        .Registers(1000, 4)
        .Registers(1050, 2)
        .Registers(4122, 2, inputRegister: true));

// Compose into any Akka.Streams pipeline
var killSwitch = source
    .RunForEach(r => Console.WriteLine($"[{r.Address}] = {r.Registers[0]}"), materializer);
```

The source emits `RegisterResponse` on every poll. Apply the built-in `ChangeDetectionStage` to only get changes:

```csharp
source
    .Via(new ChangeDetectionStage())
    .RunForEach(r => Console.WriteLine($"Changed: [{r.Address}]"), materializer);
```

## Architecture

```
ModbusClient (public API)
  -> StreamOwnerActor (owns stream lifecycle)
      -> Source.Queue -> ModbusCodec BidiFlow -> TCP -> MbapFramingStage -> Response correlation
```

- **Akka.Streams TCP** transport with proper backpressure
- **MBAP framing** via custom `GraphStage` (length-based, no delimiter parsing)
- **Transaction ID correlation** handled internally (never exposed in public API)
- **DeathWatch** on the stream owner -- `client.IsConnected` reflects actual connection state
- **Disconnected event** fires when the TCP connection drops

## Connection Lifecycle

```csharp
client.Connect();               // creates StreamOwnerActor, materializes TCP stream
Console.WriteLine(client.IsConnected); // true

client.Disconnected += () => Console.WriteLine("Connection lost");

client.Dispose();               // stops actors, cleans up
Console.WriteLine(client.IsConnected); // false
```

The stream owner actor is supervised by the ActorSystem. Killing the stream does not kill the ActorSystem.

## Raw Stream Access

For full control, use `ModbusCodec` directly:

```csharp
// Get a Flow<ReadRequest, RegisterResponse> connected to a device
var readFlow = ModbusCodec.ReadFlow(system, "192.168.1.100", 502, unitId: 1);
```
