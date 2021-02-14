using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Newtonsoft.Json;
using RemindMe.JsonConverters;

namespace RemindMe.Config {

    public class MonitorDisplay {

        [JsonIgnore] private List<DisplayTimer> cachedTimerList;
        [JsonIgnore] private readonly Stopwatch cacheTimerListStopwatch = new Stopwatch();
        [JsonIgnore]
        public List<DisplayTimer> TimerList {
            get {
                if (cachedTimerList == null) return null;
                if (!cacheTimerListStopwatch.IsRunning) return null;
                if (cacheTimerListStopwatch.ElapsedMilliseconds > UpdateInterval) return null;
                return cachedTimerList;
            }
            set {
                cachedTimerList = value;
                cacheTimerListStopwatch.Restart();
            }
        }

        [JsonIgnore]
        public TimeSpan CacheAge => cacheTimerListStopwatch.Elapsed;
        
        private static readonly string[] _displayTypes = new string[] {
            "Horizontal",
            "Vertical",
            "Icons",
        };

        public bool DirectionRtL = false;
        public bool DirectionBtT = false;
        public bool IconVerticalStack = false;

        public int UpdateInterval = 50;

        public bool Enabled = true;
        public Guid Guid;
        public string Name = "New Display";

        public bool Locked = false;
        public bool AllowClicking = false;

        public bool OnlyShowReady = false;
        public bool OnlyShowCooldown = false;

        public int RowSize = 32;
        public float TextScale = 1;
        public int BarSpacing = 5;

        public bool ShowActionIcon = true;
        public float ActionIconScale = 0.9f;
        public bool ReverseSideIcon = false;

        public bool OnlyInCombat = true;
        public bool KeepVisibleOutsideCombat = false;
        public int KeepVisibleOutsideCombatSeconds = 15;

        public bool ShowSkillName = true;
        public bool ShowStatusEffectTarget = true;
        public bool SkillNameRight = false;
        public bool ShowCountdown = false;
        public bool ShowCountdownReady = false;
        public bool ReverseCountdownSide = false;
        public bool StatusOnlyShowTargetName = false;

        public bool PulseReady = false;
        public float PulseSpeed = 1.0f;
        public float PulseIntensity = 1.0f;

        public bool FillToComplete = false;
        public bool ReverseFill = false;
        public RemindMe.FillDirection IconDisplayFillDirection = RemindMe.FillDirection.FromBottom;

        public bool LimitDisplayTime = false;
        public int LimitDisplayTimeSeconds = 10;

        public bool LimitDisplayReadyTime;
        public int LimitDisplayReadyTimeSeconds = 15;

        public List<CooldownMonitor> Cooldowns = new List<CooldownMonitor>();

        public List<StatusMonitor> StatusMonitors = new List<StatusMonitor>();

        public List<GeneralReminder> GeneralReminders = new List<GeneralReminder>();

        public Vector4 AbilityReadyColor = new Vector4(0.70f, 0.25f, 0.25f, 0.75f);
        public Vector4 AbilityCooldownColor = new Vector4(0.75f, 0.125f, 0.665f, 0.75f);
        public Vector4 StatusEffectColor = new Vector4(1f, 0.5f, 0.1f, 0.75f);
        public Vector4 TextColor = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 BarBackgroundColor = new Vector4(0.3f, 0.3f, 0.3f, 0.5f);

        public int DisplayType = 0;

        [JsonIgnore] private bool tryDelete;
        [JsonIgnore] internal bool IsClickableHovered;
        

