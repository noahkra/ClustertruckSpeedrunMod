using System.Collections.Generic;
using Random = System.Random;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Reflection;
using System.IO.Pipes;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using System.IO;
using System;

namespace ClustertruckSpeedrunModLib
{
	public static class Autosplitter
	{
		public static NamedPipeClientStream client = null;
		public static StreamReader pipeReader = null;
		public static StreamWriter pipeWriter = null;
		public static bool FirstSplit;

		public static void Connect()
		{
			try
			{
				client = new NamedPipeClientStream(".", "LiveSplit");
				client.Connect(1000);

				if (client.IsConnected)
				{
					Console.WriteLine($"[SPEEDRUNMOD] Connection to LiveSplit established!");

					pipeReader = new StreamReader(client);
					pipeWriter = new StreamWriter(client);
				}
			} 
			catch (TimeoutException)
			{
				Console.WriteLine("[SPEEDRUNMOD] Connection timed out. LiveSplit may not be running.");
				Disconnect();
			} 
			catch (IOException e)
			{
				Console.WriteLine($"[SPEEDRUNMOD] IOException: {e.Message}");
				Disconnect();
			} 
			catch (Exception ex)
			{
				Console.WriteLine($"[SPEEDRUNMOD] Exception: {ex.Message}");
				Disconnect();
			}

		}

		public static void Disconnect()
		{
			if (pipeWriter != null) { pipeWriter.Close(); pipeWriter = null; }
			if (pipeReader != null) { pipeReader.Close(); pipeReader = null; }
			if (client!= null) { client.Close(); client = null; }

			Console.WriteLine($"[SPEEDRUNMOD] Disconnected from LiveSplit");
		}

		public static bool IsConnected()
		{
			try
			{
				return client != null && client.IsConnected;
			} 
			catch (Exception e)
			{
				Console.WriteLine($"[SPEEDRUNMOD] Exception while checking connection: {e.Message}");
				return false;
			}
		}

		static void SendMessage(string message)
		{
			try
			{
				if (!IsConnected())
				{
					Console.WriteLine($"[SPEEDRUNMOD] Lost connection to LiveSplit. Reconnecting...");
					Connect();

					if (!IsConnected())
					{
						Console.WriteLine($"[SPEEDRUNMOD] Failed to reconnect to LiveSplit.");
						return;
					}
				}

				pipeWriter.WriteLine(message);
				pipeWriter.Flush();
			} 
			catch (IOException e)
			{
				Console.WriteLine($"[SPEEDRUNMOD] IOException: {e.Message}");
				Disconnect();
			} 
			catch (Exception e)
			{
				Console.WriteLine($"[SPEEDRUNMOD] Exception: {e.Message}");
				Disconnect();
			}
		}

		public static void Start()
		{
			SendMessage("starttimer");
		}

		public static void PauseGameTime()
		{
			SendMessage("pausegametime");
		}

		public static void UnpauseGameTime()
		{
			SendMessage("unpausegametime");
		}

		public static void Reset()
		{
			SendMessage("reset");
		}

		public static void Split()
		{
			SendMessage("split");
		}

		public static int GetSplitIndex()
		{
			SendMessage("getsplitindex");
			if (IsConnected()) {
				return int.Parse(pipeReader.ReadLine());
			}
			return -1;
		}
	}

	public static class Randomiser
	{
		readonly static List<int> AllLevels = Enumerable.Range(1, 89).ToList(); // Excluding boss, since it's always last
		readonly static List<string> MovementAbilities = new List<string> { String.Empty /*none*/, "Double jump", "air dash", "jetpack", "levitation", "Grappling hook", "truck boost", "disrespected blink", "surfing shoes", "trucker flip" };
		readonly static List<string> UtilityAbilities = new List<string> { String.Empty /*none*/, "Time slow", "Portable truck", "back truck", "trucksolute zero", "Epic mode", "SuperTruck", "Truck cannon" };
		static Random AbilityRandom;

		public static List<int> RandomisedLevels;
		public static int currentLevel;
		public static int seed;

		public static string abilityName;
		public static string utilityName;
			
