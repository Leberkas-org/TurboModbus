namespace TurboModbus;

public enum FunctionCode : byte
{
    ReadHoldingRegisters = 0x03,
    ReadInputRegisters = 0x04,
    WriteSingleRegister = 0x06,
    WriteMultipleRegisters = 0x10,
}

internal sealed record ModbusRequest(
    ushort TransactionId,
    byte UnitId,
    FunctionCode Function,
    ushort StartAddress,
    ushort[] Data);

internal sealed record ModbusResponse(
    ushort TransactionId,
    byte UnitId,
    FunctionCode Function,
    ushort[] Registers)
{
    public bool IsError => ((byte)Function & 0x80) != 0;
    public byte ErrorCode => Registers.Length > 0 ? (byte)Registers[0] : (byte)0;
}
