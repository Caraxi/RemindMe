using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class DancePartnerReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Dance Partner Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to select a dance partner.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Dance Partner";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 38  && 
                   pluginInterface.ClientState.LocalPlayer.Level >= 60 && 
                   pluginInterface.ClientState.LocalPlayer.StatusEffects.All(s => s.EffectId != 1823);
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return pluginInterface.Data.Excel.GetSheet<Action>().GetRow(16006).Icon;
            } catch {
                return 0;
            }
        }

    }
}
