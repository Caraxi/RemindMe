using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace RemindMe {
    public partial class RemindMeConfig{
        public void DrawDebugTab() {
            try {
                ImGui.Text($"Current ClassJobID: {pluginInterface.ClientState.LocalPlayer.ClassJob.Id}");
                ImGui.Text($"Current Level: {pluginInterface.ClientState.LocalPlayer.Level}");
                ImGui.Text($"In PvP: {plugin.InPvP}");
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

                var lastAction = pluginInterface.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(plugin.ActionManager.LastActionId);
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

                var usedFraction = (float)Math.Min(1, Math.Max(0, debugFraction));

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

        }
    }
}
