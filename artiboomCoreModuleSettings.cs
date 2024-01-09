using Celeste.Mod.UI;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.artiboom
{
    public class artiboomModuleSettings : EverestModuleSettings {
        [SettingName("SETTING_ALTER_DASH")]
        [SettingSubHeader("--------------------------------------\n\nOriginal Artificer design by @allinluck\nOriginal Slugcat in Celeste concept by @lush_nyaa\n\n--------------------------------------")]
        [SettingSubText("Alters the dash mechanics to make them more true to Artificer's Rain World mechanics.\nBuggy, and may make some maps impossible.")]
        public bool AlterDash {get; set;} = false;
        [SettingName("SETTING_FOLLOW_ENABLE")]
        [SettingSubText("Enables a cosmetic follower, the Citizen ID Drone.")]
        public bool EnableFollower {get; set;} = true;
        [SettingName("SETTING_FOLLOW_X")]
	    [SettingRange(-32, 32)]
        public int FollowX {get; set;} = -6;
        [SettingName("SETTING_FOLLOW_Y")]
	    [SettingRange(-32, 32)]
        public int FollowY {get; set;} = 6;
        [SettingName("COMPATIBILITY_MODE")]
        [SettingSubText("Removes some features made redundant by Downpour of Slugcats+.")]
        public bool CompatibilityMode {get; set;} = false;
    }
}
