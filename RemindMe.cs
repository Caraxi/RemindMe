using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {

    public class RemindMe : IDalamudPlugin {
        public string Name => "Remind Me";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow = false;

        public List<Action> ActionList;

        public IconManager IconManager;

        private Stopwatch generalStopwatch = new Stopwatch();

        public void Dispose() {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            generalStopwatch.Stop();
            RemoveCommands();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            generalStopwatch.Start();
#if DEBUG
            drawConfigWindow = true;
#endif
            this.PluginInterface = pluginInterface;
            this.PluginConfig = (RemindMeConfig)pluginInterface.GetPluginConfig() ?? new RemindMeConfig();
            this.PluginConfig.Init(this, pluginInterface);

            IconManager = new IconManager(pluginInterface);
            ActionList = PluginInterface.Data.Excel.GetSheet<Action>().Where(a => a.IsPlayerAction).ToList();

            actionManagerStatic = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");

            ActionManager = new ActionManager(this, actionManagerStatic);

            pluginInterface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;

            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;

            SetupCommands();
        }

        private void OnOpenConfigUi(object sender, EventArgs e) {
            drawConfigWindow = true;
        }

        public void SetupCommands() {
            PluginInterface.CommandManager.AddHandler("/remindme", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler) {
                HelpMessage = $"Open config window for {this.Name}",
                ShowInHelp = true
            });
        }

        public void OnConfigCommandHandler(string command, string args) {
            drawConfigWindow = !drawConfigWindow;
        }

        public void RemoveCommands() {
            PluginInterface.CommandManager.RemoveHandler("/remindme");
        }

        private void TextShadowed(string text, Vector4 foregroundColor, Vector4 shadowColor, byte shadowWidth = 1) {
            var x = ImGui.GetCursorPosX();
            var y = ImGui.GetCursorPosY();

            for (var i = -shadowWidth; i < shadowWidth; i++) {
                for (var j = -shadowWidth; j < shadowWidth; j++) {
                    ImGui.SetCursorPosX(x + i);
                    ImGui.SetCursorPosY(y + j);
                    ImGui.TextColored(shadowColor, text);
                }
            }
            ImGui.SetCursorPosX(x);
            ImGui.SetCursorPosY(y);
            ImGui.TextColored(foregroundColor, text);
        }

        private void DrawDisplays() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;

            foreach (var display in PluginConfig.MonitorDisplays.Values) {

                if (display.Locked && display.OnlyInCombat && !PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat)) {
                    continue;
                }

                var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar;

                if (display.Locked) {
                    flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground;
                }

                var TimerList = new List<DisplayTimer>();

                try {
                    if (display.Cooldowns.Count > 0) {

                        foreach (var cd in display.Cooldowns.Where(cd => {
                            if (cd.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                            var action = ActionManager.GetAction(cd.ActionId);
                            if (action == null || !action.ClassJobCategory.Value.HasClass(PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
                            if (action.ClassJobLevel > PluginInterface.ClientState.LocalPlayer.Level) return false;
                            var cooldown = ActionManager.GetActionCooldown(action);
                            if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                            if (display.OnlyShowCooldown && !cooldown.IsOnCooldown) return false;
                            if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                            if (display.LimitDisplayReadyTime && cooldown.CompleteFor > display.LimitDisplayReadyTimeSeconds) return false;
                            return true;
                        })) {
                            var action = ActionManager.GetAction(cd.ActionId);

                            if (action != null) {
                                var cooldown = ActionManager.GetActionCooldown(action);
                                TimerList.Add(new DisplayTimer {
                                    TimerMax = cooldown.CooldownTotal,
                                    TimerCurrent = cooldown.CooldownElapsed + cooldown.CompleteFor,
                                    FinishedColor = display.AbilityReadyColor,
                                    ProgressColor = display.AbilityCooldownColor,
                                    IconId = action.Icon,
                                    Name = action.Name
                                });
                            }
                        }
                    }
                } catch (Exception ex) {
                    PluginLog.LogError("Error parsing cooldowns.");
                    PluginLog.Log(ex.ToString());
                }

                try {
                    if (display.StatusMonitors.Count > 0) {

                        foreach (var sd in display.StatusMonitors.Where(sm => {
                            if (sm.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                            return true;
                        })) {
                            var status = PluginInterface.Data.Excel.GetSheet<Status>().GetRow(sd.Status);
                            var action = ActionManager.GetAction(sd.Action);

                            foreach (var a in PluginInterface.ClientState.Actors) {
                                if (a != null) {
                                    foreach (var se in a.GetStatusEffects()) {
                                        if (se.OwnerId != PluginInterface.ClientState.LocalPlayer.ActorId) continue;
                                        if (display.LimitDisplayTime && se.Duration > display.LimitDisplayTimeSeconds) continue;
                                        if (se.EffectId == (short) status.RowId) {
                                            TimerList.Add(new DisplayTimer {
                                                TimerMax = sd.MaxDuration,
                                                TimerCurrent = sd.MaxDuration - se.Duration,
                                                FinishedColor = display.AbilityReadyColor,
                                                ProgressColor = display.StatusEffectColor,
                                                IconId = action.Icon,
                                                Name = $"{action.Name} on {a.Name}"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    PluginLog.LogError("Error parsing statuses.");
                    PluginLog.Log(ex.ToString());
                }

                TimerList.Sort((a, b) => {
                    var diff = a.TimerRemaining - b.TimerRemaining;
                    if (Math.Abs(diff) < 0.1) return string.CompareOrdinal(a.Name, b.Name); // Equal
                    if (diff < 0) return -1;
                    return 1;
                });

                foreach (var reminder in display.GeneralReminders) {
                    if (reminder.ShouldShow(PluginInterface, this, display)) {
                        TimerList.Insert(0, new DisplayTimer {
                            TimerMax = 1,
                            TimerCurrent = 1 + generalStopwatch.ElapsedMilliseconds / 1000f,
                            FinishedColor = display.AbilityReadyColor,
                            ProgressColor = display.StatusEffectColor,
                            IconId = reminder.GetIconID(PluginInterface, this, display),
                            Name = reminder.GetText(PluginInterface, this, display),
                            AllowCountdown = false
                        });
                    }
                }

                if (TimerList.Count > 0 || !display.Locked) {

                    ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.FirstUseEver);
                    ImGui.SetNextWindowPos(new Vector2(250, 250), ImGuiCond.FirstUseEver);

                    if (!display.Locked) {
                        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 1));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
                    }
                    ImGui.Begin($"Display##{display.Guid}", flags);
                    if (!display.Locked) {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleVar();
                    }

                    switch (display.DisplayType) {
                        case 0:
                        case 1: {
                            DrawDisplayHorizontal(display, TimerList);
                            break;
                        }
                        case 2:
                        case 3: {
                            DrawDisplayVertical(display, TimerList);
                            break;
                        }
                        case 4: {
                            DrawDisplayIcons(display, TimerList);
                            break;
                        }
                        default: {
                            display.DisplayType = 0;
                            DrawDisplayHorizontal(display, TimerList);
                            break;
                        }
                    }

                    ImGui.End();
                }
            }
        }

        private void DrawDisplayIcons(MonitorDisplay display, List<DisplayTimer> timerList) {
            var sPosX = ImGui.GetCursorPosX();
            ImGui.SetWindowFontScale(display.TextScale);
            foreach (var timer in timerList) {
                var cPosX = ImGui.GetCursorPosX();
                var cPosY = ImGui.GetCursorPosY();
                var fraction = timer.TimerFractionComplete;
                ImGui.BeginGroup();

                var drawList = ImGui.GetWindowDrawList();

                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + new Vector2(display.RowSize);

                var barSize = barBottomRight - barTopLeft;

                var barFractionCompleteSize = new Vector2(0, barSize.Y * (1 - fraction));
                var barFractionIncompleteSize = new Vector2(0, barSize.Y * fraction);

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

                if (display.ShowActionIcon && timer.IconId > 0) {
                    ImGui.SetCursorPosY(cPosY + barSize.Y + ImGui.GetStyle().FramePadding.Y - display.RowSize);
                    ImGui.SetCursorPosX(cPosX + ImGui.GetStyle().FramePadding.X);
                    var icon = IconManager.GetIconTexture(timer.IconId);
                    if (icon != null) {
                        ImGui.Image(icon.ImGuiHandle, new Vector2(display.RowSize - ImGui.GetStyle().FramePadding.X * 2, display.RowSize - ImGui.GetStyle().FramePadding.X * 2));
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


                var newX = cPosX + display.RowSize + display.BarSpacing;
                var newY = cPosY;
                if (newX > ImGui.GetWindowWidth() - display.RowSize - ImGui.GetStyle().WindowPadding.X) {
                    newX = sPosX;
                    newY = cPosY + display.RowSize + display.BarSpacing;
                }

                ImGui.SetCursorPosX(newX);
                ImGui.SetCursorPosY(newY);
            }
        }

        private unsafe void DrawDisplayVertical(MonitorDisplay display, List<DisplayTimer> timerList) {
            var rightToLeft = display.DisplayType == 3;

            if (rightToLeft) {
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (display.RowSize + ImGui.GetStyle().WindowPadding.X));
            }
            ImGui.SetWindowFontScale(display.TextScale);
            foreach (var timer in timerList) {
                var cPosX = ImGui.GetCursorPosX();
                var cPosY = ImGui.GetCursorPosY();
                var fraction = timer.TimerFractionComplete;

                ImGui.BeginGroup();
                
                var drawList = ImGui.GetWindowDrawList();

                var barTopLeft = ImGui.GetCursorScreenPos();
                var barBottomRight = ImGui.GetCursorScreenPos() + new Vector2(display.RowSize, ImGui.GetWindowHeight() - ImGui.GetStyle().WindowPadding.Y * 2);

                var barSize = barBottomRight - barTopLeft;
                
                var barFractionCompleteSize = new Vector2(0, barSize.Y * (1 - fraction));
                var barFractionIncompleteSize = new Vector2(0, barSize.Y * fraction);

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
                    ImGui.SetCursorPosY(cPosY + barSize.Y + ImGui.GetStyle().FramePadding.Y - display.RowSize);
                    ImGui.SetCursorPosX(cPosX + ImGui.GetStyle().FramePadding.X);
                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {
                            ImGui.Image(icon.ImGuiHandle, new Vector2(display.RowSize - ImGui.GetStyle().FramePadding.X * 2, display.RowSize - ImGui.GetStyle().FramePadding.X * 2));
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

                ImGui.EndGroup();
                ImGui.SameLine();
                if (rightToLeft) {
                    ImGui.SetCursorPosX(cPosX - display.RowSize - display.BarSpacing);
                } else {
                    ImGui.SetCursorPosX(cPosX + display.RowSize + display.BarSpacing);
                }

            }

        }

        private void DrawDisplayHorizontal(MonitorDisplay display, List<DisplayTimer> timerList) {
            var bottomToTop = display.DisplayType == 1;


            if (bottomToTop) {
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

                }

                var size = ImGui.CalcTextSize(timer.Name);
                var cPosY = ImGui.GetCursorPosY();

                var fraction = timer.TimerFractionComplete;

                if (display.LimitDisplayTime && timer.TimerMax > display.LimitDisplayTimeSeconds) {
                    fraction = (display.LimitDisplayTimeSeconds - timer.TimerRemaining) / display.LimitDisplayTimeSeconds;
                }

                ImGui.ProgressBar(1 - fraction, new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2, display.RowSize), "");

                ImGui.BeginGroup();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);

                if (display.ShowActionIcon) {
                    ImGui.SetCursorPosY(cPosY + ImGui.GetStyle().FramePadding.X);

                    if (timer.IconId > 0) {
                        var icon = IconManager.GetIconTexture(timer.IconId);
                        if (icon != null) {
                            ImGui.Image(icon.ImGuiHandle, new Vector2(display.RowSize - ImGui.GetStyle().FramePadding.X * 2, display.RowSize - ImGui.GetStyle().FramePadding.X * 2));
                        }
                    }
                    ImGui.SameLine();
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

                if (bottomToTop) {
                    ImGui.SetCursorPosY(cPosY - display.RowSize - display.BarSpacing);
                } else {
                    ImGui.SetCursorPosY(cPosY + display.RowSize + display.BarSpacing);
                }
               

                if (timer.IsComplete) {
                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor();
            }
        }

        private void BuildUI() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            DrawDisplays();

        }
    }
}
