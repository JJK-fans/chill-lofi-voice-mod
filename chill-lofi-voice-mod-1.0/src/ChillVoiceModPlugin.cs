using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace JJK.ChillVoiceMod
{
    [BepInPlugin("com.jjk.chillvoicemod", "Chill Voice Mod", "2.5.0")]
    public class ChillVoiceModPlugin : BaseUnityPlugin
    {
        private static ManualLogSource s_logger;
        private static Dictionary<string, AudioClip> s_customClips;
        private static Harmony s_harmony;
        private static HashSet<string> s_loggedKeys = new HashSet<string>();
        private static int s_replacedCount;

        private void Awake()
        {
            s_logger = Logger;
            s_logger.LogInfo("[VoiceMod] v2.5.0 initializing (debug key format)...");

            string modFolder = Path.Combine(Paths.PluginPath, "ChillVoiceMod");
            string bundlePath = Path.Combine(modFolder, "voice_assets_all");

            if (!File.Exists(bundlePath))
            {
                s_logger.LogError($"[VoiceMod] Bundle not found: {bundlePath}");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                s_logger.LogError("[VoiceMod] Failed to load custom bundle");
                return;
            }

            var allClips = bundle.LoadAllAssets<AudioClip>();
            s_customClips = new Dictionary<string, AudioClip>();
            foreach (var clip in allClips)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.name))
                {
                    clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    s_customClips[clip.name] = clip;
                }
            }
            s_logger.LogInfo($"[VoiceMod] Loaded {s_customClips.Count} custom AudioClips");

            // Log first 5 custom clip names for comparison
            int n = 0;
            foreach (var key in s_customClips.Keys)
            {
                s_logger.LogInfo($"[VoiceMod]   Custom clip[{n}]: '{key}'");
                if (++n >= 5) break;
            }

            // Harmony-patch Play to log audioPath AND inject our clip
            ApplyHarmonyPatch();
        }

        private void ApplyHarmonyPatch()
        {
            Type vmType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                vmType = asm.GetType("KanKikuchi.AudioManager.VoiceManager");
                if (vmType != null) break;
            }

            if (vmType == null)
            {
                s_logger.LogError("[VoiceMod] VoiceManager type not found");
                return;
            }

            s_harmony = new Harmony("com.jjk.chillvoicemod.harmony");

            // Find the Play method
            foreach (var method in vmType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name == "Play")
                {
                    s_logger.LogInfo($"[VoiceMod] Patching: {method}");
                    var prefix = typeof(ChillVoiceModPlugin).GetMethod(nameof(PlayPrefix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var postfix = typeof(ChillVoiceModPlugin).GetMethod(nameof(PlayPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    s_harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                    break;
                }
            }

            // Also try to pre-populate _audioClipDict
            StartCoroutine(PopulateDictWhenReady(vmType));
        }

        private System.Collections.IEnumerator PopulateDictWhenReady(Type vmType)
        {
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                var instanceProp = vmType.BaseType?.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (instanceProp == null) continue;

                object instance = instanceProp.GetValue(null);
                if (instance == null) continue;

                Type currentType = vmType;
                while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(object))
                {
                    var dictField = currentType.GetField("_audioClipDict",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (dictField != null)
                    {
                        var dict = dictField.GetValue(instance) as IDictionary;
                        if (dict != null)
                        {
                            // Add custom clips using clip.name as key
                            foreach (var kvp in s_customClips)
                            {
                                if (!dict.Contains(kvp.Key))
                                {
                                    dict.Add(kvp.Key, kvp.Value);
                                    s_replacedCount++;
                                }
                            }
                            s_logger.LogInfo($"[VoiceMod] Pre-populated {s_replacedCount} clips into _audioClipDict");
                            yield break;
                        }
                    }
                    currentType = currentType.BaseType;
                }
            }
            s_logger.LogWarning("[VoiceMod] Could not populate _audioClipDict after 120 frames");
        }

        /// <summary>
        /// Prefix: Log the audioPath parameter and try to inject our clip.
        /// The Play signature is: Play(string audioPath, float volumeRate, float delay, float pitch, bool isLoop, Action callback, string key, Transform sourceTransform)
        /// </summary>
        private static void PlayPrefix(string audioPath, object __instance)
        {
            if (string.IsNullOrEmpty(audioPath)) return;

            // Log first 10 unique audioPath values to understand key format
            if (s_loggedKeys.Count < 10 && !s_loggedKeys.Contains(audioPath))
            {
                s_loggedKeys.Add(audioPath);
                s_logger.LogInfo($"[VoiceMod] Play called with audioPath[{s_loggedKeys.Count-1}]: '{audioPath}'");
            }

            // Try to inject our clip into _audioClipDict with this audioPath as key
            if (s_customClips != null && __instance != null)
            {
                InjectClipForPath(__instance, audioPath);
            }
        }

        private static void PlayPostfix(object __result, string audioPath)
        {
            // If Play returned null, the clip wasn't found
            if (__result == null && !string.IsNullOrEmpty(audioPath))
            {
                s_logger.LogWarning($"[VoiceMod] Play returned NULL for: '{audioPath}'");
            }
        }

        private static void InjectClipForPath(object instance, string audioPath)
        {
            // Find matching custom clip
            AudioClip customClip = FindMatchingClip(audioPath);
            if (customClip == null) return;

            Type currentType = instance.GetType();
            while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(object))
            {
                var dictField = currentType.GetField("_audioClipDict",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (dictField != null)
                {
                    var dict = dictField.GetValue(instance) as IDictionary;
                    if (dict != null)
                    {
                        if (!dict.Contains(audioPath))
                        {
                            dict[audioPath] = customClip;
                            s_replacedCount++;
                            if (s_replacedCount % 50 == 0)
                                s_logger.LogInfo($"[VoiceMod] Injected {s_replacedCount} clips on-demand (last: '{audioPath}')");
                        }
                        else
                        {
                            // Replace existing entry
                            dict[audioPath] = customClip;
                        }
                    }
                }
                currentType = currentType.BaseType;
            }
        }

        private static AudioClip FindMatchingClip(string audioPath)
        {
            if (string.IsNullOrEmpty(audioPath)) return null;

            // Direct match by full path (with .wav extension)
            if (s_customClips.TryGetValue(audioPath, out AudioClip clip))
                return clip;

            // Match by file name without extension
            string stem = Path.GetFileNameWithoutExtension(audioPath);
            if (!string.IsNullOrEmpty(stem) && s_customClips.TryGetValue(stem, out clip))
                return clip;

            // Try with .ogg extension (game might use .ogg in path)
            string oggPath = audioPath.Replace(".wav", ".ogg");
            if (s_customClips.TryGetValue(oggPath, out clip))
                return clip;

            // Case-insensitive search
            foreach (var kvp in s_customClips)
            {
                if (string.Equals(kvp.Key, audioPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, stem, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            // Partial match (e.g., audioPath contains the stem or vice versa)
            if (!string.IsNullOrEmpty(stem))
            {
                foreach (var kvp in s_customClips)
                {
                    if (kvp.Key.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        stem.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return kvp.Value;
                    }
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            s_logger.LogInfo($"[VoiceMod] v2.5.0 unloaded (total injected: {s_replacedCount} clips)");
            if (s_loggedKeys.Count > 0)
            {
                s_logger.LogInfo($"[VoiceMod] Sample audioPath keys seen:");
                foreach (var key in s_loggedKeys)
                    s_logger.LogInfo($"  '{key}'");
            }
            s_harmony?.UnpatchSelf();
        }
    }
}
