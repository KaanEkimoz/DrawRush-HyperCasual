#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Makes the exported Xcode project match the App Store privacy label ("no tracking") and skips the
/// export-compliance question on every upload.
///
/// The game never shows the App Tracking Transparency prompt, so:
///   • the ATT usage string must not be in Info.plist (App Store Connect refuses a "no tracking" label
///     while the binary still carries it), and
///   • the merged privacy manifest must say NSPrivacyTracking = false with no tracking domains — a
///     tracking manifest makes iOS block those ad domains for every user who never answered ATT.
///
/// ITSAppUsesNonExemptEncryption = false: only standard HTTPS, which is exempt.
/// Runs last so other post-processors (LevelPlay, EDM4U) cannot add the keys back.
/// </summary>
public static class IosBuildPostProcess
{
    [PostProcessBuild(int.MaxValue)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        if (!File.Exists(plistPath))
        {
            Debug.LogWarning("[IosBuildPostProcess] Info.plist not found at " + plistPath);
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict root = plist.root;
        root.values.Remove("NSUserTrackingUsageDescription");
        root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        root.SetBoolean("UIRequiresFullScreen", true);
        plist.WriteToFile(plistPath);

        string privacyPath = Path.Combine(pathToBuiltProject, "UnityFramework", "PrivacyInfo.xcprivacy");
        if (File.Exists(privacyPath))
        {
            var privacy = new PlistDocument();
            privacy.ReadFromFile(privacyPath);
            privacy.root.SetBoolean("NSPrivacyTracking", false);
            privacy.root.values.Remove("NSPrivacyTrackingDomains");
            privacy.WriteToFile(privacyPath);
        }

        Debug.Log("[IosBuildPostProcess] Info.plist + privacy manifest patched (no ATT, no tracking, exempt encryption).");
    }
}
#endif
