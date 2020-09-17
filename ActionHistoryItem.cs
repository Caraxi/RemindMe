using System;
using System.Diagnostics;
using Dalamud.Game.ClientState.Actors.Types;
#if DEBUG
namespace RemindMe {
    public class ActionHistoryItem : Stopwatch, IDisposable {

        public uint ActionID { get; }
        public Actor Target { get; private set; }

        public ActionHistoryItem(uint actionID, Actor target) : base() {
            ActionID = actionID;
            Target = target;
            this.Start();
        }

        public void Dispose() {
            this.Stop();
            Target = null;
        }

    }
}
#endif
