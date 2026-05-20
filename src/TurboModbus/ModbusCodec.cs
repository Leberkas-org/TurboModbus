namespace TurboModbus;

using System.Buffers.Binary;
using Akka;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;

public static class ModbusCodec
{
    public static Flow<ReadRequest, RegisterResponse, NotUsed> ReadFlow(
        ActorSystem system, string host, int port, byte unitId = 1)
    {
        var txId = 0;
        return Flow.Create<ReadRequest>()
            .Select(req =>
            {
                var id = (ushort)(Interlocked.Increment(ref txId) & 0xFFFF);
                var fc = req.InputRegister ? FunctionCode.ReadInputRegisters : FunctionCode.ReadHoldingRegisters;
                return new ModbusRequest(id, unitId, fc, req.Address, [req.Count]);
            })
            .Via(RawTcpFlow(system, host, port))
            .Select(resp => new RegisterResponse(resp.Registers.Length > 0 ? (ushort)0 : (ushort)0, resp.Registers));
    }

    internal static Flow<ModbusRequest, ModbusResponse, NotUsed> RawTcpFlow(
        ActorSystem system, string host, int port)
    {
        var tcp = system.TcpStream().OutgoingConnection(host, port);
        return Framing().Join(tcp);
    }

    internal static BidiFlow<ModbusRequest, ByteString, ByteString, ModbusResponse, NotUsed> Framing()
    {
        return BidiFlow.FromFlowsMat(
            Flow.Create<ModbusRequest>().Select(Encode),
            Flow.Create<ByteString>().Via(new MbapFramingStage()).Select(Decode),
            Keep.None);
    }

    internal static ByteString Encode(ModbusRequest req)
    {
        var pdu = EncodePdu(req);
        var mbap = new byte[7 + pdu.Length];

        BinaryPrimitives.WriteUInt16BigEndian(mbap.AsSpan(0), req.TransactionId);
        BinaryPrimitives.WriteUInt16BigEndian(mbap.AsSpan(2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(mbap.AsSpan(4), (ushort)(pdu.Length + 1));
        mbap[6] = req.UnitId;
        pdu.CopyTo(mbap.AsSpan(7));

        return ByteString.FromBytes(mbap);
    }

    internal static ModbusResponse Decode(ByteString frame)
    {
        var bytes = frame.ToArray();
        var transactionId = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(0));
        var unitId = bytes[6];
        var functionCode = bytes[7];

        if ((functionCode & 0x80) != 0)
            return new ModbusResponse(transactionId, unitId, (FunctionCode)functionCode, [bytes[8]]);

        return (FunctionCode)functionCode switch
        {
            FunctionCode.ReadHoldingRegisters or FunctionCode.ReadInputRegisters =>
                DecodeReadResponse(transactionId, unitId, functionCode, bytes),

            FunctionCode.WriteSingleRegister or FunctionCode.WriteMultipleRegisters =>
                new ModbusResponse(transactionId, unitId, (FunctionCode)functionCode,
                    [BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(8)),
                     BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(10))]),

            _ => throw new NotSupportedException($"Function code 0x{functionCode:X2}")
        };
    }

    private static ModbusResponse DecodeReadResponse(ushort txId, byte unitId, byte fc, byte[] bytes)
    {
        var byteCount = bytes[8];
        var registers = new ushort[byteCount / 2];
        for (int i = 0; i < registers.Length; i++)
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(9 + i * 2));
        return new ModbusResponse(txId, unitId, (FunctionCode)fc, registers);
    }

    private static byte[] EncodePdu(ModbusRequest req) => req.Function switch
    {
        FunctionCode.ReadHoldingRegisters or FunctionCode.ReadInputRegisters =>
            EncodeSimplePdu(req),
        FunctionCode.WriteSingleRegister =>
            EncodeSimplePdu(req),
        FunctionCode.WriteMultipleRegisters =>
            EncodeWriteMultiplePdu(req),
        _ => throw new NotSupportedException($"Function code {req.Function}")
    };

    private static byte[] EncodeSimplePdu(ModbusRequest req)
    {
        var buf = new byte[5];
        buf[0] = (byte)req.Function;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(1), req.StartAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(3), req.Data[0]);
        return buf;
    }

    private static byte[] EncodeWriteMultiplePdu(ModbusRequest req)
    {
        var byteCount = req.Data.Length * 2;
        var buf = new byte[6 + byteCount];
        buf[0] = (byte)req.Function;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(1), req.StartAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(3), (ushort)req.Data.Length);
        buf[5] = (byte)byteCount;
        for (int i = 0; i < req.Data.Length; i++)
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(6 + i * 2), req.Data[i]);
        return buf;
    }
}
