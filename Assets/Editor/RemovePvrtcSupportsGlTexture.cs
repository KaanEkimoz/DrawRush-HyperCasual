#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Unity 6 keeps emitting <c>&lt;supports-gl-texture GL_IMG_texture_compression_pvrtc/&gt;</c> into the
/// generated Android manifest — it is driven by a stuck, deprecated <c>androidBuildSubtarget</c> value
/// that reverts to PVRTC after every build no matter what the texture compression settings say.
///
/// That single tag tells Google Play the app only supports the PowerVR texture format, so Play filters
/// it out for every Mali / Adreno device — i.e. nearly every Android phone (Samsung, Pixel, etc.), which
/// is why real devices saw "your device isn't compatible with this version."
///
/// This post-processor runs after Unity exports the Gradle project but before Gradle merges/assembles the
/// bundle, and strips the PVRTC <c>supports-gl-texture</c> line from every generated AndroidManifest.xml.
/// With the tag gone, Play applies no texture-format device filter and the app installs everywhere.
/// </summary>
public sealed class RemovePvrtcSupportsGlTexture : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        foreach (var manifest in Directory.GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(manifest);
            string cleaned = Regex.Replace(
                text,
                @"[ \t]*<supports-gl-texture[^>]*pvrtc[^>]*/>\s*(\r?\n)?",
                string.Empty,
                RegexOptions.IgnoreCase);

            if (cleaned != text)
            {
                File.WriteAllText(manifest, cleaned);
                Debug.Log("[RemovePvrtcSupportsGlTexture] Stripped PVRTC supports-gl-texture from " + manifest);
            }
        }
    }
}
#endif
