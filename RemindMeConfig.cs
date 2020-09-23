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
using RemindMe.Reminder;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe
{
    public class RemindMeConfig : IPluginConfiguration
    {
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private RemindMe plugin;

        public int Version { get; set; } = 1;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<Guid, Config.MonitorDisplay> MonitorDisplays = new Dictionary<Guid, MonitorDisplay>();

        [JsonIgnore]
        public List<GeneralReminder> GeneralReminders = new List<GeneralReminder>();

        private bool showGlobalCooldowns;
        private const int GlobalCooldownGroup = 58;

        public RemindMeConfig() { }

        public void Init(RemindMe plugin, DalamudPluginInterface pluginInterface)
        {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(GeneralReminder)))) {
                GeneralReminders.Add((GeneralReminder) Activator.CreateInstance(t));
            }
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
        
        public bool DrawConfigUI()
        {
            bool drawConfig = true;
            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 400), new Vector2(1200, 1200));
            ImGui.Begin("Remind Me - Configuration###cooldownMonitorSetup", ref drawConfig);

            ImGui.BeginTabBar("###remindMeConfigTabs");

            if (ImGui.BeginTabItem("Displays")) {
                ImGui.BeginChild("###displaysScroll", ImGui.GetWindowSize() - (ImGui.GetStyle().WindowPadding * 2) - new Vector2(0, ImGui.GetCursorPosY()));
                if (ImGui.Button("Add New Display")) {
                    var guid = Guid.NewGuid();
                    MonitorDisplays.Add(guid, new MonitorDisplay { Guid = guid, Name = $"Display {MonitorDisplays.Count + 1}"});
                    Save();
                }

                if (MonitorDisplays.Count == 0) {
                    ImGui.Text("Add a display to get started.");
                }

                Guid? deletedMonitor = null;
                foreach (var m in MonitorDisplays.Values) {
                    if (ImGui.CollapsingHeader($"{m.Name}###configDisplay{m.Guid}")) {
                        m.DrawConfigEditor(this, ref deletedMonitor);
                    }
                }

                if (deletedMonitor.HasValue) {
                    MonitorDisplays.Remove(deletedMonitor.Value);
                    Save();
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Class Actions")) {

                if (MonitorDisplays.Count > 0) {
                    ImGui.Columns(1 + MonitorDisplays.Values.Count, "###", false);

                    for (var i = 1; i <= MonitorDisplays.Count; i++) {
                        ImGui.SetColumnWidth(i, 100);
                    ImGui.SetColumnWidth(0, 220);
                    }


                    ImGui.Text("Action");
                    ImGui.SameLine();
                    ImGui.Checkbox("Show GCD", ref showGlobalCooldowns);
                    ImGui.NextColumn();

                    foreach (var d in MonitorDisplays.Values) {
                        ImGui.Text(d.Name);
                        ImGui.NextColumn();
                    }

                    ImGui.Separator();
                    ImGui.Separator();
                    var gcdTextSize = ImGui.CalcTextSize("[GCD]");
                    foreach (var a in plugin.ActionList.Where(a => (showGlobalCooldowns || a.CooldownGroup != GlobalCooldownGroup || MonitorDisplays.Any(d => d.Value.Cooldowns.Any(c => c.ActionId == a.RowId && c.ClassJob == pluginInterface.ClientState.LocalPlayer.ClassJob.Id))) && a.IsPvP == false && a.ClassJobCategory.Value.HasClass(pluginInterface.ClientState.LocalPlayer.ClassJob.Id))) {
                        var cdm = new CooldownMonitor { ActionId = a.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id };

                        var icon = plugin.IconManager.GetIconTexture(a.Icon);
                        if (icon != null) {
                            ImGui.Image(icon.ImGuiHandle, new Vector2(25));
                        } else {
                            ImGui.Dummy(new Vector2(24));
                        }
                        ImGui.SameLine();
                        if (a.CooldownGroup == GlobalCooldownGroup) {
                            var x = ImGui.GetCursorPosX();
                            ImGui.SetCursorPosX(ImGui.GetColumnWidth() - gcdTextSize.X);
                            ImGui.TextColored(new Vector4(0.8f), "[GCD]");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(x);
                        }

                        ImGui.Text($"{a.Name}");

                        

                        ImGui.NextColumn();

                        foreach (var d in MonitorDisplays.Values) {
                            
                            var i = d.Cooldowns.Contains(cdm);

                            if (ImGui.Checkbox($"###action{a.RowId}in{d.Guid}", ref i)) {
                                try {
                                    if (i) {
                                        d.Cooldowns.Add(cdm);
                                    } else {
                                        d.Cooldowns.Remove(cdm);
                                    }
                                    Save();
                                } catch (Exception ex) {
                                    PluginLog.LogError(ex.ToString());
                                }
                            }

                            ImGui.NextColumn();
                        }

                        ImGui.Separator();

                    }

                    ImGui.Columns(1);

                    ImGui.TextWrapped("\nSomething Missing?\nGlobal Cooldown skills aren't shown but if anything else is missing please let Caraxi know on the goat place discord and it will be added.");

                } else {
                    ImGui.Text("No display setup.");
                }

                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Status Effects")) {
                if (MonitorDisplays.Count > 0) {

                    ImGui.Columns(1 + MonitorDisplays.Count, "###statusColumns", false);
                    ImGui.SetColumnWidth(0, 180);
                    for (var i = 1; i <= MonitorDisplays.Count; i++) {
                        ImGui.SetColumnWidth(i, 100);
                    }
                    ImGui.Text("Status");
                    ImGui.NextColumn();
                    foreach (var m in MonitorDisplays.Values) {
                        ImGui.Text(m.Name);
                        ImGui.NextColumn();
                    }

                    ImGui.Separator();

                    switch (pluginInterface.ClientState.LocalPlayer.ClassJob.Id) {
                        case 20: {
                            // MNK
                            StatusMonitorConfigDisplay(20, 66, 246, 18); // Demolish
                            break;
                        }
                        case 21: {
                            // WAR
                            StatusMonitorConfigDisplay(21, 45, 90, 60); // Storm's Path
                            break;
                        }
                        case 22: {
                            // DRG
                            StatusMonitorConfigDisplay(22, 87, 1914, 30); // Disembowment
                            StatusMonitorConfigDisplay(22, 88, 118, 24); // Chaos Thrust
                            break;
                        }
                        case 23: {
                            // BRD
                            StatusMonitorConfigDisplay(23, 7406, 1200, 30); // Causic Bite
                            StatusMonitorConfigDisplay(23, 7407, 1201, 30); // Stormbite
                            break;
                        }
                        case 24: {
                            // WHM
                            StatusMonitorConfigDisplay(24, 16532, 1871, 30); // Dia
                            StatusMonitorConfigDisplay(24, 137, 158, 18); // Regen
                            StatusMonitorConfigDisplay(24, 133, 150, 15); // Medica II

                            break;
                        }
                        case 25: {
                            // BLM
                            StatusMonitorConfigDisplay(25, 144, 161, 24); // Thunder
                            StatusMonitorConfigDisplay(25, 7447, 162, 24); // Thunder II
                            StatusMonitorConfigDisplay(25, 153, 163, 24); // Thunder III
                            StatusMonitorConfigDisplay(25, 7420, 1210, 18); // Thunder IV
                            break;
                        }
                        case 27: {
                            // SMN
                            StatusMonitorConfigDisplay(27, 164, 179, 30); // Bio
                            StatusMonitorConfigDisplay(27, 168, 180, 30); // Miasma
                            StatusMonitorConfigDisplay(27, 178, 189, 30); // Bio II
                            StatusMonitorConfigDisplay(27, 7424, 1214, 30); // Bio III
                            StatusMonitorConfigDisplay(27, 7425, 1215, 30); // Miasma III
                            break;
                        }
                        case 28: {
                            // SCH
                            StatusMonitorConfigDisplay(28, 17864, 179, 30); // Bio
                            StatusMonitorConfigDisplay(28, 17865, 189, 30); // Bio II
                            StatusMonitorConfigDisplay(28, 16540, 1895, 30); // Biolysis
                            break;
                        }
                        case 30: {
                            // NIN
                            StatusMonitorConfigDisplay(30, 2257, 508, 30); // Shadow Fang
                            break;
                        }
                        case 31: {
                            // MCH
                            // 1866
                            StatusMonitorConfigDisplay(31, 16499, 1866, 15); // Bio Blaster
                            break;
                        }
                        case 33: {
                            // AST
                            StatusMonitorConfigDisplay(33, 3599, 838, 30); // Combust
                            StatusMonitorConfigDisplay(33, 3608, 843, 30); // Combust II
                            StatusMonitorConfigDisplay(33, 16554, 1881, 30); // Combust III
                            StatusMonitorConfigDisplay(33, 3595, 835, 15, "Diurnal"); // Aspected Benific (Regen)
                            StatusMonitorConfigDisplay(33, 3601, 836, 15, "Diurnal"); // Aspected Helios (Regen)
                            break;
                        }
                        case 34: {
                            // SAM
                            StatusMonitorConfigDisplay(34, 7489, 1228, 60); // Higanbana
                            break;
                        }
                        default: {
                            ImGui.Columns(1);
                            ImGui.TextWrapped($"No status monitors are available on {pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Name}.");
                            break;
                        }
                    }

                    ImGui.Columns(1);

                    ImGui.TextWrapped("\nSomething Missing? Please let Caraxi know on the goat place discord and it will be added.");

                } else {
                    ImGui.Text("No Monitor setup");
                }
                
                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Reminders")) {

                ImGui.Columns(1 + MonitorDisplays.Count, "###remindersColumns", false);
                ImGui.SetColumnWidth(0, 180);
                for (var i = 1; i <= MonitorDisplays.Count; i++) {
                    ImGui.SetColumnWidth(i, 100);
                }
                ImGui.Text("Reminder");
                ImGui.NextColumn();
                foreach (var m in MonitorDisplays.Values) {
                    ImGui.Text(m.Name);
                    ImGui.NextColumn();
                }
                ImGui.Separator();

                foreach (var r in GeneralReminders) {
                    ImGui.Text(r.Name);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1));
                    ImGui.TextWrapped(r.Description);
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();

                    foreach (var m in MonitorDisplays.Values) {

                        var enabled = m.GeneralReminders.Contains(r);
                        if (ImGui.Checkbox($"###generalReminder{m.Guid}_{r.GetType().Name}", ref enabled)) {
                            if (enabled) {
                                m.GeneralReminders.Add(r);
                            } else {
                                m.GeneralReminders.Remove(r);
                            }
                            Save();
                        }
                        ImGui.NextColumn();
                    }

                }

                ImGui.Columns(1);


                ImGui.EndTabItem();
            }

