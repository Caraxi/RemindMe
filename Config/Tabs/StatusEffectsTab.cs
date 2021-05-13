using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMeConfig {
        public void DrawStatusEffectsTab() {
            ImGui.Columns(1 + MonitorDisplays.Values.Count(d => d.Enabled), "###statusColumns", false);
            ImGui.SetColumnWidth(0, 220);
            for (var i = 1; i <= MonitorDisplays.Values.Count(d => d.Enabled); i++) {
                ImGui.SetColumnWidth(i, 100);
            }
            ImGui.Text("Status");
            ImGui.NextColumn();
            foreach (var m in MonitorDisplays.Values.Where(d => d.Enabled)) {
                ImGui.Text(m.Name);
                ImGui.NextColumn();
            }

            ImGui.Separator();
            ImGui.Separator();
            ImGui.Columns(1);
            ImGui.BeginChild("###scrolling", new Vector2(-1));
            ImGui.Columns(1 + MonitorDisplays.Values.Count(d => d.Enabled), "###statusColumns", false);
            ImGui.SetColumnWidth(0, 220);
            for (var i = 1; i <= MonitorDisplays.Values.Count(d => d.Enabled); i++) {
                ImGui.SetColumnWidth(i, 100);
            }
            switch (pluginInterface.ClientState.LocalPlayer.ClassJob.Id) {
                case 19: {
                        // PLD
                        StatusMonitorConfigDisplay(76, 25, selfOnly: true); // Fight or Flight
                        StatusMonitorConfigDisplay(1368, 12, selfOnly: true); // Requiescat
                        StatusMonitorConfigDisplay(725, 21); // Goring Blade
                        StatusMonitorConfigDisplay(248, 15); // Circle of Scorn
                        StatusMonitorConfigDisplay(74, 15, selfOnly: true); // Sentinel
                        StatusMonitorConfigDisplay(82, 10, selfOnly: true); // Hallowed Ground
                        StatusMonitorConfigDisplay(1175, 18); // Passage of Arms
                        StatusMonitorConfigDisplay(726, 30, selfOnly: true); // Divine Veil
                        StatusMonitorConfigDisplay(727, 30, note: "shield"); // Divine Veil Shield
                        StatusMonitorConfigDisplay(1856, 5, selfOnly: true); // Shelltron
                        StatusMonitorConfigDisplay(1174, 6); // Intervention
                        StatusMonitorConfigDisplay(81, 12); // Cover
                        tankRoleEffects();
                        break;
                }
                case 20: {
                        // MNK
                        StatusMonitorConfigDisplay(246, 18, alwaysAvailable: true, minLevel: 30); // Demolish
                        break;
                    }
                case 21: {
                        // WAR
                        StatusMonitorConfigDisplay(86, 10, selfOnly: true); // Berserk
                        StatusMonitorConfigDisplay(90, 60, selfOnly: true, alwaysAvailable: true, minLevel: 50); // Storm's Eye
                        StatusMonitorConfigDisplay(87, 10, selfOnly: true); // Thrill of Battle
                        StatusMonitorConfigDisplay(89, 15, selfOnly: true); // Vengeance
                        StatusMonitorConfigDisplay(409, 8, selfOnly: true); // Holmgang
                        StatusMonitorConfigDisplay(735, 6); // Raw Intuition
                        StatusMonitorConfigDisplay(1457, 15); // Shake it Off

                        // TODO: Obtain Ability IDs
                        // StatusMonitorConfigDisplay(xxx, 10); // Inner Release (Possibly same buff as Berserk?)
                        // StatusMonitorConfigDisplay(xxx, 6); // Nascent Flash

                        tankRoleEffects();
                        break;
                    }
                case 22: {
                        // DRG
                        StatusMonitorConfigDisplay(1914, 30, alwaysAvailable: true, minLevel: 18); // Disembowment
                        StatusMonitorConfigDisplay(118, 24, alwaysAvailable: true, minLevel: 50); // Chaos Thrust
                        break;
                    }
                case 5:
                case 23: {
                        // BRD
                        StatusMonitorConfigDisplay(122, 10, selfOnly: true); // Straight Shot Ready
                        StatusMonitorConfigDisplay(124, 30, alwaysAvailable: true, minLevel: 6, maxLevel: 63); // Venomous Bite
                        StatusMonitorConfigDisplay(129, 30, alwaysAvailable: true, minLevel: 30, maxLevel: 63); // Windbite
                        if (pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 5) break;
                        StatusMonitorConfigDisplay(1200, 30, alwaysAvailable: true, minLevel: 64); // Causic Bite
                        StatusMonitorConfigDisplay(1201, 30, alwaysAvailable: true, minLevel: 64); // Stormbite
                        break;
                    }
                case 6:
                case 24: {
                        // WHM
                        StatusMonitorConfigDisplay(143, 18, alwaysAvailable: true, minLevel: 4, maxLevel: 45); // Aero
                        StatusMonitorConfigDisplay(144, 18, alwaysAvailable: true, minLevel: 46, maxLevel: 71); // Aero II
                        StatusMonitorConfigDisplay(1871, 30, alwaysAvailable: true, minLevel: 72); // Dia
                        StatusMonitorConfigDisplay(158, 18, alwaysAvailable: true, minLevel: 35); // Regen
                        StatusMonitorConfigDisplay(150, 15, alwaysAvailable: true, minLevel: 50); // Medica II
                        StatusMonitorConfigDisplay(157, 15, selfOnly: true); // Presence of Mind
                        StatusMonitorConfigDisplay(1217, 12, selfOnly: true); // Thin Air
                        StatusMonitorConfigDisplay(1218, 15); // Divine Benison
                        StatusMonitorConfigDisplay(1219, 10); // Confession
                        StatusMonitorConfigDisplay(1872, 20, selfOnly: true); // Temperance

                        break;
                    }
                case 25: {
                        // BLM
                        StatusMonitorConfigDisplay(161, 18, alwaysAvailable: true, minLevel: 6, maxLevel: 44); // Thunder
                        StatusMonitorConfigDisplay(162, 12, alwaysAvailable: true, minLevel: 26, maxLevel: 63); // Thunder II
                        StatusMonitorConfigDisplay(163, 24, alwaysAvailable: true, minLevel: 45); // Thunder III
                        StatusMonitorConfigDisplay(1210, 18, alwaysAvailable: true, minLevel: 64); // Thunder IV
                        StatusMonitorConfigDisplay(164, 18, selfOnly: true); // Thundercloud procs on player
                        StatusMonitorConfigDisplay(165, 18, selfOnly: true); // Firestarter procs on player
                        break;
                    }
                case 26:
                case 27: {
                        // ACN, SMN
                        // TODO: Min/Max level for SMN
                        StatusMonitorConfigDisplay(179, 30, alwaysAvailable: true); // Bio
                        StatusMonitorConfigDisplay(180, 30, alwaysAvailable: true); // Miasma
                        StatusMonitorConfigDisplay(189, 30, alwaysAvailable: true); // Bio II
                        StatusMonitorConfigDisplay(1214, 30, alwaysAvailable: true); // Bio III
                        StatusMonitorConfigDisplay(1215, 30, alwaysAvailable: true); // Miasma III
                        StatusMonitorConfigDisplay(1212, -1, selfOnly: true, stacking: true);
                        break;
                    }
                case 28: {
                        // SCH
                        StatusMonitorConfigDisplay(179, 30, alwaysAvailable: true, minLevel: 2, maxLevel: 25); // Bio
                        StatusMonitorConfigDisplay(189, 30, alwaysAvailable: true, minLevel: 26, maxLevel: 71); // Bio II
                        StatusMonitorConfigDisplay(1895, 30, alwaysAvailable: true, minLevel: 72); // Biolysis
                        break;
                    }
                case 30: {
                        // NIN
                        StatusMonitorConfigDisplay(508, 30); // Shadow Fang
                        StatusMonitorConfigDisplay(1955, 15, selfOnly: true); // Assassinate Ready
                        break;
                    }
                case 31: {
                        // MCH
                        // 1866
                        StatusMonitorConfigDisplay(851, 5, selfOnly: true); // Reassembled
                        StatusMonitorConfigDisplay(1951, 15); // Tactician
                        StatusMonitorConfigDisplay(1205, 10, selfOnly: true); // Flamethrower
                        StatusMonitorConfigDisplay(1866, 15); // Bio Blaster
                        break;
                    }
                case 32:
                    {
                        // DRK
                        StatusMonitorConfigDisplay(742, 10, selfOnly: true); // Blood Weapon
                        StatusMonitorConfigDisplay(747, 15, selfOnly: true); // Shadow Wall
                        StatusMonitorConfigDisplay(746, 10, selfOnly: true); // Dark Mind
                        StatusMonitorConfigDisplay(810, 10, selfOnly: true); // Living Dead
                        StatusMonitorConfigDisplay(811, 10, selfOnly: true); // Walking Dead
                        StatusMonitorConfigDisplay(749, 15, selfOnly: true); // Salted Earth
                        StatusMonitorConfigDisplay(1972, 10, selfOnly: true); // Delirium
                        StatusMonitorConfigDisplay(1178, 7); // Blackest Night

                        // TODO: Obtain Ability IDs
                        // StatusMonitorConfigDisplay(xxx, 15); // Dark Missionary
                        // StatusMonitorConfigDisplay(xxx, 24); // Living Shadow

                        tankRoleEffects();
                        break;
                    }
                case 33: {
                        // AST
                        StatusMonitorConfigDisplay(838, 30, alwaysAvailable: true, minLevel: 4, maxLevel: 45); // Combust
                        StatusMonitorConfigDisplay(843, 30, alwaysAvailable: true, minLevel: 46, maxLevel: 71); // Combust II
                        StatusMonitorConfigDisplay(1881, 30, alwaysAvailable: true, minLevel: 72); // Combust III
                        StatusMonitorConfigDisplay(835, 15, "Diurnal"); // Aspected Benific (Regen)
                        StatusMonitorConfigDisplay(836, 15, "Diurnal"); // Aspected Helios (Regen)
                        break;
                    }
                case 34: {
                        // SAM
                        StatusMonitorConfigDisplay(1228, 60, alwaysAvailable: true, minLevel: 30); // Higanbana
                        StatusMonitorConfigDisplay(1298, 40, selfOnly: true, alwaysAvailable: true, minLevel: 4); // Jinpu
                        StatusMonitorConfigDisplay(1299, 40, selfOnly: true, alwaysAvailable: true, minLevel: 18); // Shifu
                        break;
                    }
                case 35:
                    {
                        // RDM
                        StatusMonitorConfigDisplay(1234, 30, selfOnly: true); // Verfire Ready
                        StatusMonitorConfigDisplay(1235, 30, selfOnly: true); // Verstone Ready
                        StatusMonitorConfigDisplay(1238, 20, selfOnly: true); // Acceleration
                        StatusMonitorConfigDisplay(1239, 20); // Embolden
                        StatusMonitorConfigDisplay(1971, 10, selfOnly: true); // Manification
                        StatusMonitorConfigDisplay(1249, 15, selfOnly: true); // Dualcast
                        break;
                    }
                case 36: {
                        // BLU
                        StatusMonitorConfigDisplay(1714, 30, "Song of Torment"); // Song of Torment
                        StatusMonitorConfigDisplay(1715, 15, "Bad Breath");
                        StatusMonitorConfigDisplay(18, 15, "Bad Breath");
                        StatusMonitorConfigDisplay(1717, 15, "Aetherial Spark");
                        StatusMonitorConfigDisplay(1737, 180, selfOnly: true); // Toad Oil
                        break;
                    }
                case 37: {
                        // GNB
                        StatusMonitorConfigDisplay(1831, 20, selfOnly: true); // No Mercy
                        StatusMonitorConfigDisplay(1837, 30); // Sonic Break
                        StatusMonitorConfigDisplay(1838, 15); // Bow Shock
                        StatusMonitorConfigDisplay(1835, 18); // Aurora
                        StatusMonitorConfigDisplay(1839, 15); // Heart of Light
                        StatusMonitorConfigDisplay(1840, 7); // Heart of Stone
                        StatusMonitorConfigDisplay(1898, 30, selfOnly: true); // Brutal Shell
                        StatusMonitorConfigDisplay(1832, 20, selfOnly: true); // Camouflage
                        StatusMonitorConfigDisplay(1834, 15, selfOnly: true); // Nebula
                        StatusMonitorConfigDisplay(1836, 8, selfOnly: true); // Superbolide
                        tankRoleEffects();

                        break;
                    }
                default: {
                        ImGui.Columns(1);
                        ImGui.TextWrapped($"No status monitors are available on {pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Name}.");
                        break;
                    }
            }

            var oldMonitors = new List<StatusMonitor>();
            foreach (var m in MonitorDisplays.Values.Where(t => t.Enabled)) {
                foreach (var sm in m.StatusMonitors.Where(s => s.IsRaid == false && s.ClassJob == pluginInterface.ClientState.LocalPlayer.ClassJob.Id && !visibleStatusMonitor.Contains(s))) {
                    oldMonitors.Add(sm);
                }
            }

            if (oldMonitors.Count > 0) {
                ImGui.Separator();
                
                ImGui.Text("Obsolete Monitors (Disable Only)");
                while(ImGui.GetColumnIndex() != 0) ImGui.NextColumn();
                ImGui.Separator();
                foreach (var sm in oldMonitors) {
                    StatusMonitorConfigDisplay(sm, note: "Obsolete", removeOnly: true);
                }
            }

            ImGui.Columns(1);
            ImGui.TextWrapped("\nSomething Missing?\nPlease let Caraxi know on the goat place discord and it will be added.");
            ImGui.EndChild();
            
        }

        private void tankRoleEffects()
        {
            StatusMonitorConfigDisplay(1209, 6, selfOnly: true); // Arm's Length
            StatusMonitorConfigDisplay(9, 15, note: "from Arm's Length"); // Arm's Length Slow
            StatusMonitorConfigDisplay(2, 5, note: "from Low Blow"); // Low Blow Stun
            StatusMonitorConfigDisplay(1191, 20, selfOnly: true); // Rampart
            StatusMonitorConfigDisplay(1193, 10); // Reprisal
        }
    }

}
