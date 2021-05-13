using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using RemindMe.Config;

namespace RemindMe
{
    public partial class RemindMeConfig : IPluginConfiguration {
        public uint InstallNoticeDismissed = 0;

        [NonSerialized] private float debugFraction = 0;

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private RemindMe plugin;

        public int Version { get; set; } = 2;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<Guid, Config.MonitorDisplay> MonitorDisplays = new Dictionary<Guid, MonitorDisplay>();

        [JsonIgnore]
        public List<GeneralReminder> GeneralReminders = new List<GeneralReminder>();

        private bool showGlobalCooldowns;
        public long PollingRate = 100;
        private const int GlobalCooldownGroup = 58;

        [JsonIgnore] private List<StatusMonitor> visibleStatusMonitor = new();

        public RemindMeConfig() { }

        public void Init(RemindMe plugin, DalamudPluginInterface pluginInterface)
        {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(GeneralReminder)))) {
                GeneralReminders.Add((GeneralReminder) Activator.CreateInstance(t));
            }

            if (Version == 1) {
                // Update to Version 2
                // Remove Status Monitors with ClassJob of 0
                Version = 2;
                if (MonitorDisplays.Count > 0) {
                    foreach (var a in MonitorDisplays.Values) {
                        a.StatusMonitors.RemoveAll(a => a.ClassJob == 0);
                    }
                }
                Save();
            }
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
        
        public bool DrawConfigUI() {

            visibleStatusMonitor.Clear();
            
            bool drawConfig = true;
            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 400), new Vector2(1200, 1200));
            if (!ImGui.Begin($"{plugin.Name} - Configuration###cooldownMonitorSetup", ref drawConfig)) return drawConfig;
            if (InstallNoticeDismissed != 1) {
                
                ImGui.TextWrapped($"Thank you for installing {plugin.Name}.\nI am currently working on completely rewriting the plugin but please don't hesitate to bring up any issues you have with the current version. Things seem to be relatively stable, but some things may still pop up.");
                
                if (ImGui.SmallButton("Dismiss")) {
                    InstallNoticeDismissed = 1;
                    Save();
                }
                ImGui.Separator();
            }

            ImGui.BeginTabBar("###remindMeConfigTabs");

            if (ImGui.BeginTabItem("Displays")) {
                DrawDisplaysTab();
                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Values.Count(d => d.Enabled) > 0) {
                if (ImGui.BeginTabItem("Actions")) {
                    DrawActionsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Status Effects")) {
                    DrawStatusEffectsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Raid Effects")) {
                    DrawRaidEffectsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Reminders")) {
                    DrawRemindersTab();
                    ImGui.EndTabItem();
                }
            }

#if DEBUG
            if (ImGui.BeginTabItem("Debug")) {
                DrawDebugTab();
                ImGui.EndTabItem();

            }
#endif
            ImGui.EndTabBar();
            ImGui.End();

            return drawConfig;
        }

        private void StatusMonitorConfigDisplay(StatusMonitor statusMonitor, Status status = null, string forcedName = null, string note = null, bool removeOnly = false) {
            status ??= pluginInterface.Data.GetExcelSheet<Status>().GetRow(statusMonitor.Status);
            if (status == null) return;
            
            if (!visibleStatusMonitor.Contains(statusMonitor)) visibleStatusMonitor.Add(statusMonitor);
            var statusIcon = plugin.IconManager.GetIconTexture(status.Icon);
            if (statusIcon != null) {
                ImGui.Image(statusIcon.ImGuiHandle, new Vector2(18, 24));
            } else {
                ImGui.Dummy(new Vector2(18, 24));
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip(status.Description);
            }

            if (statusMonitor.StatusList != null) {
                foreach (var s in statusMonitor.StatusList) {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
                    var extraStatus = pluginInterface.Data.GetExcelSheet<Status>().GetRow(s);
                    if (extraStatus == null) continue;
                    var extraStatusIcon = plugin.IconManager.GetIconTexture(extraStatus.Icon);
                    if (extraStatusIcon == null) continue;
                    ImGui.Image(extraStatusIcon.ImGuiHandle, new Vector2(18, 24));
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(extraStatus.Description);
                    }
                }
            }

            ImGui.SameLine();

            ImGui.Text(forcedName ?? status.Name);

            if (!string.IsNullOrEmpty(note)) {
                ImGui.SameLine();
                ImGui.Text($"({note})");
            }


            var typeText = "";
            if (statusMonitor.SelfOnly) typeText += "SELF";
            if (statusMonitor.AlwaysAvailable && statusMonitor.SelfOnly) typeText += ",";
            if (statusMonitor.AlwaysAvailable) typeText += "PERMA";
            
            if (!string.IsNullOrEmpty(typeText)) {
                var typeTextSize = ImGui.CalcTextSize($"[{typeText}]");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetColumnWidth() - typeTextSize.X);
                ImGui.TextDisabled($"[{typeText}]");
            }


            ImGui.NextColumn();

            foreach (var s in MonitorDisplays.Values.Where(d => d.Enabled)) {
                var enabled = s.StatusMonitors.Contains(statusMonitor);
                if (removeOnly && !enabled) {
                    ImGui.NextColumn();
                    continue;
                }
                if (ImGui.Checkbox($"###statusToggle{s.Guid}_{status.RowId}_{visibleStatusMonitor.Count}", ref enabled)) {
                    if (enabled && !removeOnly) {
                        s.StatusMonitors.Add(statusMonitor);
                    } else {
                        s.StatusMonitors.Remove(statusMonitor);
                    }
                    Save();
                }
                ImGui.NextColumn();
            }

            ImGui.Separator();
        }
        
        private void StatusMonitorConfigDisplay(uint statusId, float maxDuration, string note = null, bool raid = false, bool selfOnly = false, uint[] statusList = null, string forcedName = null, ushort limitedZone = 0, bool stacking = false, bool alwaysAvailable = false, byte minLevel = byte.MinValue, byte maxLevel = byte.MaxValue) {
            var status = pluginInterface.Data.GetExcelSheet<Status>().GetRow(statusId);
            if (status == null) return;
            var statusMonitor = new StatusMonitor {Status = status.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id, MaxDuration = maxDuration, SelfOnly = selfOnly, StatusList = statusList, IsRaid = raid, LimitedZone = limitedZone, Stacking = stacking, AlwaysAvailable = alwaysAvailable, MinLevel = minLevel, MaxLevel = maxLevel};
            StatusMonitorConfigDisplay(statusMonitor, status, forcedName, note);
        }

    }
}