		static void LevelRandomiser<T>(List<T> list, int seed)
		{
			Random rng = new Random(seed);
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static void Randomise()
		{
			currentLevel = -1;
			RandomisedLevels = new List<int>(AllLevels);
			seed = (int)DateTime.Now.Ticks;
			AbilityRandom = new Random(seed);
			LevelRandomiser(RandomisedLevels, seed);
			RandomisedLevels.Add(90); // Add boss
			Console.WriteLine($"[SPEEDRUNMOD] Randomiser levels: {string.Join(",", RandomisedLevels.Select(n => n.ToString()).ToArray())}\nseed:{DateTime.Now.Ticks}/{(int)DateTime.Now.Ticks}");
		}

		public static int NextLevel()
		{
			currentLevel++;
			info.abilityName = MovementAbilities[AbilityRandom.Next(0, MovementAbilities.Count)];
			info.utilityName = UtilityAbilities[AbilityRandom.Next(0, UtilityAbilities.Count)];
			Console.WriteLine($"[SPEEDRUNMOD] Randomiser level {currentLevel}: {RandomisedLevels[currentLevel]}. Abilities: {info.abilityName}/{info.utilityName}");
			return RandomisedLevels[currentLevel];
		}
	}

	public static class Patcher
	{
		public static bool Patched = false;
		readonly public static string version = "1.3.0";

		public static Rigidbody playRig = null;
		public static int FPSinterval;
		public static string prevFPS = null;
		public static float avgFPS;
		public static Stopwatch stopwatch = new Stopwatch();
		public static bool isInLevelComplete = false;
		public static bool nextLevelPressed = false;

		// Preferences
		public static bool EnableSpeedometer;
		public static int SpeedUnit;
		public static Color TruckColor;
		public static int TargetFramerate;
		public static bool EnableFPSCounter;
		public static bool DisableJump;
		public static bool InvertSprint;
		public static bool EnableTimer;
		public static bool EnableLivesplit;
		public static bool SplitByLevel;
		public static bool SplitResetInMenu;
		public static bool ConfineCursor;
		public static bool EnableTimerFix;
		public static bool EnableRandomiser;
		public static bool EnableTruckCannon;
		public static bool EnableSurfingShoes;
		public static bool EnableSpacebarNextLevel;
		
		public static void PrintAllChildren(Transform parent, int layer)
		{
			foreach (Transform child in parent)
			{
				for (int i = 0; i < layer; i++)
				{
					Console.Write("-");
				}
				Console.WriteLine($"{child.name}");
				PrintAllChildren(child, layer + 1);
			}
		}

		public static void DoPatching(
			bool _enableSpeedometer, int _speedUnit,
			float _truckColorR, float _truckColorG, float _truckColorB,
			int _targetFramerate, bool _enableFPSCounter, bool _disableJump,
			bool _invertSprint, bool _enableTimer, bool _enableLivesplit,
			bool _splitByLevel, bool _splitResetInMenu, bool _confineCursor,
			bool _enableTimerFix, bool _enableRandomiser, bool _enableTruckCannon, bool _enableSurfingShoes, bool _enableSpacebarNextLevel)
		{
			if (Patched) { return; } // Don't patch again, just incase...

			FPSinterval = 0;

			EnableSpeedometer = _enableSpeedometer;
			SpeedUnit = _speedUnit;
			TruckColor = new Color(_truckColorR, _truckColorG, _truckColorB, 1f);
			TargetFramerate = _targetFramerate;
			EnableFPSCounter = _enableFPSCounter;
			DisableJump = _disableJump;
			InvertSprint = _invertSprint;
			EnableTimer = _enableTimer;
			EnableLivesplit = _enableLivesplit;
			SplitByLevel = _splitByLevel;
			SplitResetInMenu = _splitResetInMenu;
			ConfineCursor = _confineCursor;
			EnableTimerFix = _enableTimerFix;
			EnableRandomiser = _enableRandomiser;
			EnableTruckCannon = _enableTruckCannon;
			EnableSurfingShoes = _enableSurfingShoes;
			EnableSpacebarNextLevel = _enableSpacebarNextLevel;

			try
			{
				var harmony = new Harmony("com.clustertruckspeedrun.mod");
				
#if DEBUG
				Harmony.DEBUG = true;
#endif

				if (EnableRandomiser)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying RandomiserPatch...");
					RandomiserPatch.Apply(harmony);
				}

				if (EnableLivesplit)
				{
					Console.WriteLine("[SPEEDRUNMOD] Enabling Autosplitter...");
					Autosplitter.Connect();
					Console.WriteLine("[SPEEDRUNMOD] Applying LivesplitPatch...");
					LivesplitPatch.Apply(harmony);
				}

				Console.WriteLine("[SPEEDRUNMOD] Applying MenuTitlePatch...");
				MenuTitlePatch.Apply(harmony);

				Console.WriteLine("[SPEEDRUNMOD] Applying TimerPatch...");
				AssignPlayer.Apply(harmony);
				TimerPatch.Apply(harmony);

				Console.WriteLine("[SPEEDRUNMOD] Applying TruckColorPatch...");
				TruckColorPatch.Apply(harmony);

				Console.WriteLine("[SPEEDRUNMOD] Applying FPSPatch...");
				FPSPatch.Apply(harmony);

				if (EnableSpacebarNextLevel)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying NextLevelButtonPatch");
					NextLevelButtonPatch.Apply(harmony);
				}

