# Polling

TurboModbus provides a fluent polling API that returns an Akka.Streams `Source`.

## Basic Polling

```csharp
Source<RegisterResponse, UniqueKillSwitch> source = client.Poll(
    TimeSpan.FromSeconds(10),
    poll => poll
        .Registers(1000, 4)       // outdoor temp, avg temp, buffer temp, ...
        .Registers(1050, 2)       // supply temp, return temp
        .Registers(4122, 2, inputRegister: true));  // electrical power
```

Each tick reads all configured register groups sequentially (Modbus TCP is serial) and emits a `RegisterResponse` per group.

## Change Detection

Apply the built-in `ChangeDetectionStage` to filter to only changed values:

```csharp
using Akka.Streams;
using Akka.Streams.Dsl;

var mat = system.Materializer();

var (killSwitch, done) = client.Poll(TimeSpan.FromSeconds(10), poll => poll
        .Registers(1000, 4)
        .Registers(1050, 2))
    .Via(new ChangeDetectionStage())
    .ToMaterialized(
        Sink.ForEach<RegisterResponse>(r =>
            Console.WriteLine($"[{r.Address}] changed")),
        Keep.Both)
    .Run(mat);

// Stop polling
killSwitch.Shutdown();
await done;
```

The stage holds previous values internally and only pushes downstream when register content actually changes.

## RegisterResponse

```csharp
public sealed record RegisterResponse(ushort Address, ushort[] Registers);
```

The `Address` is the start address of the read request. `Registers` contains the raw 16-bit values. Interpret them according to your device's register map (float, int, bool, etc.).

## KillSwitch

The `Source` is materialized with a `UniqueKillSwitch`. Call `Shutdown()` to stop polling cleanly. The underlying TCP connection is managed by the `ModbusClient` — stopping the poll source does not disconnect the client.
