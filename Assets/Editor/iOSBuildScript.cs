#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless build script for automated iOS cloud builds.
/// Exports the Unity project into an Xcode project at build/iOS.
/// </summary>
public static class iOSBuildScript
{
    public static void Build()
    {
        Debug.Log("[iOSBuildScript] Starting iOS build export...");

        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "build/iOS";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
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
