# Getting Started

## Install

```bash
dotnet add package TurboModbus
```

## Connect and Read

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

// Read 4 holding registers starting at address 1000
ushort[] values = await client.ReadAsync(1000, 4);

// Read input registers
ushort[] power = await client.ReadAsync(4122, 2, inputRegister: true);
```

## Write

```csharp
// Single register (uses FC6)
await client.WriteAsync(1005, 1);

// Multiple registers (uses FC16)
await client.WriteAsync(1032, 0x00, 0x2E);
```

The client picks the correct Modbus function code automatically.

## Connection Lifecycle

```csharp
client.Connect();                // synchronous — creates actor, materializes TCP stream
Console.WriteLine(client.IsConnected); // true

// React to disconnection
client.Disconnected += () => Console.WriteLine("Lost connection");

// Clean up
client.Dispose();
```

`Connect()` is synchronous because the TCP connection is established inside the Akka actor's `PreStart`, not on the calling thread. The actor owns the stream lifecycle — disposing the client stops the actor, which tears down the stream.

## Options

```csharp
new ModbusClientOptions
{
    Host = "192.168.1.100",   // required
    Port = 502,                // default: 502
    UnitId = 1,                // default: 1
    Timeout = TimeSpan.FromSeconds(5),  // default: 5s per request
    QueueSize = 256,           // default: 256 (backpressure buffer)
}
```
