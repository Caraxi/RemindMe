using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class MonkFistsReminder : GeneralReminder {
        [JsonIgnore]
        public override string Name => "Monk Fists Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply a monk stance.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Apply Fists";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return (pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 2 || pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 20) &&
                   pluginInterface.ClientState.LocalPlayer.Level >= 15 &&
                   pluginInterface.ClientState.LocalPlayer.StatusEffects.All(s => s.EffectId != 103 && s.EffectId != 104 && s.EffectId != 105);
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return pluginInterface.Data.Excel.GetSheet<Action>().GetRow(pluginInterface.ClientState.LocalPlayer.Level >= 40 ? 63U : 60U).Icon;
            } catch {
                return 0;
            }
        }

    }
}
