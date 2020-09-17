using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImGuiScene;
using Action =  Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {
    public class Cooldown : IDisposable {
        private readonly Action action;
        private readonly ActionManager actionManager;

        private CooldownStruct cooldownStruct;

        private readonly Stopwatch readyStopwatch = new Stopwatch();

        public TextureWrap ActionIconTexture { get; private set; }

        public Cooldown(ActionManager actionManager, Action action) {
            this.actionManager = actionManager;
            this.action = action;
            
            cooldownStruct = new CooldownStruct() { ActionID = this.ActionID, CooldownElapsed = 0, CooldownTotal = 0, IsCooldown = false };
            readyStopwatch.Start();
            Update();

            Task.Run(() => {
                if (action.Icon > 0 && action.IsPlayerAction) ActionIconTexture = actionManager.GetActionIcon(action);
            });

        }

        public void Update() {

            var before = cooldownStruct.IsCooldown;

            if (action.CooldownGroup > 0) {
                var cooldownPtr = actionManager.GetCooldownPointer(action.CooldownGroup);
                cooldownStruct = Marshal.PtrToStructure<CooldownStruct>(cooldownPtr);
            } else {
                cooldownStruct = new CooldownStruct() {ActionID = this.ActionID, CooldownElapsed = 0, CooldownTotal = 0, IsCooldown = false};
            }

            if (before && !cooldownStruct.IsCooldown) {
                readyStopwatch.Restart();
            }

        }
        
        public uint ActionID => action.RowId;

        public byte CooldownGroup => action.CooldownGroup;

        public float CooldownElapsed => cooldownStruct.CooldownElapsed;

        public float Countdown => cooldownStruct.CooldownTotal - cooldownStruct.CooldownElapsed;

        public long CountdownTicks => (long) Countdown * 10000000;

        public float CooldownTotal => cooldownStruct.CooldownTotal;

        public float CooldownFraction => cooldownStruct.CooldownTotal <= 0 ? 1 : (cooldownStruct.CooldownElapsed / cooldownStruct.CooldownTotal);

        public float CompleteFor => IsOnCooldown ? 0 : readyStopwatch.ElapsedMilliseconds / 1000f;

        public long CompleteForTicks => IsOnCooldown ? 0 : readyStopwatch.ElapsedTicks;

        public bool IsOnCooldown => cooldownStruct.IsCooldown;

        public void Dispose() {
            readyStopwatch.Stop();
            ActionIconTexture?.Dispose();
        }

       
    }
}
