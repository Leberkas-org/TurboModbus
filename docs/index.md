---
layout: home
hero:
  name: TurboModbus
  text: Modbus TCP on Akka.Streams
  tagline: Clean async API for simple use, raw streams for reactive pipelines.
  image:
    light: /logo_square.svg
    dark: /logo_square-dark.svg
    alt: TurboModbus
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/leberkas-org/TurboModbus

features:
  - title: Easy to use
    details: Connect, read, done. No protocol details, no transaction IDs, no boilerplate.
  - title: Stream-Native Polling
    details: Fluent poll builder returns an Akka.Streams Source you compose into any pipeline.
  - title: Built on Akka.Streams
    details: TCP transport, MBAP framing, and backpressure handled by Akka.Streams internally.
  - title: Built-in Change Detection
    details: Apply ChangeDetectionStage to any poll source and only react when register values actually change.
  - title: Backpressure Aware
    details: Never overwhelm your device or your app. Akka.Streams propagates backpressure end-to-end from TCP to your code.
  - title: Clean Connection Lifecycle
    details: Actor-managed connections with IsConnected state, Disconnected events, and automatic cleanup of pending requests.
---

## Why TurboModbus?

Most .NET Modbus libraries give you a socket wrapper with synchronous reads.
That works — until you need to poll dozens of registers, react to changes, compose multiple devices, or handle backpressure in a real production system.

**TurboModbus takes a different approach.** It's built from the ground up on [Akka.Streams](https://getakka.net/articles/streams/introduction.html), so every read, write, and poll flows through a composable, backpressure-aware pipeline. You get a simple `ReadAsync` / `WriteAsync` API for straightforward use cases — and full access to raw Akka.Streams flows when you need more control.

### What sets it apart

| | Typical Modbus library | TurboModbus |
|---|---|---|
| **Polling** | Manual `while` loop with `Task.Delay` | Fluent `Poll()` builder → Akka.Streams `Source` |
| **Change detection** | Roll your own diffing | Built-in `ChangeDetectionStage` |
| **Backpressure** | None — slow consumers drop or queue unbounded | End-to-end via Akka.Streams |
| **Connection lifecycle** | Try/catch around every call | Actor-supervised with `Disconnected` event |
| **Composition** | One client, one device, manual orchestration | Compose sources, flows, and stages freely |
| **Transaction IDs** | Exposed or manual | Handled internally — never in your code |

### Who is it for?

- **IoT / SCADA applications** that poll many registers across devices and need to react to changes efficiently.
- **Akka.NET users** who want Modbus to fit naturally into an existing actor system or stream topology.
- **Anyone tired of writing polling loops** — define what to poll, get a reactive stream back, compose from there.
