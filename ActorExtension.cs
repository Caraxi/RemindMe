using System.Reflection;
using ActorStruct = Dalamud.Game.ClientState.Structs.Actor;
using Actor = Dalamud.Game.ClientState.Actors.Types.Actor;
using Dalamud.Game.ClientState.Structs;

namespace RemindMe {
    internal static class ActorExtension {
        private static readonly FieldInfo actorStruct = typeof(Actor).GetField("actorStruct", BindingFlags.NonPublic | BindingFlags.Instance);

        public static StatusEffect[] GetStatusEffects(this Actor a) {
            return ((ActorStruct) actorStruct.GetValue(a)).UIStatusEffects;
        }

    }
}
