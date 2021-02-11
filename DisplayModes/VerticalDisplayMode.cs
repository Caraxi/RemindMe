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
                var fraction = (float)(timer.TimerCurrent + display.CacheAge.TotalSeconds) / timer.TimerMax;

                if (display.LimitDisplayTime && timer.TimerMax > display.LimitDisplayTimeSeconds) {
                    fraction = (float)(display.LimitDisplayTimeSeconds - timer.TimerRemaining + display.CacheAge.TotalSeconds) / display.LimitDisplayTimeSeconds;
                }

                if (display.FillToComplete && fraction < 1) {
                    fraction = 1 - fraction;
                }

                ImGui.BeginGroup();

                var drawList = ImGui.GetWindowDrawList();

                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + barSize;

                var hovered = false;
                if (display.AllowClicking && timer.ClickAction != null) {
                    // Check Mouse Position
                    var mouse = ImGui.GetMousePos();
                    var pos1 = ImGui.GetCursorScreenPos();
                    var pos2 = ImGui.GetCursorScreenPos() + barSize;

                    if (mouse.X > pos1.X && mouse.X < pos2.X && mouse.Y > pos1.Y && mouse.Y < pos2.Y) {
                        display.IsClickableHovered = true;
                        hovered = true;
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    }
                }
   
                DrawBar(barTopLeft, barSize, 1 - fraction, display.ReverseFill ? FillDirection.FromTop : FillDirection.FromBottom, GetBarBackgroundColor(display, timer), timer.ProgressColor);
                var iconSize = new Vector2(display.RowSize) * display.ActionIconScale;
                if (display.ShowActionIcon) {
                    
                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {
                            iconSize *= new Vector2((float)icon.Width / Math.Max(icon.Width, icon.Height), (float)icon.Height / Math.Max(icon.Width, icon.Height));

                            if (display.ReverseSideIcon) {
                                ImGui.SetCursorPosY(cPosY + (barSize.X - iconSize.X) / 2);
                            } else {
                                ImGui.SetCursorPosY(cPosY + barSize.Y - iconSize.Y - (barSize.X - iconSize.X) / 2);
                            }
                            ImGui.SetCursorPosX(cPosX + (display.RowSize / 2f) - (iconSize.X / 2));
                           
                            ImGui.Image(icon.ImGuiHandle, iconSize);
                        }
                    }
                }

                if (timer.AllowCountdown && display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                    var countdownValue = Math.Abs(timer.TimerRemaining - display.CacheAge.TotalSeconds);
                    var countdownText = countdownValue.ToString(countdownValue >= 100 ? "F0" : "F1");
                    var countdownSize = ImGui.CalcTextSize(countdownText);
                    if (display.ReverseCountdownSide) {
                        ImGui.SetCursorPosY(cPosY + barSize.Y - (display.RowSize / 2f) - countdownSize.Y / 2);
                    } else {
                        ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f) - countdownSize.Y / 2);
                    }
                    
                    ImGui.SetCursorPosX(cPosX + (display.RowSize / 2f) - (countdownSize.X / 2));
                    if (display.ShowActionIcon && display.ReverseCountdownSide != display.ReverseSideIcon) {
                        TextShadowed(countdownText, display.TextColor, new Vector4(0, 0, 0, 0.5f), 2);
                    } else {
                        ImGui.TextColored(display.TextColor, countdownText);
                    }
                }

                if (display.ShowSkillName) {
                    var size = ImGui.CalcTextSize(timer.Name);
                    ImGui.SetCursorPosX(cPosX + display.RowSize / 2f - size.Y / 2);
                    ImGui.SetCursorPosY(ImGui.GetWindowHeight() - (display.RowSize + (size.X * display.TextScale) + ImGui.GetStyle().ItemSpacing.X ));
                    AddTextVertical(timer.Name, ImGui.GetColorU32(display.TextColor), display.TextScale);
                }

                if (hovered) {
                    drawList.AddRect(barTopLeft, barBottomRight, 0xFF0000FF);
                    drawList.AddRect(barTopLeft + Vector2.One, barBottomRight - Vector2.One, 0xFF0000FF);
                }


                ImGui.EndGroup();
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
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
