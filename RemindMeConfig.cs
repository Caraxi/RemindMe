using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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


    public class RemindMeConfig : IPluginConfiguration {
        [NonSerialized] public bool noticeDismissed;

        [NonSerialized] private float debugFraction = 0;

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
        public long PollingRate = 100;
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
        
        public bool DrawConfigUI() {

            bool drawConfig = true;
            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 400), new Vector2(1200, 1200));
            ImGui.Begin($"Remind Me - Configuration###cooldownMonitorSetup", ref drawConfig);
            if (!noticeDismissed) {
                ImGui.TextWrapped("RemindMe is currently still in the testing phase. Bugs and crashes are very possible. Please report any issues you have so that they can be resolved.");
                
                noticeDismissed = ImGui.SmallButton("Okay");
            }

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
                        m.DrawConfigEditor(this, plugin, ref deletedMonitor);
                    }
                }

                if (deletedMonitor.HasValue) {
                    MonitorDisplays.Remove(deletedMonitor.Value);
                    Save();
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Actions")) {
                
                if (MonitorDisplays.Count > 0) {
                    ImGui.Columns(1 + MonitorDisplays.Values.Count, "###", false);
                    ImGui.SetColumnWidth(0, 220);
                    for (var i = 1; i <= MonitorDisplays.Count; i++) {
                        ImGui.SetColumnWidth(i, 100);
                    }

                    ImGui.Text("Action");
                    ImGui.SameLine(80);

                    ImGui.Text("Show GCD");
                    ImGui.SameLine();
                    ImGui.SetWindowFontScale(0.7f);
                    ImGui.Checkbox("###showGCDCheckbox", ref showGlobalCooldowns);
                    ImGui.SetWindowFontScale(1);
                    
                    ImGui.NextColumn();

                    foreach (var d in MonitorDisplays.Values) {
                        ImGui.Text(d.Name);
                        ImGui.NextColumn();
                    }

                    ImGui.Separator();
                    ImGui.Separator();
                    ImGui.Columns(1);
                    ImGui.BeginChild("###scrolling", new Vector2(-1));
                    ImGui.Columns(1 + MonitorDisplays.Values.Count, "###", false);
                    ImGui.SetColumnWidth(0, 220);
                    for (var i = 1; i <= MonitorDisplays.Count; i++) {
                        ImGui.SetColumnWidth(i, 100);
                    }
                    var gcdTextSize = ImGui.CalcTextSize("[GCD]");
                    foreach (var a in plugin.ActionList.Where(a => (showGlobalCooldowns || a.CooldownGroup != GlobalCooldownGroup || MonitorDisplays.Any(d => d.Value.Cooldowns.Any(c => c.ActionId == a.RowId && c.ClassJob == pluginInterface.ClientState.LocalPlayer.ClassJob.Id))) && a.IsPvP == false && a.ClassJobCategory.Value.HasClass(pluginInterface.ClientState.LocalPlayer.ClassJob.Id))) {
                        var cdm = new CooldownMonitor { ActionId = a.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id };

                        var icon = plugin.IconManager.GetActionIcon(a);
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

                        ImGui.Text(a.Name);

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

                    ImGui.TextWrapped("\nSomething Missing?\nPlease let Caraxi know on the goat place discord so it can be fixed.");

                } else {
                    ImGui.Text("No display setup.");
                }

                ImGui.EndChild();

                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Status Effects")) {
                
                if (MonitorDisplays.Count > 0) {

                    ImGui.Columns(1 + MonitorDisplays.Count, "###statusColumns", false);
                    ImGui.SetColumnWidth(0, 220);
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
                    ImGui.Separator();
                    ImGui.Columns(1);
                    ImGui.BeginChild("###scrolling", new Vector2(-1));
                    ImGui.Columns(1 + MonitorDisplays.Count, "###statusColumns", false);
                    ImGui.SetColumnWidth(0, 220);
                    for (var i = 1; i <= MonitorDisplays.Count; i++) {
                        ImGui.SetColumnWidth(i, 100);
                    }
                    switch (pluginInterface.ClientState.LocalPlayer.ClassJob.Id) {
                        case 20: {
                            // MNK
                            StatusMonitorConfigDisplay(246, 18); // Demolish
                            break;
                        }
                        case 21: {
                            // WAR
                            StatusMonitorConfigDisplay(90, 60, selfOnly: true); // Storm's Path
                            break;
                        }
                        case 22: {
                            // DRG
                            StatusMonitorConfigDisplay(1914, 30); // Disembowment
                            StatusMonitorConfigDisplay(118, 24); // Chaos Thrust
                            break;
                        }
                        case 23: {
                            // BRD
                            StatusMonitorConfigDisplay(1200, 30); // Causic Bite
                            StatusMonitorConfigDisplay(1201, 30); // Stormbite
                            break;
                        }
                        case 24: {
                            // WHM
                            StatusMonitorConfigDisplay(143, 18); // Aero
                            StatusMonitorConfigDisplay(144, 18); // Aero II
                            StatusMonitorConfigDisplay(1871, 30); // Dia
                            StatusMonitorConfigDisplay(158, 18); // Regen
                            StatusMonitorConfigDisplay(150, 15); // Medica II

                            break;
                        }
                        case 25: {
                            // BLM
                            StatusMonitorConfigDisplay(161, 24); // Thunder
                            StatusMonitorConfigDisplay(162, 24); // Thunder II
                            StatusMonitorConfigDisplay(163, 24); // Thunder III
                            StatusMonitorConfigDisplay(1210, 18); // Thunder IV
                            break;
                        }
                        case 27: {
                            // SMN
                            StatusMonitorConfigDisplay(179, 30); // Bio
                            StatusMonitorConfigDisplay(180, 30); // Miasma
                            StatusMonitorConfigDisplay(189, 30); // Bio II
                            StatusMonitorConfigDisplay(1214, 30); // Bio III
                            StatusMonitorConfigDisplay(1215, 30); // Miasma III
                            break;
                        }
                        case 28: {
                            // SCH
                            StatusMonitorConfigDisplay(179, 30); // Bio
                            StatusMonitorConfigDisplay(189, 30); // Bio II
                            StatusMonitorConfigDisplay(1895, 30); // Biolysis
                            break;
                        }
                        case 30: {
                            // NIN
                            StatusMonitorConfigDisplay(508, 30); // Shadow Fang
                            break;
                        }
                        case 31: {
                            // MCH
                            // 1866
                            StatusMonitorConfigDisplay(1866, 15); // Bio Blaster
                            break;
                        }
                        case 33: {
                            // AST
                            StatusMonitorConfigDisplay(838, 30); // Combust
                            StatusMonitorConfigDisplay(843, 30); // Combust II
                            StatusMonitorConfigDisplay(1881, 30); // Combust III
                            StatusMonitorConfigDisplay(835, 15, "Diurnal"); // Aspected Benific (Regen)
                            StatusMonitorConfigDisplay(836, 15, "Diurnal"); // Aspected Helios (Regen)
                            break;
                        }
                        case 34: {
                            // SAM
                            StatusMonitorConfigDisplay(1228, 60); // Higanbana
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

                ImGui.EndChild();

                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Raid Buffs")) {
                
                ImGui.Columns(1 + MonitorDisplays.Count, "###statusColumns", false);
                ImGui.SetColumnWidth(0, 220);
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
                ImGui.Separator();
                ImGui.Columns(1);
                ImGui.BeginChild("###scrolling", new Vector2(-1));
                ImGui.Columns(1 + MonitorDisplays.Count, "###statusColumns", false);
                ImGui.SetColumnWidth(0, 220);
                for (var i = 1; i <= MonitorDisplays.Count; i++) {
                    ImGui.SetColumnWidth(i, 100);
                }
                
                StatusMonitorConfigDisplay(638, 15, raid: true, note: pluginInterface.Data.GetExcelSheet<Action>().GetRow(2258)?.Name); // Target / Trick Attack (NIN)

                StatusMonitorConfigDisplay(1221, 15, raid: true); // Target / Chain Stratagem (SCH)

                StatusMonitorConfigDisplay(1213, 15, raid: true, selfOnly: true); // Player / Devotion (SMN)
                
                StatusMonitorConfigDisplay(786, 20, raid: true, selfOnly: true); // Player / Battle Litany (DRG)

                StatusMonitorConfigDisplay(1184, 20, raid: true, selfOnly: true, note: pluginInterface.Data.GetExcelSheet<Action>().GetRow(7398)?.Name); // Player / Dragon Sight (DRG)

                StatusMonitorConfigDisplay(1185, 20, raid: true, selfOnly: true); // Player / Brotherhood (MNK)

                StatusMonitorConfigDisplay(1297, 20, raid: true, selfOnly: true); // Player / Embolden (RDM)

                StatusMonitorConfigDisplay(1202, 20, raid: true, selfOnly: true); // Player / Nature's Minne (BRD)

                

                // ... need to do this a better way...
                StatusMonitorConfigDisplay(1876, 20, raid: true, selfOnly: true, statusList: new uint[]{ 1882, 1884, 1885}, forcedName: "Melee Cards"); // Player / Balance (AST)
                StatusMonitorConfigDisplay(1877, 20, raid: true, selfOnly: true, statusList: new uint[]{ 1883, 1886, 1887}, forcedName: "Ranged Cards"); // Player / Bole (AST)
                
                ImGui.Columns(1);

                ImGui.TextWrapped("\nSomething Missing? Please let Caraxi know on the goat place discord and it will be added.");

                ImGui.EndChild();
                ImGui.EndTabItem();
            }

            if (MonitorDisplays.Count > 0 && ImGui.BeginTabItem("Reminders")) {
                ImGui.BeginChild("###scrolling", new Vector2(-1));
                ImGui.Columns(1 + MonitorDisplays.Count, "###remindersColumns", false);
                ImGui.SetColumnWidth(0, 220);
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
                ImGui.Separator();
                ImGui.Columns(1);
                ImGui.BeginChild("###scrolling", new Vector2(-1));
                ImGui.Columns(1 + MonitorDisplays.Count, "###remindersColumns", false);
                ImGui.SetColumnWidth(0, 220);
                for (var i = 1; i <= MonitorDisplays.Count; i++) {
                    ImGui.SetColumnWidth(i, 100);
                }

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
                    ImGui.Separator();

                }

                ImGui.Columns(1);

                ImGui.EndChild();
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
                        foreach (var se in pluginInterface.ClientState.Targets.CurrentTarget.StatusEffects) {
                            if (se.EffectId <= 0) continue;
                            var status = pluginInterface.Data.Excel.GetSheet<Status>().GetRow((uint)se.EffectId);
                            ImGui.Text($"\t{status.Name}: {status.RowId}");
                        }
                    }


                    ImGui.Text("\nEffects on Self: ");
                    foreach (var se in pluginInterface.ClientState.LocalPlayer.StatusEffects) {
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


                    // Bars

                    var sw = new Stopwatch();
                    sw.Start();

                    ImGui.SliderFloat("Debug Bars Fill Percent", ref debugFraction, 0, 1);
                    

                    var completeColor = new Vector4(1f, 0f, 0f, 0.25f);
                    var incompleteColor = new Vector4(0f, 0f, 1f, 0.25f);

                    var usedFraction = (float) Math.Min(1, Math.Max(0, debugFraction));

                    ImGui.Text($"{usedFraction:F2}");
                    sw.Start();
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromBottom, incompleteColor, completeColor);
                    ImGui.SameLine();
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromTop, incompleteColor, completeColor);

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromLeft, incompleteColor, completeColor);
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromRight, incompleteColor, completeColor);
                    usedFraction = 1 - usedFraction;

                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromBottom, incompleteColor, completeColor);
                    ImGui.SameLine();
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromTop, incompleteColor, completeColor);

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromLeft, incompleteColor, completeColor);
                    plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromRight, incompleteColor, completeColor);

                    ImGui.EndGroup();
                    ImGui.EndGroup();

                    sw.Stop();
                    ImGui.Text($"Time to draw bars: {sw.ElapsedTicks}");

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

        private void StatusMonitorConfigDisplay(uint statusId, float maxDuration, string note = null, bool raid = false, bool selfOnly = false, uint[] statusList = null, string forcedName = null) {
            var status = pluginInterface.Data.GetExcelSheet<Status>().GetRow(statusId);
            if (status == null) return;
            var statusMonitor = new StatusMonitor {Status = status.RowId, ClassJob = raid ? 0 : pluginInterface.ClientState.LocalPlayer.ClassJob.Id, MaxDuration = maxDuration, SelfOnly = selfOnly, StatusList = statusList};
            
            var statusIcon = plugin.IconManager.GetIconTexture(status.Icon);
            if (statusIcon != null) {
                ImGui.Image(statusIcon.ImGuiHandle, new Vector2(18, 24));
            } else {
                ImGui.Dummy(new Vector2(18, 24));
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip(status.Description);
            }

            if (statusList != null) {
                foreach (var s in statusList) {
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

            if (selfOnly) {
                var selfTextSize = ImGui.CalcTextSize("[SELF]");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetColumnWidth() - selfTextSize.X);
                ImGui.TextDisabled("[SELF]");
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

            ImGui.Separator();

        }

    }
}