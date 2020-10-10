using System.Numerics;
using ImGuiNET;

namespace RemindMe {
    public partial class RemindMeConfig {
        public void DrawRemindersTab() {
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
        }
    }
}
