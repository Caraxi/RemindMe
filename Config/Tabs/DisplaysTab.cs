using System;
using System.Numerics;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMeConfig {
        public void DrawDisplaysTab() {
            ImGui.BeginChild("###displaysScroll", ImGui.GetWindowSize() - (ImGui.GetStyle().WindowPadding * 2) - new Vector2(0, ImGui.GetCursorPosY()));
            if (ImGui.Button("Add New Display")) {
                var guid = Guid.NewGuid();
                MonitorDisplays.Add(guid, new MonitorDisplay { Guid = guid, Name = $"Display {MonitorDisplays.Count + 1}" });
                Save();
            }

            if (MonitorDisplays.Count == 0) {
                ImGui.Text("Add a display to get started.");
            }

            Guid? deletedMonitor = null;
            MonitorDisplay copiedDisplay = null;
            foreach (var m in MonitorDisplays.Values) {
                if (ImGui.CollapsingHeader($"{(m.Enabled ? "":"[Disabled] ")}{m.Name}###configDisplay{m.Guid}")) {
                    m.DrawConfigEditor(this, plugin, ref deletedMonitor, ref copiedDisplay);
                }
            }

            if (deletedMonitor.HasValue) {
                MonitorDisplays.Remove(deletedMonitor.Value);
                Save();
            }

            if (copiedDisplay != null && !MonitorDisplays.ContainsKey(copiedDisplay.Guid)) {
                MonitorDisplays.Add(copiedDisplay.Guid, copiedDisplay);
            }
            ImGui.EndChild();
        }
    }
}
