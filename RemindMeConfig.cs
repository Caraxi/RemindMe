using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using EasyHook;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using RemindMe.Config;
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


        public RemindMeConfig() { }

        public void Init(RemindMe plugin, DalamudPluginInterface pluginInterface)
        {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
        public bool DrawConfigUI()
        {
            bool drawConfig = true;

            ImGui.Begin("Cooldown###cooldownMonitorSetup", ref drawConfig);
            
            ImGui.BeginTabBar("###remindMeConfigTabs");

            if (ImGui.BeginTabItem("Displays")) {

                if (ImGui.Button("Add New Display")) {
                    var guid = Guid.NewGuid();
                    MonitorDisplays.Add(guid, new MonitorDisplay { Guid = guid });
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
                
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Ability Monitors")) {

                if (MonitorDisplays.Count > 0) {
                    ImGui.Columns(1 + MonitorDisplays.Values.Count, "###", false);

                    ImGui.SetColumnWidth(0, 180);

                    ImGui.Text("Action");
                    ImGui.NextColumn();

                    foreach (var d in MonitorDisplays.Values) {
                        ImGui.Text(d.Name);
                        ImGui.NextColumn();
                    }

                    ImGui.Separator();

                    foreach (var a in plugin.ActionList.Where(a => a.CooldownGroup != 58 && a.IsPvP == false && a.ClassJobCategory.Value.HasClass(pluginInterface.ClientState.LocalPlayer.ClassJob.Id))) {
                        var cdm = new CooldownMonitor { ActionId = a.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id };
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


                    }

                    ImGui.Columns(1);
                } else {
                    ImGui.Text("No monitor setup.");
                }

                


                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Status Monitors")) {
                if (MonitorDisplays.Count > 0) {
                    ImGui.Text($"Current ClassJobID: {pluginInterface.ClientState.LocalPlayer.ClassJob.Id}");

                    ImGui.Columns(1 + MonitorDisplays.Count);

                    ImGui.Text("Status");
                    ImGui.NextColumn();
                    foreach (var m in MonitorDisplays.Values) {
                        ImGui.Text(m.Name);
                        ImGui.NextColumn();
                    }

                    ImGui.Separator();

                    switch (pluginInterface.ClientState.LocalPlayer.ClassJob.Id) {
                        case 28: {
                            // SCH
                            StatusMonitorConfigDisplay(28, 16540, 1895); // Biolysis
                            break;
                        }
                        default: {
                            ImGui.Text($"Not supported on {pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Name}");
                            break;
                        }
                    }
                } else {
                    ImGui.Text("No Monitor setup");
                }
                





                ImGui.EndTabItem();
            }



            ImGui.EndTabBar();


            

            ImGui.End();

            return drawConfig;
        }

        private void StatusMonitorConfigDisplay(uint classJob, uint actionId, uint statusId) {
            var status = pluginInterface.Data.GetExcelSheet<Status>().GetRow(statusId);
            var action = pluginInterface.Data.GetExcelSheet<Action>().GetRow(actionId);
            if (status != null && action != null) {
                var statusMonitor = new StatusMonitor {Status = status.RowId, ClassJob = pluginInterface.ClientState.LocalPlayer.ClassJob.Id, Action = action.RowId};

                if (action.Name != status.Name) {
                    ImGui.Text($"{status.Name} ({action.Name})");
                } else {
                    ImGui.Text(status.Name);
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
                }


            }

        }

    }
}