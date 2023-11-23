using Celeste.Mod.UI;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.artiboom
{
    public class artiboomModuleSettings : EverestModuleSettings {
        [SettingName("SETTING_CREDITS")]
        [SettingSubHeader("--------------------------------------\n\nOriginal Artificer design by @allinluck\nOriginal Slugcat in Celeste concept by @lush_nyaa\n\n--------------------------------------")]
        public bool Credits {get; set;} = true;

        [SettingName("SETTING_ALTER_DASH")]
        public bool AlterDash {get; set;} = true;
        [SettingName("SETTING_FOLLOW_ENABLE")]
        public bool EnableFollower {get; set;} = true;
        [SettingName("SETTING_FOLLOW_X")]
	    [SettingRange(-32, 32)]
        public int FollowX {get; set;} = -6;
        [SettingName("SETTING_FOLLOW_Y")]
	    [SettingRange(-32, 32)]
        public int FollowY {get; set;} = 6;
    }
}
