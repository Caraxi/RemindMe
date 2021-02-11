using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class AetherialMimicryReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Aetherial Mimicry Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply mimicry.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Aetherial Mimicry";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display)
        {
            return pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 36 &&
                   pluginInterface.ClientState.LocalPlayer.StatusEffects.All(s => s.EffectId != 2124 && s.EffectId != 2125 && s.EffectId != 2126);
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return pluginInterface.Data.Excel.GetSheet<Action>().GetRow(18322).Icon;
            } catch {
                return 0;
            }
        }

    }
}
