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
	private CommunicationMark mark;

	private bool wasActiveOnLastFrame = false;
	private bool pastMirror = false;

	private bool CheckPastMirror(Level level) {
		var area = level.Session.Area;
		var campaign = area.GetLevelSet();
		Logger.Log(nameof(ArtiboomModule), $"In campaign {campaign}");
		if (campaign != "Celeste") {
			return true;
		}
		var chapter = area.ChapterIndex;
		Logger.Log(nameof(ArtiboomModule), $"In chapter {chapter}");
		if (chapter != 5) {
			return chapter < 5;
		}
		var side = area.Mode;
		Logger.Log(nameof(ArtiboomModule), $"In side {side}");
		var room = level.Session.LevelData.Name;
		Logger.Log(nameof(ArtiboomModule), $"In room {room}");
        return side switch {
            AreaMode.CSide => false,
            AreaMode.Normal => room[0] == 'c' || room[0] == 'd' || room[0] == 'e',
            AreaMode.BSide => room[0] == 'c' || room[0] == 'd',
            _ => true,
        };
    }

	public void Load()
	{
		Logger.Log(nameof(ArtiboomModule), "Loaded Sofanthiel.");
		Everest.Events.Level.OnPause += OnPause;
		On.Celeste.Level.LoadLevel += OnLoadLevel;
		On.Celeste.Level.UnloadLevel += OnUnloadLevel;
		On.Celeste.Player.Update += OnPlayerUpdate;
	}

	public void UnLoad()
	{
		Logger.Log(nameof(ArtiboomModule), "Unloaded Sofanthiel.");
		Everest.Events.Level.OnPause -= OnPause;
		On.Celeste.Level.LoadLevel -= OnLoadLevel;
		On.Celeste.Level.UnloadLevel -= OnUnloadLevel;
		On.Celeste.Player.Update -= OnPlayerUpdate;
	}

	private void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig.Invoke(self, playerIntro, isFromLoader);
		Player entity = self.Tracker.GetEntity<Player>();
		Logger.Log(nameof(ArtiboomModule), "Loading level...");
		pastMirror = CheckPastMirror(self);
		if (pastMirror) {
			Logger.Log(nameof(ArtiboomModule), "Past mirror!");
		}
		if (ArtiboomModule.Settings.EnableFollower && entity != null)
		{
			follower = new Sofanthiel(entity.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)entity.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			self.Add(follower);
			if (pastMirror) {
				mark = new CommunicationMark(entity.Center + new Vector2(0, -ArtiboomModule.Settings.FollowY - 5f));
				self.Add(mark);
			}
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}

	private void OnUnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
		orig.Invoke(self);
		Logger.Log(nameof(ArtiboomModule), "Unloading level...");
		wasActiveOnLastFrame = false;
	}

	public void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig.Invoke(self);
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

	public void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
	{
		follower?.UpdateSettings();
		mark?.UpdateSettings();
	}
}