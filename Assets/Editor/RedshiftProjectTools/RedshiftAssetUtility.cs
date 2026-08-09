using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Redshift.EditorTools
{
    internal static class RedshiftAssetUtility
    {
        private static readonly HashSet<string> TextureExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".tga", ".psd",
                ".exr", ".hdr", ".tif", ".tiff"
            };

        private static readonly HashSet<string> ModelExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".fbx", ".obj", ".blend"
            };

        private static readonly HashSet<string> AudioExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".wav", ".mp3", ".ogg", ".aif", ".aiff"
            };

        private static readonly HashSet<string> AnimationExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".anim", ".controller", ".overrideController", ".mask"
            };

        private static readonly HashSet<string> ConservativeUnusedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".prefab", ".mat",
                ".png", ".jpg", ".jpeg", ".tga", ".psd",
                ".exr", ".hdr", ".tif", ".tiff",
                ".fbx", ".obj", ".blend",
                ".anim", ".controller", ".overrideController", ".mask",
                ".wav", ".mp3", ".ogg", ".aif", ".aiff"
            };

        public static string[] GetProjectAssetPaths()
        {
            return AssetDatabase
                .GetAllAssetPaths()
                .Where(IsProjectAssetFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool IsProjectAssetFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !AssetDatabase.IsValidFolder(path)
                && !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsUnderAnyFolder(
            string path,
            IEnumerable<string> folders)
        {
            foreach (string rawFolder in folders)
            {
                if (string.IsNullOrWhiteSpace(rawFolder))
                {
                    continue;
                }

                string folder = NormalizeAssetPath(rawFolder).TrimEnd('/');

                if (path.Equals(folder, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(
                        folder + "/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsConservativeUnusedCandidateType(string path)
        {
            return ConservativeUnusedExtensions.Contains(
                Path.GetExtension(path));
        }

        public static string GetCategory(string path)
        {
            string extension = Path.GetExtension(path);

            if (TextureExtensions.Contains(extension))
            {
                return "Texture";
            }

            if (ModelExtensions.Contains(extension))
            {
                return "Model";
            }

            if (AudioExtensions.Contains(extension))
            {
                return "Audio";
            }

            if (AnimationExtensions.Contains(extension))
            {
                return "Animation";
            }

            if (extension.Equals(".mat", StringComparison.OrdinalIgnoreCase))
            {
                return "Material";
            }

            if (extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return "Prefab";
            }

            if (extension.Equals(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return "Scene";
            }

            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return "Script";
            }

            return "Other";
        }

        public static long GetSourceFileSize(string assetPath)
        {
            try
            {
                string fullPath = Path.GetFullPath(assetPath);
                return File.Exists(fullPath)
                    ? new FileInfo(fullPath).Length
                    : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        public static Object LoadMainAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        public static void PingAsset(string assetPath)
        {
            Object asset = LoadMainAsset(assetPath);

            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        public static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }

        public static bool EnsureAssetFolder(string folderPath)
        {
            folderPath = NormalizeAssetPath(folderPath).TrimEnd('/');

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            if (!folderPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] parts = folderPath.Split('/');

            if (parts.Length == 0
                || !parts[0].Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string current = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, parts[i]);

                    if (string.IsNullOrEmpty(guid))
                    {
                        return false;
                    }
                }

                current = next;
            }

            return true;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + " B";
            }

            double value = bytes;
            string[] suffixes = { "KB", "MB", "GB", "TB" };
            int suffixIndex = -1;

            do
            {
                value /= 1024d;
                suffixIndex++;
            }
            while (value >= 1024d && suffixIndex < suffixes.Length - 1);

            return value.ToString("0.##") + " " + suffixes[suffixIndex];
        }

        public static bool IsTexture(string path)
        {
            return TextureExtensions.Contains(Path.GetExtension(path));
        }

        public static bool IsModel(string path)
        {
            return ModelExtensions.Contains(Path.GetExtension(path));
        }

        public static bool IsAudio(string path)
        {
            return AudioExtensions.Contains(Path.GetExtension(path));
        }

        public static bool IsAnimation(string path)
        {
            return AnimationExtensions.Contains(Path.GetExtension(path));
        }
    }
}
