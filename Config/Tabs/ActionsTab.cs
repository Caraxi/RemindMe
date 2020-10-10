using System;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMeConfig {

        public void DrawActionsTab() {
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
        }




    }
}
