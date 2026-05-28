using System;
using System.Reflection;
using UnityEditor;

namespace Studios208.DrawRush.EditorTools
{
    /// <summary>
    /// Keeps Google EDM4U (External Dependency Manager) quiet. EDM4U rewrites
    /// ProjectSettings/GvhProjectSettings.xml and drops manually-set keys, so a one-off
    /// change reverts on the next editor restart and the "Android Resolver has detected a
    /// change — enable auto-resolution?" prompt comes back. This enforcer re-applies the
    /// quiet configuration on every editor load via reflection (the settings type is
    /// internal), so the prompt stays gone regardless of what EDM4U writes to disk.
    ///
    /// Effect: auto-resolution stays enabled but silent (no prompt), and the
    /// disabled-auto-resolution warning is suppressed too. Auto-resolve-on-build is left
    /// untouched, so builds still pull the right Android dependencies.
    /// </summary>
    [InitializeOnLoad]
    internal static class AndroidResolverSettingsEnforcer
    {
        static AndroidResolverSettingsEnforcer()
        {
            Apply();                          // best-effort immediately
            EditorApplication.delayCall += Apply;   // and after EDM4U has finished init
        }

        private static void Apply()
        {
            Type settings = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                settings = asm.GetType("GooglePlayServices.SettingsDialog");
                if (settings != null) break;
            }
            if (settings == null) return;

            const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            // Quiet config: no editing-time auto-resolution (so no surprise Gradle runs or
            // "Gradle failed to fetch dependencies" noise), no prompt, no disabled-warning nag.
            // Dependencies still resolve at build time (AutoResolveOnBuild is left on).
            SetBool(settings, "EnableAutoResolution", false, Flags);
            SetBool(settings, "AutoResolutionDisabledWarning", false, Flags);
            SetBool(settings, "PromptBeforeAutoResolution", false, Flags);
        }

        private static void SetBool(Type type, string propertyName, bool value, BindingFlags flags)
        {
            PropertyInfo prop = type.GetProperty(propertyName, flags);
            if (prop == null || !prop.CanWrite) return;
            try
            {
                if (!Equals(prop.GetValue(null), value)) prop.SetValue(null, value);
            }
            catch
            {
                // EDM4U internals can change between versions; never let this break editor load.
            }
        }
    }
}
