using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMe {
        private void DrawDisplayVertical(MonitorDisplay display, List<DisplayTimer> timerList) {

            if (display.DirectionRtL) {
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (display.RowSize + ImGui.GetStyle().WindowPadding.X));
            }
            ImGui.SetWindowFontScale(display.TextScale);

            var barSize = new Vector2(display.RowSize, ImGui.GetWindowHeight() - ImGui.GetStyle().WindowPadding.Y * 2);

            foreach (var timer in timerList) {
                var cPosX = ImGui.GetCursorPosX();
                var cPosY = ImGui.GetCursorPosY();
                var fraction = timer.TimerFractionComplete;

                ImGui.BeginGroup();

                var drawList = ImGui.GetWindowDrawList();

                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + barSize;
                
                var barFractionCompleteSize = new Vector2(0, barSize.Y * (1 - fraction));
                var barFractionIncompleteSize = new Vector2(0, barSize.Y * fraction);

                var hovered = false;
                if (display.AllowClicking && timer.ClickAction != null) {
                    // Check Mouse Position
                    var mouse = ImGui.GetMousePos();
                    var pos1 = ImGui.GetCursorScreenPos();
                    var pos2 = ImGui.GetCursorScreenPos() + barSize;

                    if (mouse.X > pos1.X && mouse.X < pos2.X && mouse.Y > pos1.Y && mouse.Y < pos2.Y) {
                        display.isClickableHovered = true;
                        hovered = true;
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    }
                }

                // Draw Bar
                if (timer.IsComplete) {
                    var finishedColor = Vector4.Zero + timer.FinishedColor;
                    var s = Math.Abs((Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed)) - (float)Math.Floor(Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed))) - 0.5f) / 2) * display.PulseIntensity;
                    if (timer.FinishedColor.W < 0.75) {
                        finishedColor += new Vector4(0, 0, 0, s);
                    } else {
                        finishedColor -= new Vector4(0, 0, 0, s);
                    }
                    drawList.AddRectFilled(barTopLeft, barBottomRight, ImGui.GetColorU32(finishedColor));
                } else {
                    drawList.AddRectFilled(barTopLeft, barBottomRight - barFractionCompleteSize, ImGui.GetColorU32(display.BarBackgroundColor));
                    drawList.AddRectFilled(barTopLeft + barFractionIncompleteSize, barBottomRight, ImGui.GetColorU32(timer.ProgressColor));
                }



                if (display.ShowActionIcon) {
                    var iconSize = new Vector2(display.RowSize) * display.ActionIconScale;
                    if (display.ReverseSideIcon) {
                        ImGui.SetCursorPosY(cPosY + (barSize.X - iconSize.X) / 2);
                    } else {
                        ImGui.SetCursorPosY(cPosY + barSize.Y - iconSize.Y - (barSize.X - iconSize.X) / 2);
                    }
                    ImGui.SetCursorPosX(cPosX + (display.RowSize / 2f) - (iconSize.X / 2));
                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {
                            ImGui.Image(icon.ImGuiHandle, iconSize);
                        }
                    }
                }

                if (timer.AllowCountdown && display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                    var countdownText = Math.Abs(timer.TimerRemaining).ToString("F1");
                    var countdownSize = ImGui.CalcTextSize(countdownText);
                    ImGui.SetCursorPosY(cPosY + ImGui.GetStyle().FramePadding.Y);
                    ImGui.SetCursorPosX(cPosX + (display.RowSize / 2f) - (countdownSize.X / 2));

                    ImGui.TextColored(display.TextColor, countdownText);
                }

                if (hovered) {
                    drawList.AddRect(barTopLeft, barBottomRight, 0xFF0000FF);
                    drawList.AddRect(barTopLeft + Vector2.One, barBottomRight - Vector2.One, 0xFF0000FF);
                }


                ImGui.EndGroup();
                if (ImGui.IsItemClicked(0)) {
                    timer.ClickAction?.Invoke(this, timer.ClickParam);
                }

                ImGui.SameLine();
                if (display.DirectionRtL) {
                    ImGui.SetCursorPosX(cPosX - display.RowSize - display.BarSpacing);
                } else {
                    ImGui.SetCursorPosX(cPosX + display.RowSize + display.BarSpacing);
                }

                if (ImGui.GetCursorPosX() + display.RowSize > ImGui.GetWindowWidth()) {
                    return;
                }
            }

        }
    }
}
