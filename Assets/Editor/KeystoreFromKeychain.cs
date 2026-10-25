using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fills the Android keystore + key alias passwords from the macOS Keychain whenever the editor
/// loads (and after every script reload), so a signed release build never needs them typed in.
/// Unity deliberately keeps these passwords in memory only; the Keychain stores them encrypted
/// outside the project, so nothing secret ever lands in a file or a commit.
///
/// One-time setup (Terminal, prompts for the password — never pass it on the command line):
///   security add-generic-password -s "DrawRush.keystore" -a "ekimozgames" -w
/// Optional, only if the alias password differs from the keystore password:
///   security add-generic-password -s "DrawRush.keyalias" -a "ekimozgames" -w
///
/// No Keychain entry (another machine, CI, Windows) → does nothing; type them in as before.
/// Never logs the password.
/// </summary>
[InitializeOnLoad]
public static class KeystoreFromKeychain
{
    private const string StoreService = "DrawRush.keystore";
    private const string AliasService = "DrawRush.keyalias";
    private const string SecurityTool = "/usr/bin/security";

    static KeystoreFromKeychain() => Apply();

    [MenuItem("DrawRush/Dev/Reload Keystore Passwords From Keychain")]
    private static void ApplyFromMenu()
    {
        bool ok = Apply(force: true);
        UnityEngine.Debug.Log(ok
            ? "[Keystore] Passwords loaded from the Keychain."
            : "[Keystore] No Keychain entry found (see KeystoreFromKeychain.cs for setup).");
    }

    private static bool Apply(bool force = false)
    {
        if (Application.platform != RuntimePlatform.OSXEditor) return false;
        if (!PlayerSettings.Android.useCustomKeystore) return false;
        if (!force && !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)) return true;

        string account = PlayerSettings.Android.keyaliasName;
        string storePass = Read(StoreService, account);
        if (string.IsNullOrEmpty(storePass)) return false;

        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasPass = Read(AliasService, account) ?? storePass;
        return true;
    }

    // `security` prints the secret on stdout with -w; a missing item exits non-zero.
    private static string Read(string service, string account)
    {
        try
        {
            var psi = new ProcessStartInfo(SecurityTool,
                $"find-generic-password -s \"{service}\" -a \"{account}\" -w")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process p = Process.Start(psi);
            string secret = p.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
            p.WaitForExit(3000);
            return p.ExitCode == 0 && secret.Length > 0 ? secret : null;
        }
        catch (System.Exception)
        {
            return null;   // no `security` tool, sandboxed editor, … — fall back to typing it in
        }
    }
}