				if (DisableJump)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying JumplessPatch...");
					JumplessPatch.Apply(harmony);
				}
				if (InvertSprint)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying SprintPatch...");
					SprintPatch.Apply(harmony);
				}
				if (EnableTimer)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying ShowTimerPatch...");
					ShowTimerPatch.Apply(harmony);
				}

				if (EnableTimerFix)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying TimerFixPatch...");
					TimerFixPatch.Apply(harmony);
				}

				if (ConfineCursor)
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying ConfineCursorPatch...");
					ConfineCursorPatch.Apply(harmony);
				}

				if (!EnableRandomiser && (EnableSurfingShoes || EnableTruckCannon)) // hidden abilities break randomiser so can't have 'em both enabled.
				{
					Console.WriteLine("[SPEEDRUNMOD] Applying HiddenAbilityPatch...");
					HiddenAbilityPatch.Apply(harmony);
				}

				Console.WriteLine("[SPEEDRUNMOD] All patches applied successfully!");
				Patched = true;
			} catch (Exception ex)
			{
				Console.WriteLine($"[SPEEDRUNMOD] Patching Failed: {ex}");
			}
		}
	}

	static class LivesplitPatch
	{
		public static void Apply(Harmony harmony)
		{
			var pauseSplitOriginal = typeof(steam_WorkshopHandler).GetMethod(nameof(steam_WorkshopHandler.UploadScoreToLeaderBoard));
			var unpauseStartOriginal = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
			var menuResetOriginal = typeof(Manager).GetMethod(nameof(Manager.ActuallyGoToLevelSelect));
			var menuReset2Original = typeof(Manager).GetMethod(nameof(Manager.OpenMainMenuFromGame));

			var pauseSplitPatch = typeof(LivesplitPatch).GetMethod(nameof(PauseSplitPostfix));
			var unpauseStartPatch = typeof(LivesplitPatch).GetMethod(nameof(unpauseStartPrefix));
			var menuResetPatch = typeof(LivesplitPatch).GetMethod(nameof(MenuResetPostfix));

			harmony.Patch(pauseSplitOriginal, postfix: new HarmonyMethod(pauseSplitPatch));
			harmony.Patch(unpauseStartOriginal, prefix: new HarmonyMethod(unpauseStartPatch));
			harmony.Patch(menuResetOriginal, postfix: new HarmonyMethod(menuResetPatch));
			harmony.Patch(menuReset2Original, postfix: new HarmonyMethod(menuResetPatch));
		}

		public static void PauseSplitPostfix()
		{
			Autosplitter.PauseGameTime();

			// Only split if splitting by level or if first level of the world if randomiser isn't enabled, otherwise only split on boss
			if (((Patcher.SplitByLevel || info.currentLevel % 10 == 0) && !Patcher.EnableRandomiser) || info.currentLevel == 90)
			{
				Autosplitter.Split();
			}
		}

		public static void unpauseStartPrefix(player __instance)
		{
			if (__instance.framesSinceStart == 0)
			{
				if ((!Patcher.EnableRandomiser && info.currentLevel % 10 == 1) || 
					(Patcher.EnableRandomiser && Randomiser.currentLevel == 0))
				{
					if (Autosplitter.GetSplitIndex() == -1) // If splitter not running
					{
						Autosplitter.Start();
					}
					if (Autosplitter.GetSplitIndex() == 0) // If first split
					{
						Autosplitter.Reset();
						Autosplitter.Start();
					}
				}

				Autosplitter.UnpauseGameTime();
			}
		}

		public static void MenuResetPostfix() {
			if (!Autosplitter.IsConnected())
			{
				Autosplitter.Connect();
			}
			if (Patcher.SplitResetInMenu)
			{
				Autosplitter.Reset();
			}
		}
	}
	
	static class RandomiserPatch
	{
		public static void Apply(Harmony harmony)
		{
			var nextLevelOriginal = typeof(Manager).GetMethod(nameof(Manager.NextLevel));
			var playButtonOriginal = typeof(menuBar).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
			var levelSelectOriginal = typeof(pauseScreen).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var nextLevelPatch = typeof(RandomiserPatch).GetMethod(nameof(NextLevelPrefix));
			var playButtonPatch = typeof(RandomiserPatch).GetMethod(nameof(PlaybuttonPostfix));
			var levelSelectPatch = typeof(RandomiserPatch).GetMethod(nameof(LevelSelectPostfix));

			harmony.Patch(nextLevelOriginal, prefix: new HarmonyMethod(nextLevelPatch));
			harmony.Patch(playButtonOriginal, postfix: new HarmonyMethod(playButtonPatch));
			harmony.Patch(levelSelectOriginal, postfix: new HarmonyMethod(levelSelectPatch));
		}
		public static void NextLevelPrefix()
		{
			info.currentLevel = Randomiser.NextLevel() - 1;
		}

		public static void PlaybuttonPostfix(menuBar __instance)
		{
			Transform parent = __instance.leftMask.Find("Main");
			Transform playButton = parent.Find("play");
			Text playButtonText = playButton.Find("Text").GetComponent<Text>();

			playButtonText.text = "PLAY\nRANDOMISER";
			playButtonText.transform.parent.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 150f);

			parent.Find("abilities").gameObject.SetActive(false);

			playButton.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
			playButton.gameObject.GetComponent<Button>().onClick.AddListener(PlayButton_OnClick);
		}

		static void PlayButton_OnClick()
		{
			Randomiser.Randomise();
			info.currentLevel = Randomiser.NextLevel();
			Manager.Instance().Play();
		}

		public static void LevelSelectPostfix(GameObject ___pauseMenu)
		{
			___pauseMenu.transform.Find("levels").gameObject.SetActive(false); // remove the level select button
			___pauseMenu.transform.Find("GhostHandler_Pause").gameObject.SetActive(false); // remove ghosts because they for sure mess with the randomiser in ways I don't want to deal with
		}
	}

	static class NextLevelButtonPatch
	{
		public static void Apply(Harmony harmony)
		{
			var keyDownOriginal = typeof(GameManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
			var isInLevelCompeteOriginal = typeof(steam_WorkshopHandler).GetMethod(nameof(steam_WorkshopHandler.UploadScoreToLeaderBoard));
			var isOutOfLevelCompleteOriginal = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

			var keyDownPatch = typeof(NextLevelButtonPatch).GetMethod(nameof(KeyDownPostfix));
			var isInLevelCompletePatch = typeof(NextLevelButtonPatch).GetMethod(nameof(PauseSplitPostfix));
			var isOutOfLevelCompletePatch = typeof(NextLevelButtonPatch).GetMethod(nameof(unpauseStartPrefix));

			harmony.Patch(keyDownOriginal, postfix: new HarmonyMethod(keyDownPatch));
			harmony.Patch(isInLevelCompeteOriginal, postfix: new HarmonyMethod(isInLevelCompletePatch));
			harmony.Patch(isOutOfLevelCompleteOriginal, prefix: new HarmonyMethod(isOutOfLevelCompletePatch));
		}

		public static void KeyDownPostfix(GameManager __instance)
		{
			if (Patcher.isInLevelComplete && !Patcher.nextLevelPressed && Input.GetKeyDown(KeyCode.Space))
			{
				Patcher.nextLevelPressed = true; // this needs to be set to prevent calling NextLevel() multiple times if spamming space, which crashes the game.
				__instance.NextLevel();	
			}
		}

		public static void PauseSplitPostfix()
		{
			Patcher.isInLevelComplete = true;
		}

		public static void unpauseStartPrefix(player __instance)
		{
			if (__instance.framesSinceStart == 0)
			{
				Patcher.isInLevelComplete = false;
				Patcher.nextLevelPressed = false;
			}
		}
	}

	static class HiddenAbilityPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(AbilitySelector).GetMethod(nameof(AbilitySelector.SelectAbility));

			var patch = typeof(HiddenAbilityPatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix(AbilitySelector __instance)
		{
			if (Patcher.EnableSurfingShoes)
			{ 
				__instance.movementManager.myAbility = __instance.a_powerLegs;
				__instance.movementManager.Activate();
			}
			if (Patcher.EnableTruckCannon)
			{
				__instance.utilityManager.myAbility = __instance.truckCannon;
				__instance.utilityManager.Activate();
			}
		}
	}

	static class ConfineCursorPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(ConfineCursorPatch).GetMethod(nameof(Prefix));

			harmony.Patch(original, prefix: new HarmonyMethod(patch));
		}

		public static void Prefix()
		{
			if (Cursor.lockState == CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.Confined;
			}
		}
	}

	static class TimerFixPatch
	{
		public static void Apply(Harmony harmony)
		{
			var StopOriginal = typeof(steam_WorkshopHandler).GetMethod(nameof(steam_WorkshopHandler.UploadScoreToLeaderBoard));
			var StartOriginal = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
			var TimerUpdateOriginal = typeof(GameManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

			var StopPatch = typeof(TimerFixPatch).GetMethod(nameof(StopPostfix));
			var startPatch = typeof(TimerFixPatch).GetMethod(nameof(StartPrefix));
			var TimeUpdatePatch = typeof(TimerFixPatch).GetMethod(nameof(TimeUpdatePrefix));

			harmony.Patch(StopOriginal, postfix: new HarmonyMethod(StopPatch));
			harmony.Patch(StartOriginal, prefix: new HarmonyMethod(startPatch));
			harmony.Patch(TimerUpdateOriginal, prefix: new HarmonyMethod(TimeUpdatePatch));
		}

		public static void StopPostfix()
		{
			Patcher.stopwatch.Stop();
		}

		public static void StartPrefix(player __instance)
		{
			if (__instance.framesSinceStart == 0f)
			{
				Patcher.stopwatch.Reset();
				Patcher.stopwatch.Start();
			}
		}

		public static void TimeUpdatePrefix(PlayerClock ___PlayerClock)
		{
			if (Patcher.EnableTimerFix)
			{
				if (Patcher.stopwatch.IsRunning)
				{
					___PlayerClock.SetTimeText("this literally doesn't matter it'll get replaced anyway lol");
				}
			}
		}
	}

	static class MenuTitlePatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(menuBar).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(MenuTitlePatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix(menuBar __instance)
		{
			Transform parent = __instance.leftMask.Find("Main"); 
			Text playButtonText = parent.Find("play").Find("Text").GetComponent<Text>();
			GameObject title = new GameObject("SpeedrunmodTitle");
			Text titleText = title.AddComponent<Text>();
			titleText.text = $"Clustertruck Speedrun Mod v{Patcher.version}";
			titleText.font = playButtonText.font;
			titleText.color = playButtonText.color;
			titleText.fontSize = 32;
			titleText.alignment = TextAnchor.MiddleCenter;
			RectTransform titleRT = title.GetComponent<RectTransform>();
			titleRT.SetParent(parent);
			titleRT.sizeDelta = new Vector2(1000f, 40f);
			titleRT.localScale = new Vector3(0.661255f, 0.661255f, 0.661255f);
			titleRT.localPosition = new Vector3(0f, 270f, 0f);
		}
	}

	static class AssignPlayer // Required for speedometer
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(player).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(AssignPlayer).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix(player __instance)
		{
			Patcher.playRig = __instance.rig;
		}
	}

	static class TimerPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(PlayerClock).GetMethod(nameof(PlayerClock.SetTimeText));

			var patch = typeof(TimerPatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix(ref Text ____TimeText, ref Text ____NameText, string val)
		{
			____TimeText.alignment = TextAnchor.LowerRight;
			Patcher.avgFPS += Time.unscaledDeltaTime;
			if (Patcher.FPSinterval == 0)
			{
				Patcher.prevFPS = (1 / (Patcher.avgFPS / 50)).ToString("0");
				Patcher.avgFPS = 0;
			}
			Patcher.FPSinterval += 1;
			if (Patcher.FPSinterval == 50)
			{
				Patcher.FPSinterval = 0;
			}

			string velocity = null;

			switch (Patcher.SpeedUnit)
			{
				case 1:
					velocity = $"{(Patcher.playRig.velocity.magnitude * 3.6f).ToString("0")}km/h";
					break;
				case 2:
					velocity = $"{(Patcher.playRig.velocity.magnitude * 2.236936f).ToString("0")}mph";
					break;
				default:
					velocity = $"{Patcher.playRig.velocity.magnitude.ToString("0")}m/s";
					break;
			}

			if (Patcher.EnableTimerFix)
			{
				TimeSpan ts = Patcher.stopwatch.Elapsed;

				if (ts.Hours > 0)
				{
					val = string.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
				}
				if (ts.Minutes > 0)
				{
					val = string.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);
				}
				else
				{
					val = string.Format("{0:00}.{1:000}", ts.Seconds, ts.Milliseconds);
				}
			}

			string m = info.abilityName;
			if (m == String.Empty) { m = "None"; }
			string u = info.utilityName;
			if (u == String.Empty) { u = "None"; }

			____NameText.text = String.Empty;
			____TimeText.text = $"{ (Patcher.EnableRandomiser ? $"Randomiser\n{m}/{u}\n{Randomiser.currentLevel + 1}/90\n{Randomiser.seed}\n\n" : "") }{ (Patcher.DisableJump ? "Jumpless\n\n" : "") }{ (Patcher.EnableSpeedometer ? $"{velocity}\n\n" : "") }{ (Patcher.EnableFPSCounter ? $"{Patcher.prevFPS}fps\n" : "") }{ val }";
		}
	}

	static class TruckColorPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(LandfallTwitch).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(TruckColorPatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}
		
		public static void Postfix(ref Material ___mTruckMaterial)
		{
			___mTruckMaterial.color = Patcher.TruckColor;
		}
	}

	static class FPSPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(Manager).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(FPSPatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix()
		{
			Application.targetFrameRate = Patcher.TargetFramerate;
		}
	}

	static class JumplessPatch
	{
		public static void Apply(Harmony harmony)
		{
			var origami = typeof(player).GetMethod("JumpRayHit", BindingFlags.NonPublic | BindingFlags.Instance);
			var orange  = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

			var origamiPatch = typeof(JumplessPatch).GetMethod(nameof(Prefix));
			var orangePatch  = typeof(JumplessPatch).GetMethod(nameof(Transpiler));

			harmony.Patch(origami, prefix: new HarmonyMethod(origamiPatch));
			harmony.Patch(orange, transpiler: new HarmonyMethod(orangePatch));
		}

		public static void Prefix(ref bool ___canForward)
		{
			___canForward = false;
			return;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "Jump")
				{
					codes[i].operand = "Unbound";
				}
			}

			return codes.AsEnumerable();
		}
	}

	static class SprintPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(player).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(SprintPatch).GetMethod(nameof(Transpiler));

			harmony.Patch(original, transpiler: new HarmonyMethod(patch));
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "Sprint")
				{
					codes[i + 2].opcode = OpCodes.Brfalse_S;
					break;
				}
			}

			return codes.AsEnumerable();
		}
	}

	static class ShowTimerPatch
	{
		public static void Apply(Harmony harmony)
		{
			var original = typeof(GameManager).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

			var patch = typeof(ShowTimerPatch).GetMethod(nameof(Postfix));

			harmony.Patch(original, postfix: new HarmonyMethod(patch));
		}

		public static void Postfix(ref PlayerClock ___PlayerClock)
		{
			info.ShowClock = true;
		}
	}
}
