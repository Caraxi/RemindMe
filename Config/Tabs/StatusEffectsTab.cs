using System.Linq;
using System.Numerics;
using ImGuiNET;

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
                        StatusMonitorConfigDisplay(76, 25); // Fight or Flight
                        StatusMonitorConfigDisplay(1368, 12); // Requiescat
                        StatusMonitorConfigDisplay(725, 21); // Goring Blade
                        StatusMonitorConfigDisplay(248, 15); // Circle of Scorn
                        StatusMonitorConfigDisplay(74, 15); // Sentinel
                        StatusMonitorConfigDisplay(82, 10); // Hallowed Ground
                        StatusMonitorConfigDisplay(1175, 18); // Passage of Arms
                        StatusMonitorConfigDisplay(726, 30); // Divine Veil
                        StatusMonitorConfigDisplay(727, 30, note: "shield"); // Divine Veil Shield
                        StatusMonitorConfigDisplay(1856, 5); // Shelltron
                        StatusMonitorConfigDisplay(1174, 6); // Intervention
                        StatusMonitorConfigDisplay(81, 12); // Cover
                        tankRoleEffects();
                        break;
                }
                case 20: {
                        // MNK
                        StatusMonitorConfigDisplay(246, 18); // Demolish
                        break;
                    }
                case 21: {
                        // WAR
                        StatusMonitorConfigDisplay(86, 10); // Berserk
                        StatusMonitorConfigDisplay(90, 60, selfOnly: true); // Storm's Path
                        StatusMonitorConfigDisplay(87, 10); // Thrill of Battle
                        StatusMonitorConfigDisplay(89, 15); // Vengeance
                        StatusMonitorConfigDisplay(409, 8); // Holmgang
                        StatusMonitorConfigDisplay(735, 6); // Raw Intuition
                        StatusMonitorConfigDisplay(1457, 15); // Shake it Off

                        // TODO: Obtain Ability IDs (my WAR is too low level to use these)
                        // StatusMonitorConfigDisplay(xxx, 10); // Inner Release (Possibly same buff as Berserk?)
                        // StatusMonitorConfigDisplay(xxx, 6); // Nascent Flash


                        tankRoleEffects();
                        break;
                    }
                case 22: {
                        // DRG
                        StatusMonitorConfigDisplay(1914, 30); // Disembowment
                        StatusMonitorConfigDisplay(118, 24); // Chaos Thrust
                        break;
                    }
                case 5:
                case 23: {
                        // BRD
                        StatusMonitorConfigDisplay(124, 30); // Venomous Bite
                        StatusMonitorConfigDisplay(129, 30); // Windbite
                        if (pluginInterface.ClientState.LocalPlayer.ClassJob.Id == 5) break;
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
                        StatusMonitorConfigDisplay(157, 15); // Presence of Mind
                        StatusMonitorConfigDisplay(1217, 12); // Thin Air
                        StatusMonitorConfigDisplay(1218, 15); // Divine Benison
                        StatusMonitorConfigDisplay(1219, 10); // Confession
                        StatusMonitorConfigDisplay(1872, 20); // Temperance

                        break;
                    }
                case 25: {
                        // BLM
                        StatusMonitorConfigDisplay(161, 24); // Thunder
                        StatusMonitorConfigDisplay(162, 24); // Thunder II
                        StatusMonitorConfigDisplay(163, 24); // Thunder III
                        StatusMonitorConfigDisplay(1210, 18); // Thunder IV
                        StatusMonitorConfigDisplay(164, 18, selfOnly: true); // Thundercloud procs on player
                        StatusMonitorConfigDisplay(165, 18, selfOnly: true); // Firestarter procs on player
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
                        StatusMonitorConfigDisplay(1212, -1, selfOnly: true, stacking: true);
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
                        StatusMonitorConfigDisplay(1955, 15, selfOnly: true); // Assassinate Ready
                        break;
                    }
                case 31: {
                        // MCH
                        // 1866
                        StatusMonitorConfigDisplay(851, 5); // Reassembled
                        StatusMonitorConfigDisplay(1951, 15); // Tactician
                        StatusMonitorConfigDisplay(1205, 10); // Flamethrower
                        StatusMonitorConfigDisplay(1866, 15); // Bio Blaster
                        break;
                    }
                case 32:
                    {
                        // DRK
                        StatusMonitorConfigDisplay(742, 10); // Blood Weapon
                        StatusMonitorConfigDisplay(747, 15); // Shadow Wall
                        StatusMonitorConfigDisplay(746, 10); // Dark Mind
                        StatusMonitorConfigDisplay(810, 10); // Living Dead
                        StatusMonitorConfigDisplay(811, 10); // Walking Dead
                        StatusMonitorConfigDisplay(749, 15); // Salted Earth
                        StatusMonitorConfigDisplay(1972, 10); // Delirium
                        StatusMonitorConfigDisplay(1178, 7); // Blackest Night

                        // TODO: Obtain Ability IDs (my DRK is too low level to use these)
                        // StatusMonitorConfigDisplay(xxx, 15); // Dark Missionary
                        // StatusMonitorConfigDisplay(xxx, 24); // Living Shadow

                        tankRoleEffects();
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
                case 35:
                    {
                        // RDM
                        StatusMonitorConfigDisplay(1234, 30); // Verfire Ready
                        StatusMonitorConfigDisplay(1235, 30); // Verstone Ready
                        StatusMonitorConfigDisplay(1238, 20); // Acceleration
                        StatusMonitorConfigDisplay(1239, 20); // Embolden
                        StatusMonitorConfigDisplay(1971, 10); // Manification
                        StatusMonitorConfigDisplay(1249, 15); // Dualcast
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
                        StatusMonitorConfigDisplay(1831, 20); // No Mercy
                        StatusMonitorConfigDisplay(1837, 30); // Sonic Break
                        StatusMonitorConfigDisplay(1838, 15); // Bow Shock
                        StatusMonitorConfigDisplay(1835, 18); // Aurora
                        StatusMonitorConfigDisplay(1839, 15); // Heart of Light
                        StatusMonitorConfigDisplay(1840, 7); // Heart of Stone
                        StatusMonitorConfigDisplay(1898, 30); // Brutal Shell
                        StatusMonitorConfigDisplay(1832, 20); // Camouflage
                        StatusMonitorConfigDisplay(1834, 15); // Nebula
                        StatusMonitorConfigDisplay(1836, 8); // Superbolide
                        tankRoleEffects();

                        break;
                    }
                default: {
                        ImGui.Columns(1);
                        ImGui.TextWrapped($"No status monitors are available on {pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Name}.");
                        break;
                    }
            }

            ImGui.Columns(1);
            ImGui.TextWrapped("\nSomething Missing?\nPlease let Caraxi know on the goat place discord and it will be added.");
            ImGui.EndChild();
            
        }

        private void tankRoleEffects()
        {
            StatusMonitorConfigDisplay(1209, 6); // Arm's Length
            StatusMonitorConfigDisplay(9, 15, note: "from Arm's Length"); // Arm's Length Slow
            StatusMonitorConfigDisplay(2, 5, note: "from Low Blow"); // Low Blow Stun
            StatusMonitorConfigDisplay(1191, 20); // Rampart
            StatusMonitorConfigDisplay(1193, 10); // Reprisal
        }
    }

}
