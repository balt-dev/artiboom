using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.artiboom;
using Celeste.Mod.artiboom.Entities;
using Monocle;
using System.Linq;

internal class FollowerManager
{
	private Sofanthiel follower;

	private bool wasActiveOnLastFrame = false;
	private bool pastMirror = false;

	private bool CheckPastMirror(Level level) {
		var area = level.Session.Area;
		var campaign = area.GetLevelSet();
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"In campaign {campaign}");
		if (campaign != "Celeste") {
			return true;
		}
		var chapter = area.ChapterIndex;
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"In chapter {chapter}");
		if (chapter != 5) {
			return chapter < 5;
		}
		var side = area.Mode;
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"In side {side}");
		var room = level.Session.LevelData.Name;
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"In room {room}");
        return side switch {
            AreaMode.CSide => false,
            AreaMode.Normal => false,
            AreaMode.BSide => room[0] == 'c' || room[0] == 'd',
            _ => true,
        };
    }

	public void Load()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Loaded Sofanthiel.");
		Everest.Events.Level.OnPause += OnPause;
		On.Celeste.Level.LoadLevel += OnLoadLevel;
		On.Celeste.Level.UnloadLevel += OnUnloadLevel;
		On.Celeste.Player.Update += OnPlayerUpdate;
	}

	public void UnLoad()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Unloaded Sofanthiel.");
		Everest.Events.Level.OnPause -= OnPause;
		On.Celeste.Level.LoadLevel -= OnLoadLevel;
		On.Celeste.Level.UnloadLevel -= OnUnloadLevel;
		On.Celeste.Player.Update -= OnPlayerUpdate;
	}

	private void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig.Invoke(self, playerIntro, isFromLoader);
		Player entity = self.Tracker.GetEntity<Player>();
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Loading level...");
		CheckPastMirror(self);
		if (ArtiboomModule.Settings.EnableFollower && entity != null)
		{
			follower = new Sofanthiel(entity.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)entity.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			self.Add(follower);
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}

	private void OnUnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
		orig.Invoke(self);
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Unloading level...");
		wasActiveOnLastFrame = false;
	}

	public void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig.Invoke(self);
		if (Engine.Scene is Level level) {
			if (!wasActiveOnLastFrame && ArtiboomModule.Settings.EnableFollower && level.Tracker.CountEntities<Sofanthiel>() == 0)
			{
				follower = new Sofanthiel(self.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)self.Facing, -ArtiboomModule.Settings.FollowY - 5f));
				level.Add(follower);
				follower.Position = self.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)self.Facing, -ArtiboomModule.Settings.FollowY - 5f);
			}
			if (wasActiveOnLastFrame && !ArtiboomModule.Settings.EnableFollower && level.Tracker.CountEntities<Sofanthiel>() > 0)
			{
				follower.Disable();
			}
			wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
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
	}

	public void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
	{
		follower?.UpdateSettings();
	}
}