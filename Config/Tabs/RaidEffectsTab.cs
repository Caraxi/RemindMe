using System.Numerics;
using ImGuiNET;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {
    public partial class RemindMeConfig {
        public void DrawRaidEffectsTab() {
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

            StatusMonitorConfigDisplay(1876, 20, raid: true, selfOnly: true, statusList: new uint[] { 1882, 1884, 1885 }, forcedName: "Melee Cards"); // Player / Balance (AST)
            StatusMonitorConfigDisplay(1877, 20, raid: true, selfOnly: true, statusList: new uint[] { 1883, 1886, 1887 }, forcedName: "Ranged Cards"); // Player / Bole (AST)

            ImGui.Columns(1);

            ImGui.TextWrapped("\nSomething Missing?\nPlease let Caraxi know on the goat place discord and it will be added.");

            ImGui.EndChild();
        }
    }
}