        public void DrawConfigEditor(RemindMeConfig mainConfig, RemindMe plugin, ref Guid? deletedMonitor) {
            ImGui.Indent(10);
            if (ImGui.Checkbox($"Enabled##{this.Guid}", ref this.Enabled)) mainConfig.Save();
            if (ImGui.Checkbox($"Lock Display##{this.Guid}", ref this.Locked)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputText($"###displayName{this.Guid}", ref this.Name, 32)) mainConfig.Save();
            if (ImGui.Checkbox($"Clickable##{this.Guid}", ref this.AllowClicking)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.TextDisabled("A clickable display will allow selecting targets from status effects\nbut may get in the way of other activity.");
            ImGui.Separator();
            if (ImGui.Combo($"Display Type##{Guid}", ref DisplayType, _displayTypes, _displayTypes.Length)) mainConfig.Save();
            

            if ((DisplayType == 1 || DisplayType == 2) && ImGui.Checkbox($"Right to Left##{Guid}", ref DirectionRtL)) mainConfig.Save();
            if ((DisplayType == 0 || DisplayType == 2) && ImGui.Checkbox($"Bottom to Top##{Guid}", ref DirectionBtT)) mainConfig.Save();
            if (DisplayType == 2 && ImGui.Checkbox($"Vertical Stack##{Guid}", ref IconVerticalStack)) mainConfig.Save();

            ImGui.Separator();
            ImGui.Separator();

            ImGui.Text("Colours");
            ImGui.Separator();

            if (ImGui.ColorEdit4($"Ability Ready##{Guid}", ref AbilityReadyColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Ability Cooldown##{Guid}", ref AbilityCooldownColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Status Effect##{Guid}", ref StatusEffectColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Bar Background##{Guid}", ref BarBackgroundColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Text##{Guid}", ref TextColor)) mainConfig.Save();

            ImGui.Separator();
            ImGui.Separator();
            ImGui.Text("Display Options");
            ImGui.Separator();
            if (ImGui.Checkbox($"Hide outside of combat##{this.Guid}", ref this.OnlyInCombat)) mainConfig.Save();

            if (OnlyInCombat) {
                ImGui.Indent(20);
                if (ImGui.Checkbox($"Keep visible for###keepVisibleOutsideCombat{this.Guid}", ref this.KeepVisibleOutsideCombat)) mainConfig.Save();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputInt($"seconds after exiting combat.###keepVisibleOutsideCombatSeconds{this.Guid}", ref KeepVisibleOutsideCombatSeconds)) mainConfig.Save();
                if (KeepVisibleOutsideCombatSeconds < 0) {
                    KeepVisibleOutsideCombatSeconds = 0;
                    mainConfig.Save();
                }
                ImGui.Indent(-20);
            }

            if (ImGui.Checkbox($"Don't show complete cooldowns##{this.Guid}", ref this.OnlyShowCooldown)) {
                OnlyShowReady = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Only show complete cooldowns##{this.Guid}", ref this.OnlyShowReady)) {
                OnlyShowCooldown = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Fill bar to complete##{this.Guid}", ref this.FillToComplete)) mainConfig.Save();
            if (DisplayType < 2 && ImGui.Checkbox($"Reverse fill direction##{this.Guid}", ref this.ReverseFill)) mainConfig.Save();
            if (DisplayType == 2) { 
                ImGui.BeginGroup();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(22, 22), 0.45f, IconDisplayFillDirection, new Vector4(0.3f, 0.3f, 0.3f, 1), new Vector4(0.8f, 0.8f, 0.8f, 1), 3); 
                ImGui.SameLine();
                ImGui.Text("Fill Direction");
                ImGui.EndGroup();
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
                   IconDisplayFillDirection = (RemindMe.FillDirection) ((((int) IconDisplayFillDirection) + 1) % Enum.GetValues(typeof(RemindMe.FillDirection)).Length);
                }
            }
            

            if (ImGui.Checkbox($"Show Ability Icon##{this.Guid}", ref this.ShowActionIcon)) mainConfig.Save();
            if (this.ShowActionIcon) {
                switch (DisplayType) {
                    case 0: {
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(75);
                        var v = ReverseSideIcon ? 1 : 0;
                        var text = ReverseSideIcon ? "Right" : "Left";
                        ImGui.SliderInt($"###actionIconReverse##{Guid}", ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseSideIcon = !ReverseSideIcon;
                        break;
                    }
                    case 1: {
                        ImGui.SameLine();
                        var v = ReverseSideIcon ? 1 : 0;
                        var text = ReverseSideIcon ? "Top" : "Bottom";
                        ImGui.VSliderInt($"###actionIconReverse##{Guid}", new Vector2(60, 25), ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseSideIcon = !ReverseSideIcon;
                        break;
                    }
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###actionIconScale{this.Guid}", ref this.ActionIconScale, 0.1f, 1f, "Scale")) mainConfig.Save();

            }

            if ((DisplayType == 0 || DisplayType == 1) && ImGui.Checkbox($"Show Skill Name##{this.Guid}", ref this.ShowSkillName)) mainConfig.Save();

            if (DisplayType == 0 && this.ShowSkillName) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(75);
                var v = SkillNameRight ? 1 : 0;
                var text = SkillNameRight ? "Right" : "Left";
                ImGui.SliderInt("###skillNameAlign", ref v, 0, 1, text);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) SkillNameRight = !SkillNameRight;
            }

            if ((DisplayType == 0 || DisplayType == 1) && ShowSkillName && ImGui.Checkbox($"Show Status Effect Target Name##{this.Guid}", ref this.ShowStatusEffectTarget)) mainConfig.Save();
            
            if ((DisplayType == 0 || DisplayType == 1) && ShowSkillName && ShowStatusEffectTarget && ImGui.Checkbox($"Only show target name on status effects##{this.Guid}", ref this.StatusOnlyShowTargetName)) mainConfig.Save();
            
            if (ImGui.Checkbox($"Show Countdown##{this.Guid}", ref this.ShowCountdown)) mainConfig.Save();
            if (ShowCountdown) {

                switch (DisplayType) {
                    case 0: {
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(75);
                        var v = ReverseCountdownSide ? 0 : 1;
                        var text = ReverseCountdownSide ? "Left" : "Right";
                        ImGui.SliderInt($"###actionCountdownReverse##{Guid}", ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseCountdownSide = !ReverseCountdownSide;
                        break;
                    }
                    case 1: {
                        ImGui.SameLine();
                        var v = ReverseCountdownSide ? 0 : 1;
                        var text = ReverseCountdownSide ? "Bottom" : "Top";
                        ImGui.VSliderInt($"###countdownReverse##{Guid}", new Vector2(60, 25), ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseCountdownSide = !ReverseCountdownSide;
                        break;
                    }
                }



                ImGui.Indent(20);
                if (ImGui.Checkbox($"Show Countup while ready##{this.Guid}", ref this.ShowCountdownReady)) mainConfig.Save();
                ImGui.Indent(-20);

            }
            if (ImGui.Checkbox($"Pulse when ready##{this.Guid}", ref this.PulseReady)) mainConfig.Save();

            if (this.PulseReady) {

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###pulseSpeed{this.Guid}", ref this.PulseSpeed, 0.5f, 2f, "Speed")) mainConfig.Save();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###pulseIntensity{this.Guid}", ref this.PulseIntensity, 0.1f, 2f, "Intensity")) mainConfig.Save();


            }

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
            }
            if (ImGui.InputInt($"Bar Spacing##{this.Guid}", ref this.BarSpacing, 1, 2)) {
                if (this.BarSpacing < 0) this.BarSpacing = 0;
                mainConfig.Save();
            }
            if (ImGui.InputFloat($"Text Scale##{this.Guid}", ref this.TextScale, 0.01f, 0.1f)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }
            
            if (ImGui.InputInt($"Update Interval##{this.Guid}", ref this.UpdateInterval, 1, 50)) {
                if (this.UpdateInterval < 1) this.UpdateInterval = 1;
                mainConfig.Save();
            }
            
            ImGui.Separator();


            if (tryDelete) {

                ImGui.Text("Delete this display?");
                ImGui.SameLine();
                if (ImGui.Button($"Don't Delete##{Guid}")) tryDelete = false;
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button($"Delete this display##{Guid}confirm")) deletedMonitor = Guid;
                ImGui.PopStyleColor(3);

            } else {
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button($"Delete this display##{Guid}")) {
                    tryDelete = true;
                }
                ImGui.PopStyleColor(3);
            }

            

            ImGui.Separator();
            ImGui.Indent(-10);
        }

    }
}
