# TurboModbus vs NModbus

TurboModbus is not a drop-in replacement for NModbus. It focuses on a narrower set of Modbus operations but adds a reactive, stream-based architecture that NModbus does not offer.

## Feature Comparison

### Function Codes

| Function Code | TurboModbus | NModbus |
|---|:---:|:---:|
| FC01 Read Coils | - | yes |
| FC02 Read Discrete Inputs | - | yes |
| FC03 Read Holding Registers | yes | yes |
| FC04 Read Input Registers | yes | yes |
| FC05 Write Single Coil | - | yes |
| FC06 Write Single Register | yes | yes |
| FC15 Write Multiple Coils | - | yes |
| FC16 Write Multiple Registers | yes | yes |
| FC23 Read/Write Multiple Registers | - | yes |
| FC08 Diagnostics | - | partial |
| FC43 Device Identification | - | partial |

### Transport

| Transport | TurboModbus | NModbus |
|---|:---:|:---:|
| TCP | yes | yes |
| UDP | - | yes |
| RTU (serial) | - | yes |
| ASCII (serial) | - | yes |

### Roles

| Role | TurboModbus | NModbus |
|---|:---:|:---:|
| Client / Master | yes | yes |
| Server / Slave | - | yes |

### Data Types

| Feature | TurboModbus | NModbus |
|---|:---:|:---:|
| Raw ushort registers | yes | yes |
| Float / Double / UInt32 helpers | - | yes |

### Architecture

| Feature | TurboModbus | NModbus |
|---|:---:|:---:|
| Reactive streaming (Akka.Streams) | yes | - |
| Backpressure-aware pipeline | yes | - |
| Built-in polling with change detection | yes | - |
| Actor-based concurrency | yes | - |
| Custom function code extensibility | - | yes |

## When to Use Which

**Choose TurboModbus** when you need a high-throughput TCP client with reactive streaming, automatic polling, and change detection -- for example, continuously reading sensor data from industrial devices with backpressure handling.

**Choose NModbus** when you need broad protocol coverage (coils, serial transports, server role) or are working with devices that require function codes beyond register read/write.
