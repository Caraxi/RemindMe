using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMe {
        private void DrawDisplayIcons(MonitorDisplay display, List<DisplayTimer> timerList) {


            if (display.DirectionBtT) {
                ImGui.SetCursorPosY(ImGui.GetWindowHeight() - (display.RowSize + ImGui.GetStyle().WindowPadding.Y));
            }

            if (display.DirectionRtL) {
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (display.RowSize + ImGui.GetStyle().WindowPadding.X));
            }

            var sPosX = ImGui.GetCursorPosX();
            var sPosY = ImGui.GetCursorPosY();
            ImGui.SetWindowFontScale(display.TextScale);
            foreach (var timer in timerList) {
                var cPosX = ImGui.GetCursorPosX();
                var cPosY = ImGui.GetCursorPosY();
                var fraction = timer.TimerFractionComplete;

                if (display.FillToComplete && fraction < 1) {
                    fraction = 1 - fraction;
                }

                ImGui.BeginGroup();

                var drawList = ImGui.GetWindowDrawList();

                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + new Vector2(display.RowSize);

                var barSize = barBottomRight - barTopLeft;
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

                var barFractionCompleteSize = new Vector2(0, barSize.Y * (1 - fraction));
                var barFractionIncompleteSize = new Vector2(0, barSize.Y * fraction);

                if (timer.IsComplete) {
                    var finishedColor = Vector4.Zero + timer.FinishedColor;
                    if (display.PulseReady) {
                        var s = Math.Abs((Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed)) - (float)Math.Floor(Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed))) - 0.5f) / 2) * display.PulseIntensity;
                        if (timer.FinishedColor.W < 0.75) {
                            finishedColor += new Vector4(0, 0, 0, s);
                        } else {
                            finishedColor -= new Vector4(0, 0, 0, s);
                        }
                    }
                    drawList.AddRectFilled(barTopLeft, barBottomRight, ImGui.GetColorU32(finishedColor));
                } else {
                    drawList.AddRectFilled(barTopLeft, barBottomRight - barFractionCompleteSize, ImGui.GetColorU32(display.BarBackgroundColor));
                    drawList.AddRectFilled(barTopLeft + barFractionIncompleteSize, barBottomRight, ImGui.GetColorU32(timer.ProgressColor));
                }

                if (hovered) {
                    drawList.AddRect(barTopLeft, barBottomRight, 0xFF0000FF);
                    drawList.AddRect(barTopLeft + Vector2.One, barBottomRight - Vector2.One, 0xFF0000FF);
                }


                if (display.ShowActionIcon && timer.IconId > 0) {

                    var iconSize = new Vector2(display.RowSize * display.ActionIconScale);
                    
                    ImGui.SetCursorPosY(cPosY + barSize.Y / 2 - iconSize.X / 2);
                    ImGui.SetCursorPosX(cPosX + barSize.X / 2 - iconSize.X / 2);
                    var icon = IconManager.GetIconTexture(timer.IconId);

                    if (icon != null) {
                        ImGui.Image(icon.ImGuiHandle, iconSize);
                    }
                }

                if (timer.AllowCountdown && display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                    var countdownText = Math.Abs(timer.TimerRemaining).ToString("F1");
                    var countdownSize = ImGui.CalcTextSize(countdownText);
                    ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f) - (countdownSize.Y / 2));
                    ImGui.SetCursorPosX(cPosX + (display.RowSize / 2f) - (countdownSize.X / 2));

                    // ImGui.TextColored(display.TextColor, countdownText);
                    TextShadowed(countdownText, display.TextColor, new Vector4(0, 0, 0, 1), 2);
                }

                ImGui.EndGroup();
                if (timer.ClickAction != null) {
                    if (hovered && ImGui.GetIO().MouseDown[0]) {
                        timer.ClickAction?.Invoke(this, timer.ClickParam);
                    }
                }
                

                var newX = cPosX;
                var newY = cPosY;
                if (display.IconVerticalStack) {
                    if (display.DirectionBtT) {
                        newY = cPosY - display.RowSize - display.BarSpacing;
                        if (newY < 0 + ImGui.GetStyle().WindowPadding.Y) {
                            newY = sPosY;
                            if (display.DirectionRtL) {
                                newX = cPosX - display.RowSize - display.BarSpacing;
                            } else {
                                newX = cPosX + display.RowSize + display.BarSpacing;
                            }
                        }
                    } else {
                        newY = cPosY + display.RowSize + display.BarSpacing;
                        newX = cPosX;
                        if (newY > ImGui.GetWindowHeight() - display.RowSize - ImGui.GetStyle().WindowPadding.Y) {
                            newY = sPosY;
                            if (display.DirectionRtL) {
                                newX = cPosX - display.RowSize - display.BarSpacing;
                            } else {
                                newX = cPosX + display.RowSize + display.BarSpacing;
                            }
                        }
                    }
                } else {
                    if (display.DirectionRtL) {
                        newX = cPosX - display.RowSize - display.BarSpacing;
                        if (newX < 0 + ImGui.GetStyle().WindowPadding.X) {
                            newX = sPosX;
                            if (display.DirectionBtT) {
                                newY = cPosY - display.RowSize - display.BarSpacing;
                            } else {
                                newY = cPosY + display.RowSize + display.BarSpacing;
                            }
                        }
                    } else {
                        newX = cPosX + display.RowSize + display.BarSpacing;
                        newY = cPosY;
                        if (newX > ImGui.GetWindowWidth() - display.RowSize - ImGui.GetStyle().WindowPadding.X) {
                            newX = sPosX;
                            if (display.DirectionBtT) {
                                newY = cPosY - display.RowSize - display.BarSpacing;
                            } else {
                                newY = cPosY + display.RowSize + display.BarSpacing;
                            }
                        }
                    }
                }



                ImGui.SetCursorPosY(newY);
                ImGui.SetCursorPosX(newX);

            }
        }
    }
}
