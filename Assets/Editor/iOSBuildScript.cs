#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.Management;

/// <summary>
/// Headless build script for automated iOS cloud builds.
/// Configures iOS PlayerSettings, enables ARKit XR plug-in provider,
/// and exports the Unity project into an Xcode project at build/iOS.
/// </summary>
public static class iOSBuildScript
{
    public static void ConfigureProject()
    {
        Debug.Log("[iOSBuildScript] Configuring iOS Project & XR ARKit Settings...");

        // 1. Switch Active Build Target to iOS
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        // 2. Configure PlayerSettings for iOS
        PlayerSettings.iOS.cameraUsageDescription = "Camera is required for AR surface tracking and paper toss gameplay.";
        PlayerSettings.SetArchitecture(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), 1); // 1 = ARM64
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ScriptingImplementation.IL2CPP);

        // 3. Initialize & Configure XR Plug-in Management for iOS
        var buildTargetSettings = XRGeneralSettingsPerBuildTarget.GetOrCreate();
        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
        {
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
        }

        var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.iOS);
        if (generalSettings != null && generalSettings.Manager != null)
        {
            generalSettings.InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(generalSettings.Manager, typeof(ARKitLoader).FullName, BuildTargetGroup.iOS);
            EditorUtility.SetDirty(generalSettings);
            EditorUtility.SetDirty(generalSettings.Manager);
        }

        // 4. Configure ARKit Settings
        var arkitSettings = ARKitSettings.GetOrCreateSettings();
        if (arkitSettings != null)
        {
            arkitSettings.requirement = ARKitSettings.Requirement.Optional;
            EditorUtility.SetDirty(arkitSettings);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[iOSBuildScript] Configuration completed successfully.");
    }

    public static void Build()
    {
        ConfigureProject();

        Debug.Log("[iOSBuildScript] Starting iOS build export...");

        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "build/iOS";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[iOSBuildScript] Build failed with result: {report.summary.result}, errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"[iOSBuildScript] Build succeeded! Output at: {buildPath}, Total time: {report.summary.totalTime}");
            EditorApplication.Exit(0);
        }
    }
}
#endif
