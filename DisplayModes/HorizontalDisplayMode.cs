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
            var barSize = new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2, display.RowSize);
            foreach (var timer in timerList) {
                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + barSize;

                ImGui.BeginGroup();

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
                var cPosY = ImGui.GetCursorPosY();

                var fraction = (float) (timer.TimerCurrent + display.CacheAge.TotalSeconds) / timer.TimerMax;

                if (display.LimitDisplayTime && timer.TimerMax > display.LimitDisplayTimeSeconds) {
                    fraction = (float) (display.LimitDisplayTimeSeconds - timer.TimerRemaining + display.CacheAge.TotalSeconds) / display.LimitDisplayTimeSeconds;
                }

                if (display.FillToComplete && fraction < 1) {
                    fraction = 1 - fraction;
                }

                DrawBar(ImGui.GetCursorScreenPos(), barSize, 1 - fraction, display.ReverseFill ? FillDirection.FromRight : FillDirection.FromLeft, GetBarBackgroundColor(display, timer), timer.ProgressColor);

                var iconSize = new Vector2(display.RowSize) * display.ActionIconScale;

                if (display.ShowActionIcon) {
                    var x = ImGui.GetCursorPosX();
                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {
                            var iconRatio = new Vector2((float)icon.Width / Math.Max(icon.Width, icon.Height), (float)icon.Height / Math.Max(icon.Width, icon.Height));

                            var displayedIconSize = iconSize * iconRatio;

                            ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f) - (displayedIconSize.Y / 2));
                            if (display.ReverseSideIcon) {
                                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (display.RowSize / 2f) - (displayedIconSize.X / 2) - ImGui.GetStyle().WindowPadding.X);
                            } else {
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (display.RowSize / 2f) - (displayedIconSize.X / 2));
                            }

                            
                            ImGui.Image(icon.ImGuiHandle, displayedIconSize);
                        }
                    }
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(x + (display.RowSize / 2f) + (iconSize.X / 2) + ImGui.GetStyle().ItemSpacing.X);
                }

                if (timer.AllowCountdown && display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                    var time = Math.Abs(timer.TimerRemaining - display.CacheAge.TotalSeconds);
                    var countdownText = time.ToString(time >= 100 ? "F0" : "F1");
                    var countdownSize = ImGui.CalcTextSize(countdownText);

                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - countdownSize.Y / 2f));

                    if (display.ReverseCountdownSide) {
                        ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X + display.RowSize / 2f - countdownSize.X / 2);
                    } else {
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - display.RowSize / 2f - countdownSize.X / 2);
                    }

                    if (display.ShowActionIcon && display.ReverseSideIcon != display.ReverseCountdownSide) {
                        TextShadowed(countdownText, display.TextColor, new Vector4(0, 0, 0, 0.5f), 2);
                    } else {
                        ImGui.TextColored(display.TextColor, countdownText);
                    }
                }

                if (display.ShowSkillName) {
                    var name = timer.Name;
                    if (display.ShowStatusEffectTarget && !string.IsNullOrEmpty(timer.TargetName)) {
                        if (timer.TargetNameOnly) {
                            name = timer.TargetName;
                        } else {
                            name += $" on {timer.TargetName}";
                        }
                        
                    }
                    var size = ImGui.CalcTextSize(name);
                    if (display.SkillNameRight) {
                        ImGui.SetCursorPosX(
                            ImGui.GetWindowWidth() - 
                            size.X - 
                            ImGui.GetStyle().WindowPadding.X - 
                            ImGui.GetStyle().FramePadding.X -
                            ((display.ShowActionIcon && display.ReverseSideIcon) || (display.ShowCountdown && !display.ReverseCountdownSide) ? (iconSize.X + ImGui.GetStyle().ItemSpacing.X) : 0)
                            );
                    } else {
                        ImGui.SetCursorPosX(
                            ImGui.GetStyle().WindowPadding.X +
                            ImGui.GetStyle().FramePadding.X +
                            ((display.ShowActionIcon && !display.ReverseSideIcon) || (display.ShowCountdown && display.ReverseCountdownSide) ? (iconSize.X + ImGui.GetStyle().ItemSpacing.X) : 0)
                            );
                    }

                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - size.Y / 2f));
                    ImGui.TextColored(display.TextColor, name);
                }

                if (hovered) {
                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddRect(barTopLeft, barBottomRight, 0xFF0000FF);
                    drawList.AddRect(barTopLeft + Vector2.One, barBottomRight - Vector2.One, 0xFF0000FF);
                }

                ImGui.EndGroup();
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
                    timer.ClickAction?.Invoke(this, timer.ClickParam);
                }

                if (display.DirectionBtT) {
                    ImGui.SetCursorPosY(cPosY - display.RowSize - display.BarSpacing);
                } else {
                    ImGui.SetCursorPosY(cPosY + display.RowSize + display.BarSpacing);
                }

                if (ImGui.GetCursorPosY() + display.RowSize > ImGui.GetWindowHeight()) {
                    return;
                }
            }
        }
    }
}
