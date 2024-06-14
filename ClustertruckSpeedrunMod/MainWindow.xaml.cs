using System.Windows;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClustertruckSpeedrunMod
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			TargetFPS.ValueChanged += TargetFPS_ValueChanged;

			Loaded += (sender, e) => SetupProgressReset(this);

			LoadSettings();

			TargetFPSValue.Text = TargetFPS.Value.ToString("0");
		}

		private void SetupProgressReset(DependencyObject container)
		{
			int count = VisualTreeHelper.GetChildrenCount(container);

			for (int i = 0; i < count; i++)
			{
				var childNode = VisualTreeHelper.GetChild(container, i);

				if (childNode is CheckBox checkBox)
				{
					checkBox.Checked += (sender, e) => Progress(0, "");
					checkBox.Unchecked += (sender, e) => Progress(0, "");
				}

				if (childNode is RadioButton radioButton)
				{
					radioButton.Checked += (sender, e) => Progress(0, "");
					radioButton.Unchecked += (sender, e) => Progress(0, "");
				}

				SetupProgressReset(childNode);
			}
		}

		private void EnableLivesplit_CheckChanged(object sender, RoutedEventArgs e)
		{
			bool isChecked = (bool)((CheckBox)sender).IsChecked;
			SplitByLevel.IsEnabled = isChecked;
			SplitByWorld.IsEnabled = isChecked;
			SplitResetInMenu.IsEnabled = isChecked;
		}

		private void EnableTruckColor_CheckChanged(object sender, RoutedEventArgs e)
		{
			bool isChecked = (bool)((CheckBox)sender).IsChecked;
			TruckColor.IsEnabled = isChecked;
		}

		private void EnableSpeedometer_CheckChanged(object sender, RoutedEventArgs e)
		{
			bool isChecked = (bool)((CheckBox)sender).IsChecked;
			mps.IsEnabled = isChecked;
			kmph.IsEnabled = isChecked;
			mph.IsEnabled = isChecked;
		}

		private void UnlockFPS_CheckChanged(object sender, RoutedEventArgs e)
		{
			bool isChecked = (bool)((CheckBox)sender).IsChecked;
			TargetFPS.IsEnabled = isChecked;
		}

		private void TargetFPS_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			TargetFPSValue.Text = TargetFPS.Value.ToString("0");
			Progress(0, "");
		}

		int GetSpeedUnitInt()
		{
			if ((bool)mps.IsChecked) return 0;
			if ((bool)kmph.IsChecked) return 1;
			if ((bool)mph.IsChecked) return 2;
			return -1;
		}

		void SetSpeedUnitInt()
		{
			switch (Properties.Settings.Default.SpeedUnit)
			{
				case 0:
					mps.IsChecked = true;
					break;
				case 1:
					kmph.IsChecked = true;
					break;
				case 2:
					mph.IsChecked = true;
					break;
			}
		}

		void SaveSettings(object sender = null, RoutedEventArgs e = null)
		{
			Properties.Settings.Default.ClustertruckPath = FolderPath.Text;
			Properties.Settings.Default.EnableSpeedometer = (bool)EnableSpeedometer.IsChecked;
			Properties.Settings.Default.EnableTruckColor = (bool)EnableTruckColor.IsChecked;
			Properties.Settings.Default.TruckColor = TruckColor.Text;
			Properties.Settings.Default.UnlockFPS = (bool)UnlockFPS.IsChecked;
			Properties.Settings.Default.TargetFPS = (int)TargetFPS.Value;
			Properties.Settings.Default.EnableFPSCounter = (bool)EnableFPSCounter.IsChecked;
			Properties.Settings.Default.DisableJump = (bool)DisableJump.IsChecked;
			Properties.Settings.Default.InvertSprint = (bool)InvertSprint.IsChecked;
			Properties.Settings.Default.EnableTimer = (bool)EnableTimer.IsChecked;
			Properties.Settings.Default.SpeedUnit = GetSpeedUnitInt();
			Properties.Settings.Default.EnableLivesplit = (bool)EnableLivesplit.IsChecked;
			Properties.Settings.Default.SplitByLevel = (bool)SplitByLevel.IsChecked;
			Properties.Settings.Default.SplitResetInMenu = (bool)SplitResetInMenu.IsChecked;
			Properties.Settings.Default.ConfineCursor = (bool)ConfineCursor.IsChecked;
			Properties.Settings.Default.EnableTimerFix = (bool)EnableTimerFix.IsChecked;
			Properties.Settings.Default.Save();
		}

		void LoadSettings()
		{
			FolderPath.Text = Properties.Settings.Default.ClustertruckPath;
			EnableSpeedometer.IsChecked = Properties.Settings.Default.EnableSpeedometer;
			EnableTruckColor.IsChecked = Properties.Settings.Default.EnableTruckColor;
			TruckColor.Text = Properties.Settings.Default.TruckColor;
			UnlockFPS.IsChecked = Properties.Settings.Default.UnlockFPS;
			TargetFPS.Value = Properties.Settings.Default.TargetFPS;
			EnableFPSCounter.IsChecked = Properties.Settings.Default.EnableFPSCounter;
			DisableJump.IsChecked = Properties.Settings.Default.DisableJump;
			InvertSprint.IsChecked = Properties.Settings.Default.InvertSprint;
			EnableTimer.IsChecked = Properties.Settings.Default.EnableTimer;
			SetSpeedUnitInt();
			EnableLivesplit.IsChecked = Properties.Settings.Default.EnableLivesplit;
			SplitByLevel.IsChecked = Properties.Settings.Default.SplitByLevel;
			SplitResetInMenu.IsChecked = Properties.Settings.Default.SplitResetInMenu;
			ConfineCursor.IsChecked = Properties.Settings.Default.ConfineCursor;
			EnableTimerFix.IsChecked = Properties.Settings.Default.EnableTimerFix;
		}

		private void PatchButton_Click(object sender, RoutedEventArgs e)
		{
			Progress(0, "Starting...");
			AssemblyDefinition gameAssembly = null;
			AssemblyDefinition patchAssembly = null;

			try
			{
				Progress(5, "Setting up paths...");
				string gameAssemblyPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\Assembly-CSharp.dll";
				string gameAssemblyPathOriginal = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\Assembly-CSharp-ORIGINAL.dll";
				string localPatchPath = $"{Environment.CurrentDirectory}\\ClustertruckSpeedrunModLib.dll";
				string localHarmonyPath = $"{Environment.CurrentDirectory}\\0Harmony.dll";
				string gamePatchPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\ClustertruckSpeedrunModLib.dll";
				string gameHarmonyPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\0Harmony.dll";
				string unityEnginePath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\UnityEngine.dll";
				string unityEngineUIPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\UnityEngine.UI.dll";
				string localUnityEnginePath = $"{Environment.CurrentDirectory}\\UnityEngine.dll";
				string localUnityEngineUIPath = $"{Environment.CurrentDirectory}\\UnityEngine.UI.dll";


				Progress(10, "Backing up Assembly-CSharp.dll...");

				// Wrong folder?
				if (!File.Exists(gameAssemblyPath)) {
					throw new Exception("Clustertruck directory not found.\n\nAre you sure you selected the right folder?");
				}

				// Necessary dlls?
				if (!File.Exists(localPatchPath))
				{
					throw new Exception("ClustertruckSpeedrunModLib.dll not found.\n\nPlease make sure it is located in the execution directory.");
				}
				if (!File.Exists(localHarmonyPath))
				{
					throw new Exception("0Harmony.dll not found.\n\nPlease make sure it is located in the execution directory.");
				}

				// Do not judge me... I'm happy enough I got this working as is
				File.Copy(unityEnginePath, localUnityEnginePath, true);
				File.Copy(unityEngineUIPath, localUnityEngineUIPath, true);

				// Create a backup of the original Assembly-CSharp.dll. Patching will be done from here.
				if (!File.Exists(gameAssemblyPathOriginal))
				{
					File.Copy(gameAssemblyPath, gameAssemblyPathOriginal);
				}

				File.Copy(localPatchPath, gamePatchPath, true);
				File.Copy(localHarmonyPath, gameHarmonyPath, true);

				Progress(25, "Reading Assemblies...");

				gameAssembly = AssemblyDefinition.ReadAssembly(gameAssemblyPathOriginal);
				var mainModule = gameAssembly.MainModule;

				patchAssembly = AssemblyDefinition.ReadAssembly(gamePatchPath);
				mainModule.AssemblyReferences.Add(patchAssembly.Name);

				Progress(35, "Setting up Entrypoint...");
				TypeDefinition introHandlerType = mainModule.Types.FirstOrDefault(type => type.Name == "info");
				MethodDefinition entryPoint = introHandlerType.Methods.FirstOrDefault(m => m.Name == "Awake");

				// Ensure the entry point and instructions are not null
				if (entryPoint != null && entryPoint.Body != null && entryPoint.Body.Instructions.Count > 0)
				{
					// Inject code to load and execute the Harmony patches
					var ilProcessor = entryPoint.Body.GetILProcessor();
					var firstInstruction = entryPoint.Body.Instructions[0];

					Progress(40, "Injecting patches...");
					var patchType = patchAssembly.MainModule.Types.FirstOrDefault(t => t.FullName == "ClustertruckSpeedrunModLib.Patcher");
					var patchMethod = patchType.Methods.FirstOrDefault(m => m.Name == "DoPatching");

					var color = System.Drawing.ColorTranslator.FromHtml((bool)EnableTruckColor.IsChecked ? TruckColor.Text : "#FFFFFF");

					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(EnableSpeedometer.IsChecked))); // EnableSpeedometer
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, GetSpeedUnitInt())); // SpeedUnit
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_R4, (float)color.R / 255f)); // TruckColor.r
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_R4, (float)color.G / 255f)); // TruckColor.g
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_R4, (float)color.B / 255f)); // TruckColor.b
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, (bool)UnlockFPS.IsChecked ? (int)TargetFPS.Value : 90)); // TargetFramerate
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(EnableFPSCounter.IsChecked))); // EnableFPSCounter
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(DisableJump.IsChecked))); // DisableJump
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(InvertSprint.IsChecked))); // InvertSprint
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(EnableTimer.IsChecked))); // EnableTimer
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(EnableLivesplit.IsChecked))); // EnableLivesplit
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(SplitByLevel.IsChecked))); // SplitByLevel
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(SplitResetInMenu.IsChecked))); // SplitResetInMenu
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(ConfineCursor.IsChecked))); // ConfineCursor
					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(BoolToOpCode(EnableTimerFix.IsChecked))); // EnableTimerFix

					ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, mainModule.ImportReference(patchMethod)));


					Progress(95, "Writing modified assembly to disk...");
					gameAssembly.Write(gameAssemblyPath);

					Progress(100, "Assembly patched successfully! :D");
				} 
				else
				{
					MessageBox.Show("Error: Unable to find suitable injection point.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			} 
			catch (Exception ex)
			{
				if (ex.Message.Contains("user-mapped section open"))
				{
					MessageBox.Show($"Please close the game before trying to apply the patches!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else if(ex.Message.Contains("Parameter 'htmlColor'"))
				{
					MessageBox.Show($"Truck colour \"{TruckColor.Text}\" is not a valid colour.\n\nPlease use HTML HEX formatting (ex. #FF00FF). ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				Progress(0, "Patching interrupted :(");
			}
			finally
			{
				gameAssembly?.Dispose();
				patchAssembly?.Dispose();
			}
		}

		private void BrowseFolders_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CommonOpenFileDialog();
			dialog.IsFolderPicker = true;

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				FolderPath.Text = dialog.FileName;
			}
		}

		void Progress(int val,  string msg)
		{
			ProgressVal.Value = val;
			ProgressText.Text = msg;
		}

		OpCode BoolToOpCode(bool? a)
		{
			if (a == null)
			{
				return OpCodes.Ldc_I4_0;
			}
			return (bool)a ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
		}

		private void UnpatchButton_Click(object sender, RoutedEventArgs e)
		{
			Progress(0, "Starting...");
			try
			{
				Progress(5, "Setting up paths...");
				string gameAssemblyPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\Assembly-CSharp.dll";
				string gameAssemblyPathOriginal = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\Assembly-CSharp-ORIGINAL.dll";
				string gamePatchAssemblyPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\ClustertruckSpeedrunModLib.dll";
				string gameHarmonyPath = $"{FolderPath.Text}\\Clustertruck_Data\\Managed\\0Harmony.dll";

				Progress(40, "Restoring Assembly-CSharp.dll...");
				if (File.Exists(gameAssemblyPathOriginal))
				{
					File.Copy(gameAssemblyPathOriginal, gameAssemblyPath, true);
					File.Delete(gameAssemblyPathOriginal);
				}

				Progress(80, "Removing libraries...");
				if (File.Exists(gamePatchAssemblyPath))
				{
					File.Delete(gamePatchAssemblyPath);
				}
				if (File.Exists(gameHarmonyPath))
				{
					File.Delete(gameHarmonyPath);
				}

				Progress(100, "Game files restored!");

			} 
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			SaveSettings();
			base.OnClosing(e);
		}
	}
}