namespace TurboModbus;

public sealed class ModbusException(byte functionCode, byte errorCode)
    : Exception($"Modbus error: FC 0x{functionCode:X2}, exception code {errorCode} ({Describe(errorCode)})")
{
    public byte FunctionCode { get; } = functionCode;
    public byte ErrorCode { get; } = errorCode;

    private static string Describe(byte code) => code switch
    {
        1 => "Illegal Function",
        2 => "Illegal Data Address",
        3 => "Illegal Data Value",
        4 => "Slave Device Failure",
        _ => "Unknown"
    };
}
