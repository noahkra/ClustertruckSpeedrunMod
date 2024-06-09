using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;

namespace ClustertruckSpeedrunModLib
{
	public static class Autosplitter
	{
		static TcpClient client;
		static NetworkStream stream;

		public static void Connect()
		{
			client = new TcpClient("localhost", 16834);
			stream = client.GetStream();

			//// Should probably put this somewhere..?
			// if (stream != null) { stream.Close(); }
			// if (client != null) { client.Close(); }
		}

		static void SendMessage(string message)
		{
			byte[] data = Encoding.ASCII.GetBytes(message);
			stream.Write(data, 0, data.Length);
		}
	}

	public static class Patcher
	{
		public static Rigidbody playRig;
		public static int FPSinterval;
		public static string prevFPS;
		public static float avgFPS;

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

		public static void DoPatching(
			bool _enableSpeedometer, int _speedUnit, 
			float _truckColorR, float _truckColorG, float _truckColorB, 
			int _targetFramerate, bool _enableFPSCounter, bool _disableJump, 
			bool _invertSprint, bool _enableTimer, bool _enableLivesplit, 
			bool _splitByLevel, bool _splitResetInMenu)
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

			try
			{
				var harmony = new Harmony("com.clustertruckspeedrun.mod");

#if DEBUG
				Harmony.DEBUG = true;
#endif

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

				Console.WriteLine("[SPEEDRUNMOD] All patches applied successfully!");
			} 
			catch (Exception ex)
			{
				Console.WriteLine($"[SPEEDRUNMOD] Patching Failed: {ex}");
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
			titleText.text = "Clustertruck Speedrun Mod v1.0.0";
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

		public static void Postfix(PlayerClock __instance, ref Text ____TimeText, ref Text ____NameText, string val)
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

			string velocity;

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
