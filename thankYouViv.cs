using System.Reflection;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.artiboom {
    public class VivHack {
        // @ OTHER PROGRAMMERS DO NOT USE THIS METHOD I BEG YOU
        public static void ForceSetStateOverrideOnPlayerDash(ILContext il) {
            ILCursor cursor = new(il);
            // checks after the state equality check "if state is already the state you're telling the game to set to, don't set it." This works for both ForceState and set_State
            if (cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg(0), j => j.MatchLdfld<StateMachine>("Log"))) {
                cursor.Emit(OpCodes.Ldarg, 0); // stateMachine
                cursor.Emit(OpCodes.Dup); // stateMachine, stateMachine
                cursor.Emit(OpCodes.Ldfld, typeof(StateMachine).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic)); // stateMachine, stateMachine.state
                cursor.Emit(OpCodes.Ldarg, 1); // stateMachine, stateMachine.state, newState
                // This call is to save a miniscule amount of memory
                cursor.Emit(OpCodes.Call, typeof(ArtiboomModule).GetMethod("OverrideDashCheck", BindingFlags.NonPublic | BindingFlags.Static));
                cursor.Emit(OpCodes.Starg, 1); // oooo rare instruction
            }
        }
    }
}