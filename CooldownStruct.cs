using System.Runtime.InteropServices;

namespace RemindMe {
    [StructLayout(LayoutKind.Explicit)]
    public struct CooldownStruct {

    [FieldOffset(0x0)] public bool IsCooldown;

    [FieldOffset(0x4)] public uint ActionID;
    [FieldOffset(0x8)] public float CooldownElapsed;
    [FieldOffset(0xC)] public float CooldownTotal;

    }
}
