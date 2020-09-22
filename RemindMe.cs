using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {

    public partial class RemindMe : IDalamudPlugin {
        public string Name => "Remind Me";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow = false;

        public List<Action> ActionList;

        public IconManager IconManager;

        private readonly Stopwatch generalStopwatch = new Stopwatch();

        internal Stopwatch OutOfCombatTimer = new Stopwatch();

        public void Dispose() {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            PluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            generalStopwatch.Stop();
            OutOfCombatTimer.Stop();
            RemoveCommands();
            PluginInterface.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            generalStopwatch.Start();
#if DEBUG
            drawConfigWindow = true;
#endif
            this.PluginInterface = pluginInterface;
            this.PluginConfig = (RemindMeConfig)pluginInterface.GetPluginConfig() ?? new RemindMeConfig();
            this.PluginConfig.Init(this, pluginInterface);

            PluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;

            IconManager = new IconManager(pluginInterface);
            ActionList = PluginInterface.Data.Excel.GetSheet<Action>().Where(a => a.IsPlayerAction).ToList();

            actionManagerStatic = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");

            ActionManager = new ActionManager(this, actionManagerStatic);

            pluginInterface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;

            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;

            SetupCommands();
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {
            if (PluginInterface.ClientState?.LocalPlayer == null) return;
            var inCombat = PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat);
            if (OutOfCombatTimer.IsRunning && inCombat) {
                generalStopwatch.Restart();
                ActionManager.ResetTimers();
                OutOfCombatTimer.Stop();
                OutOfCombatTimer.Reset();
            } else if (!OutOfCombatTimer.IsRunning && !inCombat) {
                OutOfCombatTimer.Start();
            }
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

                if (display.Locked && display.OnlyInCombat) {
                    var inCombat = PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat);

                    if (!inCombat && !display.KeepVisibleOutsideCombat) continue;

                    if (!inCombat && display.KeepVisibleOutsideCombat) {
                        if (OutOfCombatTimer.Elapsed.TotalSeconds > display.KeepVisibleOutsideCombatSeconds) {
                            continue;
                        }
                    }
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
                        case 0: {
                            DrawDisplayHorizontal(display, TimerList);
                            break;
                        }
                        case 1: {
                            DrawDisplayVertical(display, TimerList);
                            break;
                        }
                        case 2: {
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
        
        private void BuildUI() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            DrawDisplays();

        }
    }
}
