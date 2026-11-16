using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line entry points for the iOS Xcode export, so a release can be built without clicking
/// through Build Profiles:
///   Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod IosBuild.Build
///   Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod IosBuild.BuildSimulator
/// Build writes Builds/iOS (append mode keeps Pods and Xcode signing edits between builds).
/// BuildSimulator writes Builds/iOS-Simulator for the iOS Simulator and puts the device SDK back
/// afterwards, so the next release build is not accidentally a simulator one.
/// </summary>
public static class IosBuild
{
    private const string OutputPath = "Builds/iOS";
    private const string SimulatorOutputPath = "Builds/iOS-Simulator";

    public static void Build() => Finish(Run(OutputPath));

    public static void BuildSimulator()
    {
        iOSSdkVersion previous = PlayerSettings.iOS.sdkVersion;
        AppleMobileArchitectureSimulator previousArch = PlayerSettings.iOS.simulatorSdkArchitecture;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64; // Apple-silicon simulators run no x86_64 apps
        bool ok;
        try { ok = Run(SimulatorOutputPath); }
        finally
        {
            PlayerSettings.iOS.sdkVersion = previous;
            PlayerSettings.iOS.simulatorSdkArchitecture = previousArch;
            AssetDatabase.SaveAssets();
        }
        Finish(ok);
    }

    private static void Finish(bool ok)
    {
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool Run(string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildPipeline.BuildCanBeAppended(BuildTarget.iOS, outputPath) == CanAppendBuild.Yes
                ? BuildOptions.AcceptExternalModificationsToPlayer
                : BuildOptions.None,
        };

        Debug.Log($"[IosBuild] {PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS)} " +
                  $"{PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber}), team {PlayerSettings.iOS.appleDeveloperTeamID}, " +
                  $"sdk {PlayerSettings.iOS.sdkVersion}, scenes: {string.Join(", ", scenes)}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[IosBuild] Result: {report.summary.result}, errors {report.summary.totalErrors}, " +
                  $"size {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
        return report.summary.result == BuildResult.Succeeded;
    }
}
