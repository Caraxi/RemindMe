using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {

    public class RemindMe : IDalamudPlugin {
        public string Name => "RemindMe";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow = false;

        public List<Action> ActionList;

        public IconManager IconManager;

        public void Dispose() {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            RemoveCommands();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {

            drawConfigWindow = true;

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

        private void DrawDisplays() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;

            foreach (var display in PluginConfig.MonitorDisplays.Values) {

                if (!display.Locked) {
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
                }

                if (display.Locked && display.OnlyInCombat && !PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat)) {
                    continue;
                }

                var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar;

                if (display.Locked) {
                    flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground;
                }

                ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(new Vector2(250, 250), ImGuiCond.FirstUseEver);

                ImGui.Begin($"Display##{display.Guid}", flags);
                if (!display.Locked) {
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                }
                var TimerList = new List<DisplayTimer>();
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
                                    if (se.EffectId == (short)status.RowId) {
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

                TimerList.Sort((a, b) => {
                    var diff = a.TimerRemaining - b.TimerRemaining;
                    if (Math.Abs(diff) < 0.1) return string.CompareOrdinal(a.Name, b.Name); // Equal
                    if (diff < 0) return -1;
                    return 1;
                });


                if (TimerList.Count > 0) {

                    foreach (var timer in TimerList) {
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, timer.ProgressColor);
                        if (timer.IsComplete) {

                            if (display.PulseReady) {
                                var s = Math.Abs((Math.Abs(timer.TimerRemaining) - (float)Math.Floor(Math.Abs(timer.TimerRemaining)) - 0.5f) / 2);

                                if (timer.FinishedColor.W < 0.75) {
                                    ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor + new Vector4(0, 0, 0, s));
                                } else {
                                    ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor - new Vector4(0, 0, 0, s));
                                }



                            } else {
                                ImGui.PushStyleColor(ImGuiCol.FrameBg, timer.FinishedColor);
                            }



                        }

                        ImGui.SetWindowFontScale(display.TextScale);

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

                        ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - size.Y / 2f));
                        ImGui.TextColored(display.TextColor, $"{timer.Name}");

                        if (display.ShowCountdown && (!timer.IsComplete || display.ShowCountdownReady)) {
                            var countdownText = Math.Abs(timer.TimerRemaining).ToString("F1");
                            var countdownSize = ImGui.CalcTextSize(countdownText);

                            ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - countdownSize.Y / 2f));
                            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (countdownSize.X + ImGui.GetStyle().WindowPadding.X + ImGui.GetStyle().FramePadding.X));

                            ImGui.TextColored(display.TextColor, countdownText);
                        }

                        ImGui.EndGroup();

                        ImGui.SetCursorPosY(cPosY + display.RowSize + ImGui.GetStyle().ItemSpacing.Y);

                        if (timer.IsComplete) {
                            ImGui.PopStyleColor();
                        }

                        ImGui.PopStyleColor();
                    }
                }

                ImGui.End();
            }
        }

        private void BuildUI() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            DrawDisplays();

        }
    }
}
