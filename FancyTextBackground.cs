
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.artiboom {
    public static class FancyBackground {
            
        public class TextboxChanger : FancyText.Node {
            public readonly string path = "";

            public TextboxChanger(string path) {
                this.path = path;
            }
        }

        public static void ModFancyBackgroundParse(ILContext il) {
            // Modified from https://github.com/l-Luna/PrismaticHelper/blob/master/PrismaticHelper/Cutscenes/ParserHooks.cs

            ILCursor cursor = new(il);
            
            if(cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdstr("savedata"))){
                Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Hooking into FancyText.Parse");
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.Emit(OpCodes.Ldloc_S, il.Method.Body.Variables[7]); // s
                cursor.Emit(OpCodes.Ldloc_S, il.Method.Body.Variables[8]); // stringList
                cursor.EmitDelegate<Action<FancyText, string, List<string>>>((text, s, vals) => {
                    var parserData = new DynamicData(text);
                    FancyText.Text group = parserData.Get<FancyText.Text>("group");
                    List<FancyText.Node> nodes = group.Nodes;
                    switch(s){
                        case "artiboom_textbox":
                            nodes.Add(new TextboxChanger(vals.FirstOrDefault() ?? "default"));
                            break;
                    }
                });
            }
        }

        public static void ModFancyBackground(ILContext il) {
            ILCursor cursor = new(il);
            if(!(
                cursor.TryGotoNext(MoveType.Before, instr => instr.MatchIsinst<FancyText.Portrait>()) &&
                cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchLdarg(0))
            )) {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), "Failed to hook into Textbox.RunRoutine");
                return;
            }
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Hooking into into Textbox.RunRoutine");
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit<FancyText.Node>(OpCodes.Ldfld, "<current>5__4"); // current
            cursor.EmitDelegate<Action<Textbox, FancyText.Node>>((self, current) => {
                if (current is TextboxChanger) {
                    var curr = (TextboxChanger)current;
                    string text = "textbox/" + curr.path;
					typeof(Textbox).GetField("textbox", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(self, GFX.Portraits[text]);
					if (GFX.Portraits.Has(text + "_overlay"))
					{
                        typeof(Textbox).GetField("textboxOverlay", BindingFlags.NonPublic | BindingFlags.Instance)
                            .SetValue(self, GFX.Portraits[text + "_overlay"]);
                    }
                    
                }
            });
        }
    }
}