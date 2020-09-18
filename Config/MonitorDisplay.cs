using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace RemindMe.Config {

    [JsonObject(ItemTypeNameHandling = TypeNameHandling.None)]
    public class MonitorDisplay {
        public bool Enabled = false;
        public Guid Guid;
        public string Name = "New Display";

        public bool Locked = false;

        public bool OnlyShowReady = false;
        public bool OnlyShowCooldown = false;

        public int RowSize = 32;
        public float TextScale = 1;

        public bool ShowActionIcon = true;
        public bool OnlyInCombat = true;

        public bool ShowCountdown = false;
        public bool ShowCountdownReady = false;

        public bool PulseReady = false;

        public bool LimitDisplayTime = false;
        public int LimitDisplayTimeSeconds = 10;

        public bool LimitDisplayReadyTime;
        public int LimitDisplayReadyTimeSeconds = 15;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public List<CooldownMonitor> Cooldowns = new List<CooldownMonitor>();

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public List<StatusMonitor> StatusMonitors = new List<StatusMonitor>();


        public Vector4 AbilityReadyColor = new Vector4(0.70f, 0.25f, 0.25f, 0.75f);
        public Vector4 AbilityCooldownColor = new Vector4(0.75f, 0.125f, 0.665f, 0.75f);
        public Vector4 StatusEffectColor = new Vector4(1f, 0.5f, 0.1f, 0.75f);
        public Vector4 TextColor = new Vector4(1f, 1f, 1f, 1f);

        [JsonIgnore] private bool tryDelete;
        

        public void DrawConfigEditor(RemindMeConfig mainConfig, ref Guid? deletedMonitor) {
            if (ImGui.Checkbox($"Lock Display##{this.Guid}", ref this.Locked)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputText($"###displayName{this.Guid}", ref this.Name, 32)) mainConfig.Save();
            ImGui.Separator();
            ImGui.Text("Colours");
            ImGui.Separator();

            if (ImGui.ColorEdit4($"Ability Ready##{Guid}", ref AbilityReadyColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Ability Cooldown##{Guid}", ref AbilityCooldownColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Status Effect##{Guid}", ref StatusEffectColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Text##{Guid}", ref TextColor)) mainConfig.Save();

            ImGui.Separator();
            ImGui.Separator();
            ImGui.Text("Display Options");
            ImGui.Separator();
            if (ImGui.Checkbox($"Only show while in combat##{this.Guid}", ref this.OnlyInCombat)) mainConfig.Save();
            if (ImGui.Checkbox($"Don't show complete cooldowns##{this.Guid}", ref this.OnlyShowCooldown)) {
                OnlyShowReady = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Only show complete cooldowns##{this.Guid}", ref this.OnlyShowReady)) {
                OnlyShowCooldown = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Show Ability Icon##{this.Guid}", ref this.ShowActionIcon)) mainConfig.Save();
            if (ImGui.Checkbox($"Show Countdown##{this.Guid}", ref this.ShowCountdown)) mainConfig.Save();
            if (ShowCountdown && ImGui.Checkbox($"  > Show Countup while ready##{this.Guid}", ref this.ShowCountdownReady)) mainConfig.Save();
            if (ImGui.Checkbox($"Pulse when ready##{this.Guid}", ref this.PulseReady)) mainConfig.Save();
            ImGui.Separator();
            if (ImGui.Checkbox($"###limitDisplay{this.Guid}", ref this.LimitDisplayTime)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.Text("Only show when below");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt($"seconds##limitSeconds{this.Guid}", ref LimitDisplayTimeSeconds)) mainConfig.Save();

            if (ImGui.Checkbox($"###limitDisplayReady{this.Guid}", ref this.LimitDisplayReadyTime)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.Text("Don't show ready abilities after");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt($"seconds##limitReadySeconds{this.Guid}", ref LimitDisplayReadyTimeSeconds)) mainConfig.Save();

            ImGui.Separator();
            if (ImGui.InputInt($"Bar Height##{this.Guid}", ref this.RowSize, 1, 5)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }if (ImGui.InputFloat($"Text Scale##{this.Guid}", ref this.TextScale, 0.01f, 0.1f)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }
            ImGui.Separator();


            if (tryDelete) {

                ImGui.Text("Delete this monitor?");
                ImGui.SameLine();
                if (ImGui.Button("Don't Delete")) tryDelete = false;
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button("Delete this display")) deletedMonitor = Guid;
                ImGui.PopStyleColor(3);

            } else {
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button("Delete this display")) {
                    tryDelete = true;
                }
                ImGui.PopStyleColor(3);
            }

            

            ImGui.Separator();

        }

    }
}
