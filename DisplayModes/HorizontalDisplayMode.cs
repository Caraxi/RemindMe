using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMe {
        private void DrawDisplayHorizontal(MonitorDisplay display, List<DisplayTimer> timerList) {

            if (display.DirectionBtT) {
                ImGui.SetCursorPosY(ImGui.GetWindowHeight() - (display.RowSize + ImGui.GetStyle().WindowPadding.Y));
            }
            ImGui.SetWindowFontScale(display.TextScale);

            foreach (var timer in timerList) {
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, timer.ProgressColor);
                if (timer.IsComplete) {

                    if (display.PulseReady) {
                        var s = Math.Abs((Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed)) - (float)Math.Floor(Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed))) - 0.5f) / 2) * display.PulseIntensity;

                        if (timer.FinishedColor.W < 0.75) {
                            ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor + new Vector4(0, 0, 0, s));
                        } else {
                            ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor - new Vector4(0, 0, 0, s));
                        }

                    } else {
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor);
                    }

                } else {
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, display.BarBackgroundColor);
                }

                var size = ImGui.CalcTextSize(timer.Name);
                var cPosY = ImGui.GetCursorPosY();

                var fraction = timer.TimerFractionComplete;

                if (display.LimitDisplayTime && timer.TimerMax > display.LimitDisplayTimeSeconds) {
                    fraction = (display.LimitDisplayTimeSeconds - timer.TimerRemaining) / display.LimitDisplayTimeSeconds;
                }

                ImGui.ProgressBar(1 - fraction, new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2, display.RowSize), "");

                ImGui.BeginGroup();

                if (display.ShowActionIcon) {
                    var iconSize = new Vector2(display.RowSize) * display.ActionIconScale;
                    var x = ImGui.GetCursorPosX();
                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f) - (iconSize.Y / 2));
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (display.RowSize / 2f) - (iconSize.X / 2));

                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {

                            ImGui.Image(icon.ImGuiHandle, iconSize);
                        }
                    }
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(x + (display.RowSize / 2f) + (iconSize.X / 2) + ImGui.GetStyle().ItemSpacing.X);
                }

                if (display.ShowSkillName) {
                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - size.Y / 2f));
                    ImGui.TextColored(display.TextColor, $"{timer.Name}");
                }

                if (timer.AllowCountdown && display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                    var countdownText = Math.Abs(timer.TimerRemaining).ToString("F1");
                    var countdownSize = ImGui.CalcTextSize(countdownText);

                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - countdownSize.Y / 2f));
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (countdownSize.X + ImGui.GetStyle().WindowPadding.X + ImGui.GetStyle().FramePadding.X));

                    ImGui.TextColored(display.TextColor, countdownText);
                }

                ImGui.EndGroup();

                if (display.DirectionBtT) {
                    ImGui.SetCursorPosY(cPosY - display.RowSize - display.BarSpacing);
                } else {
                    ImGui.SetCursorPosY(cPosY + display.RowSize + display.BarSpacing);
                }

                ImGui.PopStyleColor(2);
            }
        }
    }
}
