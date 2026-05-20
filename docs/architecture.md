# Architecture

## Stream Pipeline

```
ModbusClient
  -> StreamOwnerActor (owns stream lifecycle)
      -> Source.Queue<ModbusRequest>
          -> ModbusCodec BidiFlow
              -> Encode: ModbusRequest -> MBAP frame (ByteString)
              -> TCP.OutgoingConnection
              -> MbapFramingStage: ByteString -> complete MBAP frames
              -> Decode: MBAP frame -> ModbusResponse
          -> Sink.ForEach: route response back to actor
      -> Transaction ID correlation -> reply to original Ask sender
```

## Key Components

### StreamOwnerActor

Owns the materialized Akka.Streams pipeline. When this actor stops, the stream is torn down. When the TCP connection drops, the stream completes, which stops the actor.

The `ModbusClient` watches the actor via `DeathWatch` — when it dies, `IsConnected` becomes `false` and the `Disconnected` event fires.

### ModbusCodec

A `BidiFlow` that encodes `ModbusRequest` into MBAP-framed `ByteString` and decodes incoming `ByteString` into `ModbusResponse`. Handles all four supported function codes (FC3, FC4, FC6, FC16) and Modbus error responses.

### MbapFramingStage

A custom `GraphStage` that implements length-based framing for MBAP headers. Reads the 7-byte header, extracts the PDU length field, and emits complete frames. Handles partial reads and TCP fragmentation.

### Transaction ID Correlation

The `StreamOwnerActor` assigns transaction IDs internally and maintains a `ConcurrentDictionary<ushort, IActorRef>` mapping each transaction ID to the Akka `Ask` sender. When a response arrives, the actor looks up the sender by transaction ID and replies. This allows out-of-order responses (though Modbus TCP devices typically respond in order).

## Design Decisions

**Why Akka.Streams over raw TCP?** Backpressure, lifecycle management, and composability. The stream pipeline naturally handles slow consumers, connection drops, and resource cleanup.

**Why an actor owns the stream?** Akka.Streams materializations are tied to a materializer. By owning the stream in an actor, we get clean lifecycle management — stop the actor, stop the stream. No dangling connections.

**Why synchronous `Connect()`?** Actor creation is synchronous. The TCP connection happens asynchronously in the actor's `PreStart`. The client is usable immediately — requests queue in the `Source.Queue` while the connection establishes.

**Why separate `ReadAsync`/`WriteAsync` instead of raw stream access?** Most users want request-response semantics. The stream internals (transaction IDs, MBAP framing, function codes) are implementation details. Power users can access `ModbusCodec.ReadFlow()` or `ModbusCodec.Framing()` directly.
