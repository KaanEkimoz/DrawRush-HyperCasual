using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line entry point for the iOS Xcode export, so a release can be built without clicking
/// through Build Profiles:
///   Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod IosBuild.Build
/// Output goes to Builds/iOS (append mode keeps Pods and Xcode signing edits between builds).
/// </summary>
public static class IosBuild
{
    private const string OutputPath = "Builds/iOS";

    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildPipeline.BuildCanBeAppended(BuildTarget.iOS, OutputPath) == CanAppendBuild.Yes
                ? BuildOptions.AcceptExternalModificationsToPlayer
                : BuildOptions.None,
        };

        Debug.Log($"[IosBuild] {PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS)} " +
                  $"{PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber}), team {PlayerSettings.iOS.appleDeveloperTeamID}, " +
                  $"scenes: {string.Join(", ", scenes)}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[IosBuild] Result: {report.summary.result}, errors {report.summary.totalErrors}, " +
                  $"size {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
