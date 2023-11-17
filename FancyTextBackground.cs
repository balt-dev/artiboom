
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
                            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"{nodes}");
                            break;
                    }
                });
            }
        }

        public static void ModFancyBackground(ILContext il, Type EnumeratorType) {
            // Thanks to vividescence for basically writing this function for me
            ILCursor cursor = new(il);
            FieldInfo current = EnumeratorType.GetField("<current>5__4", BindingFlags.NonPublic | BindingFlags.Instance);
            ILLabel label = null;
            // Various checks
            if(!cursor.TryGotoNext(i=>i.MatchLdarg(0), i=>i.MatchLdfld(current), i => i.MatchIsinst<FancyText.Anchor>(), i=>i.MatchBrfalse(out _))) { // This may need to change dependent on hook interactions.
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), "Failed to hook into Textbox.RunRoutine"); return;
            }
            ILCursor clone = cursor.Clone();
            if(!(clone.TryGotoNext(i=>i.MatchLdarg(0), i=>i.MatchLdfld(current), i => i.MatchIsinst<FancyText.Portrait>(), i=>i.MatchBrfalse(out _)) && clone.TryGotoPrev(i => i.MatchBr(out label)))) { 
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), "Failed to hook into Textbox.RunRoutine"); return;
            }
            //clone.Dispose();
            clone = null;
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"Hooking into into Textbox.RunRoutine {cursor.Next}");
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldfld, EnumeratorType.GetField("<>4__this"));
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldfld, current);
            cursor.EmitDelegate<Func<Textbox, FancyText.Node, bool>>((self, node) => {
                // Find the node
                Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Searching for custom node...");
                if(node is not TextboxChanger curr) return false;
                Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Found it!");

                // Reset the portrait, since this shouldn't be used for anything that can speak
                typeof(Textbox).GetField("portraitExists", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, false);
                typeof(Textbox).GetField("activeTalker", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, null);
                typeof(Textbox).GetField("isPortraitGlitchy", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, false);
                typeof(Textbox).GetField("portrait", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, null);

                // Set the background
                string text = "textbox/" + curr.path;
                typeof(Textbox).GetField("textbox", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(self, GFX.Portraits[text]);
                var overlay = typeof(Textbox).GetField("textboxOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GFX.Portraits.Has(text + "_overlay")) {
                    overlay.SetValue(self, GFX.Portraits[text + "_overlay"]);
                } else {
                    overlay.SetValue(self, null);
                }
                return true;
            });
            cursor.Emit(OpCodes.Brtrue, label);
        }
    }
}