#if DEBUG
            if (ImGui.BeginTabItem("Debug")) {
                try {
                    ImGui.Text($"Current ClassJobID: {pluginInterface.ClientState.LocalPlayer.ClassJob.Id}");
                    ImGui.Text($"Current Level: {pluginInterface.ClientState.LocalPlayer.Level}");
                    ImGui.Text($"Not In Combat for: {plugin.OutOfCombatTimer.Elapsed.TotalSeconds} seconds.");

                    if (pluginInterface.ClientState.Targets.CurrentTarget != null) {
                        ImGui.Text("\nEffects on Target: ");
                        foreach (var se in pluginInterface.ClientState.Targets.CurrentTarget.GetStatusEffects()) {
                            if (se.EffectId <= 0) continue;
                            var status = pluginInterface.Data.Excel.GetSheet<Status>().GetRow((uint)se.EffectId);
                            ImGui.Text($"\t{status.Name}: {status.RowId}");
                        }
                    }


                    ImGui.Text("\nEffects on Self: ");
                    foreach (var se in pluginInterface.ClientState.LocalPlayer.GetStatusEffects()) {
                        if (se.EffectId <= 0) continue;
                        var status = pluginInterface.Data.Excel.GetSheet<Status>().GetRow((uint)se.EffectId);
                        ImGui.Text($"\t{status.Name}: {status.RowId}");
                    }

                    var lastAction = pluginInterface.Data.GetExcelSheet<Action>().GetRow(plugin.ActionManager.LastActionId);
                    ImGui.Text(lastAction != null ? $"\nLast Action: [{lastAction.RowId}] {lastAction.Name}" : $"\nLast Action: [{plugin.ActionManager.LastActionId}] Unknown");
                     
                    if (lastAction != null) {
                        var ptr = plugin.ActionManager.GetCooldownPointer(lastAction.CooldownGroup).ToInt64().ToString("X");
                        ImGui.InputText("Cooldown Ptr", ref ptr, 16, ImGuiInputTextFlags.ReadOnly);
                    }

                    ImGui.Text($"Last Action Max Charges: {lastAction.MaxCharges}");


                } catch {
                    // ignored
                }

                ImGui.EndTabItem();

            }
