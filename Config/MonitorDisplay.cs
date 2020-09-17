using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RemindMe.Config {

    [JsonObject(ItemTypeNameHandling = TypeNameHandling.None)]
    public class MonitorDisplay {
        public bool Enabled = false;
        public Guid Guid;
        public string Name = "New Display";

        public bool Locked = false;

        public bool OnlyShowReady = false;

        public int RowSize = 24;

        public bool ShowActionIcon = true;
        public bool OnlyInCombat = false;

        public bool ShowCountdown = false;

        public bool LimitDisplayTime = false;
        public int LimitDisplayTimeSeconds = 10;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public List<CooldownMonitor> Cooldowns = new List<CooldownMonitor>();

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public List<StatusMonitor> StatusMonitors = new List<StatusMonitor>();


        public void DrawConfigEditor(RemindMeConfig mainConfig, ref Guid? deletedMonitor) {
            if (this.Cooldowns.Count > 0) {
                ImGui.Text("Can't delete while contains actions");
            } else {
                if (ImGui.Button("Delete Display")) {
                    deletedMonitor = this.Guid;
                }
            }

            if (ImGui.Checkbox($"Lock Display##{this.Guid}", ref this.Locked)) mainConfig.Save();
            if (ImGui.Checkbox($"Only show ready##{this.Guid}", ref this.OnlyShowReady)) mainConfig.Save();
            if (ImGui.Checkbox($"Only show while in combat##{this.Guid}", ref this.OnlyInCombat)) mainConfig.Save();
            if (ImGui.Checkbox($"Show Icon##{this.Guid}", ref this.ShowActionIcon)) mainConfig.Save();
            if (ImGui.Checkbox($"Show Countdown##{this.Guid}", ref this.ShowCountdown)) mainConfig.Save();
            

            if (ImGui.InputInt($"Bar Size##{this.Guid}", ref this.RowSize, 1, 5)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }


            ImGui.SetNextItemWidth(150);
            if (ImGui.InputText($"Display Name###displayName{this.Guid}", ref this.Name, 32)) mainConfig.Save();

            if (ImGui.Checkbox($"###limitDisplay{this.Guid}", ref this.LimitDisplayTime)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.Text("Only show when below");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt($"seconds##limitSeconds{this.Guid}", ref LimitDisplayTimeSeconds)) mainConfig.Save();


        }

    }
}
