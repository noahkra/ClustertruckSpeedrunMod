using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO.Pipes;
using System.IO;
using System.Diagnostics;

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

	public static class Patcher
	{
		readonly public static string version = "1.1.0";

		public static Rigidbody playRig = null;
		public static int FPSinterval;
		public static string prevFPS = null;
		public static float avgFPS;
		public static Stopwatch stopwatch = new Stopwatch();

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
		public static bool CursorDeathLock;
		public static bool EnableTimerFix;

		public static void DoPatching(
			bool _enableSpeedometer, int _speedUnit,
			float _truckColorR, float _truckColorG, float _truckColorB,
			int _targetFramerate, bool _enableFPSCounter, bool _disableJump,
			bool _invertSprint, bool _enableTimer, bool _enableLivesplit,
			bool _splitByLevel, bool _splitResetInMenu, bool _cursorDeathLock,
			bool _enableTimerFix)
		{
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
			CursorDeathLock = _cursorDeathLock;
			EnableTimerFix = _enableTimerFix;

			try
			{
				var harmony = new Harmony("com.clustertruckspeedrun.mod");

#if DEBUG
				Harmony.DEBUG = true;
#endif

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

				Console.WriteLine("[SPEEDRUNMOD] All patches applied successfully!");
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

			var pauseSplitPatch = typeof(LivesplitPatch).GetMethod(nameof(PauseSplitPostfix));
			var unpauseStartPatch = typeof(LivesplitPatch).GetMethod(nameof(unpauseStartPrefix));
			var menuResetPatch = typeof(LivesplitPatch).GetMethod(nameof(MenuResetPostfix));

			harmony.Patch(pauseSplitOriginal, postfix: new HarmonyMethod(pauseSplitPatch));
			harmony.Patch(unpauseStartOriginal, prefix: new HarmonyMethod(unpauseStartPatch));
			harmony.Patch(menuResetOriginal, postfix: new HarmonyMethod(menuResetPatch));
		}

		public static void PauseSplitPostfix()
		{
			Autosplitter.PauseGameTime();

			// Only split if splitting by level or if first level of the world
			if (Patcher.SplitByLevel || info.currentLevel % 10 == 0)
			{
				Autosplitter.Split();
			}
		}

		public static void unpauseStartPrefix(player __instance)
		{
			if (__instance.framesSinceStart == 0)
			{
				if (info.currentLevel % 10 == 1)
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
			Text playButtonText = __instance.leftMask.Find("Main").Find("play").Find("Text").GetComponent<Text>();
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
			
			____NameText.text = String.Empty;
			____TimeText.text = $"{ (Patcher.DisableJump ? "Jumpless\n\n" : "") }{ (Patcher.EnableSpeedometer ? $"{velocity}\n\n" : "") }{ (Patcher.EnableFPSCounter ? $"{Patcher.prevFPS}fps\n" : "") }{ val }";
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