#endif
            ImGui.EndTabBar();
            ImGui.End();

            return drawConfig;
        }

        private void StatusMonitorConfigDisplay(uint classJob, uint actionId, uint statusId, float masDuration, string note = null) {
            var status = pluginInterface.Data.GetExcelSheet<Status>().GetRow(statusId);
            var action = pluginInterface.Data.GetExcelSheet<Action>().GetRow(actionId);
            if (status != null && action != null) {
                var statusMonitor = new StatusMonitor {Status = status.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id, Action = action.RowId, MaxDuration = masDuration};

                if (action.Name != status.Name) {
                    ImGui.Text($"{status.Name} ({action.Name})");
                } else {
                    ImGui.Text(status.Name);
                }

                if (!string.IsNullOrEmpty(note)) {
                    ImGui.SameLine();
                    ImGui.Text($"({note})");
                }

                
                ImGui.NextColumn();

                foreach (var s in MonitorDisplays.Values) {
                    var enabled = s.StatusMonitors.Contains(statusMonitor);
                    if (ImGui.Checkbox($"###statusToggle{s.Guid}_{status.RowId}", ref enabled)) {
                        if (enabled) {
                            s.StatusMonitors.Add(statusMonitor);
                        } else {
                            s.StatusMonitors.Remove(statusMonitor);
                        }
                        Save();
                    }
                    ImGui.NextColumn();
                }


            }

        }

    }
}