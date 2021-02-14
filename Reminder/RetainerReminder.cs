using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;

namespace RemindMe.Reminder {
    public unsafe class RetainerReminder : GeneralReminder {
        
        [StructLayout(LayoutKind.Explicit, Size = Size)]
        public struct Retainer {
            public const int Size = 0x48;
            [FieldOffset(0x00)] public ulong RetainerID;
            [FieldOffset(0x08)] public fixed byte Name[0x20];
            [FieldOffset(0x29)] public byte Available;
            [FieldOffset(0x29)] public byte ClassJob;
            [FieldOffset(0x2C)] public uint Gil;
            [FieldOffset(0x38)] public uint VentureID;
            [FieldOffset(0x3C)] public uint VentureComplete;
        }
        
        [StructLayout(LayoutKind.Sequential, Size = Retainer.Size + 12)]
        public struct RetainerContainer {
            public fixed byte Retainers[Retainer.Size * 10];
            public fixed byte DisplayOrder[10];
            public byte Ready;
            public byte RetainerCount;
        }

        private static Retainer* _retainers = null;
        private static RetainerContainer* _retainerContainer = null;
        private static bool _isSetup = false;

        [JsonIgnore]
        public override string Name => "Retainer Venture Reminder";

        [JsonIgnore]
        public override string Description => "Shows a notice when retainers are inactive.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            if (_retainers == null || _retainerContainer == null) return "Error";
            if (_retainerContainer->Ready == 0) return "Retainer List Unloaded";
            
            return $"{count} Inactive Retainer{(count>1?"s":"")}";
        }

        private byte count = 0;
        
        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            if (!_isSetup) {
                try {
                    _retainerContainer = (RetainerContainer*) pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 8B E9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 4E");
                    _retainers = (Retainer*) _retainerContainer->Retainers;
                } catch (Exception ex) {
                    PluginLog.LogError(ex, "Failed to find retainer static address");
                }
                
                _isSetup = true;
                return false;
            }

            if (_retainers == null || _retainerContainer == null) return false;
            if (_retainerContainer->Ready == 0) return true;
            count = 0;
            var timeNow = DateTime.UtcNow.Ticks / 10000000L - 62135596800L;
            for (var i = 0; i < _retainerContainer->RetainerCount; i++) {
                var retainer = _retainers[i];
                if (retainer.RetainerID == 0 || retainer.Available == 0 || retainer.ClassJob == 0) continue;
                if (retainer.VentureComplete < timeNow) {
                    count++;
                }
            }
            
            return count > 0;
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return 60; //60840;
        }
    }
}
