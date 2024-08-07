using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Artiboom;
using Celeste.Mod.Artiboom.Entities;
using Monocle;
using System.Linq;

internal class FollowerManager
{
	private static Sofanthiel follower;
	private static CommunicationMark mark;

	private static bool wasActiveOnLastFrame = false;
	private static bool pastMirror = false;

	private static bool CheckPastMirror(Level level) {
		var area = level.Session.Area;
		var campaign = area.GetLevelSet();
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), $"In campaign {campaign}");
		if (campaign != "Celeste") {
			return true;
		}
		var chapter = area.ChapterIndex;
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), $"In chapter {chapter}");
		if (chapter != 5) {
			return chapter > 5;
		}
		var side = area.Mode;
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), $"In side {side}");
		var room = level.Session.LevelData.Name;
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), $"In room {room}");
        return side switch {
            AreaMode.CSide => false,
            AreaMode.Normal => room[0] == 'c' || room[0] == 'd' || room[0] == 'e',
            AreaMode.BSide => room[0] == 'c' || room[0] == 'd',
            _ => true,
        };
    }

	public static void Load()
	{
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), "Loaded Sofanthiel.");
		Everest.Events.Level.OnPause += OnPause;
		On.Celeste.Level.LoadLevel += OnLoadLevel;
		On.Celeste.Level.UnloadLevel += OnUnloadLevel;
		On.Celeste.Player.Update += OnPlayerUpdate;
	}

	public static void Unload()
	{
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), "Unloaded Sofanthiel.");
		Everest.Events.Level.OnPause -= OnPause;
		On.Celeste.Level.LoadLevel -= OnLoadLevel;
		On.Celeste.Level.UnloadLevel -= OnUnloadLevel;
		On.Celeste.Player.Update -= OnPlayerUpdate;
	}

	private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig(self, playerIntro, isFromLoader);
		Player entity = self.Tracker.GetEntity<Player>();
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), "Loading level...");
		pastMirror = CheckPastMirror(self);
		if (pastMirror) {
			Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), "Past mirror!");
		}
		if (ArtiboomModule.Settings.EnableFollower && entity != null)
		{
			follower = new Sofanthiel(entity.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)entity.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			self.Add(follower);
			if (pastMirror) {
				mark = new CommunicationMark(entity.Center + new Vector2(0, -ArtiboomModule.Settings.FollowY - 5f));
				if (SaveData.Instance.Assists.PlayAsBadeline) {
					mark.sprite.Play("badelineLoop");
				}
				self.Add(mark);
			}
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}
	
	private static void OnUnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
		orig(self);
		Logger.Log(LogLevel.Debug, nameof(ArtiboomModule), "Unloading level...");
		wasActiveOnLastFrame = false;
	}

	public static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig(self);
		if (Engine.Scene is Level level) {
			if (!wasActiveOnLastFrame && ArtiboomModule.Settings.EnableFollower)
			{
				if (level.Tracker.CountEntities<Sofanthiel>() == 0) {
					follower = new Sofanthiel(self.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)self.Facing, -ArtiboomModule.Settings.FollowY - 5f));
					level.Add(follower);
				}
				if (pastMirror && level.Tracker.CountEntities<CommunicationMark>() == 0) {
					mark = new CommunicationMark(self.Center + new Vector2(0, -ArtiboomModule.Settings.FollowY - 5f));
					level.Add(mark);
				}
			}
			
			if (wasActiveOnLastFrame && !ArtiboomModule.Settings.EnableFollower)
			{
				if (level.Tracker.CountEntities<Sofanthiel>() > 0)
					follower.Disable();
				if (level.Tracker.CountEntities<CommunicationMark>() > 0)
					mark.Disable();
			}
			wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
			if (mark?.Active == true) { // If not null and not false
				if (SaveData.Instance.Assists.PlayAsBadeline && mark.sprite.CurrentAnimationID != "badelineLoop") {
					mark.sprite.Play("badelineLoop");
				}
				if (!SaveData.Instance.Assists.PlayAsBadeline && mark.sprite.CurrentAnimationID != "madelineLoop") {
					mark.sprite.Play("madelineLoop");
				}
			}
		} else {
			wasActiveOnLastFrame = false;
		}
	}

	public static void RefreshSettings()
	{
		Level level = Engine.Scene as Level;
		foreach (Sofanthiel item in level.Entities.FindAll<Sofanthiel>())
		{
			item.UpdateSettings();
		}
		foreach (CommunicationMark item in level.Entities.FindAll<CommunicationMark>())
		{
			item.UpdateSettings();
		}
	}

	public static void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
	{
		follower?.UpdateSettings();
		mark?.UpdateSettings();
	}
}