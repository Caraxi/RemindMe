using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using RemindMe.Config;

namespace RemindMe.Reminder {
    internal class PetReminder : GeneralReminder {
        private readonly uint[] petJobs = {26, 27, 28};

        private readonly Dictionary<uint, uint> petIcons = new Dictionary<uint, uint>() {
            { 26, 165 },
            { 27, 170 },
            { 28, 17216 },
        };

        [JsonIgnore]
        public override string Name => "Pet Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to summon your pet.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Summon Pet";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            if (!petJobs.Contains(pluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
            if (pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 28 && plugin.ActorsWithStatus.ContainsKey(791) && plugin.ActorsWithStatus[791].Contains(pluginInterface.ClientState.LocalPlayer)) return false;
            if (pluginInterface.ClientState.Actors.Any(a => a.ObjectKind == ObjectKind.BattleNpc && a is BattleNpc bNpc && bNpc.OwnerId == pluginInterface.ClientState.LocalPlayer.ActorId)) return false;
            return true;
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                var action = pluginInterface.Data.Excel.GetSheet<Action>().GetRow(petIcons[pluginInterface.ClientState.LocalPlayer.ClassJob.Id]);
                return action.Icon;
            } catch {
                return 0;
            }
        }

    }
}
