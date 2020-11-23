using System.Numerics;
using ImGuiNET;

namespace RemindMe {
    public partial class RemindMeConfig {
        public void DrawStatusEffectsTab() {
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
                case 6:
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
                case 26:
                case 27: {
                        // ACN, SMN
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
            ImGui.EndChild();
            ImGui.TextWrapped("\nSomething Missing? Please let Caraxi know on the goat place discord and it will be added.");
        }
    }
}
