namespace TurboModbus;

public sealed record ReadRequest(ushort Address, ushort Count, bool InputRegister = false) : IWithModbusAddress;

public sealed record WriteRequest(ushort Address, ushort[] Values) : IWithModbusAddress;

public sealed record RegisterResponse(ushort Address, ushort[] Registers) : IWithModbusAddress;

public interface IWithModbusAddress
{
    public ushort Address { get; }
}
