# Raw Streams

For full control over the Modbus TCP pipeline, use `ModbusCodec` directly.

## ReadFlow

A pre-wired `Flow<ReadRequest, RegisterResponse>` connected to a device:

```csharp
var readFlow = ModbusCodec.ReadFlow(system, "192.168.1.100", 502, unitId: 1);

Source.From(new[]
    {
        new ReadRequest(1000, 4),
        new ReadRequest(1050, 2),
    })
    .Via(readFlow)
    .RunForEach(r => Console.WriteLine($"[{r.Address}]: {r.Registers.Length} registers"), mat);
```

## Custom Topology

Use `ModbusCodec.Framing()` to get the raw `BidiFlow` and bring your own TCP transport:

```csharp
// BidiFlow<ModbusRequest, ByteString, ByteString, ModbusResponse>
var framing = ModbusCodec.Framing();
```

This is useful when you need custom TCP configuration, TLS, or want to multiplex multiple logical connections over a single transport.
