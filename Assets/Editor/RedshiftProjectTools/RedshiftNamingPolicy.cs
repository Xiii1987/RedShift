using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal enum RedshiftManagedAssetType
    {
        Model,
        Material,
        Texture,
        Prefab,
        Animation,
        Controller,
        OverrideController,
        AvatarMask,
        Audio,
        Scene,
        Shader,
        Script,
        Other
    }

    internal static class RedshiftNamingPolicy
    {
        private const string ExclusionStateKey =
            "Redshift.NamingPolicy.Exclusions.V1";

        [Serializable]
        private sealed class ExclusionState
        {
            public List<string> IgnoredGuids = new List<string>();
            public List<string> ExcludedFolders = new List<string>();
        }

        private static readonly Dictionary<string, string> PrefixByExtension =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".mat", "MAT_" },
                { ".png", "T_" },
                { ".jpg", "T_" },
                { ".jpeg", "T_" },
                { ".tga", "T_" },
                { ".psd", "T_" },
                { ".exr", "T_" },
                { ".hdr", "T_" },
                { ".tif", "T_" },
                { ".tiff", "T_" },
                { ".fbx", "M_" },
                { ".obj", "M_" },
                { ".blend", "M_" },
                { ".prefab", "PF_" },
                { ".anim", "ANIM_" },
                { ".controller", "AC_" },
                { ".overrideController", "AOC_" },
                { ".mask", "MASK_" },
                { ".wav", "AUD_" },
                { ".mp3", "AUD_" },
                { ".ogg", "AUD_" },
                { ".aif", "AUD_" },
                { ".aiff", "AUD_" },
                { ".unity", "SCN_" },
                { ".shader", "SH_" },
                { ".shadergraph", "SH_" },
                { ".compute", "CSH_" }
            };

        private static readonly string[] KnownLegacyPrefixes =
        {
            "OverrideController_",
            "AnimatorController_",
            "Controller_",
            "Animation_",
            "Material_",
            "Texture_",
            "Prefab_",
            "Shader_",
            "Model_",
            "ANIM_",
            "MASK_",
            "MAT_",
            "AOC_",
            "AUD_",
            "SCN_",
            "CSH_",
            "CTRL_",
            "SFX_",
            "Mesh_",
            "Tex_",
            "Mtl_",
            "AC_",
            "PF_",
            "SH_",
            "SM_",
            "T_",
            "M_"
        };

        private static ExclusionState _state;

        public static bool TryGetExpectedPrefix(
            string assetPath,
            out string expectedPrefix)
        {
            expectedPrefix = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string extension = Path.GetExtension(assetPath);
            return PrefixByExtension.TryGetValue(extension, out expectedPrefix);
        }

        public static RedshiftManagedAssetType GetAssetType(string path)
        {
            string extension =
                Path.GetExtension(path ?? string.Empty).ToLowerInvariant();

            switch (extension)
            {
                case ".fbx":
                case ".obj":
                case ".blend":
                    return RedshiftManagedAssetType.Model;
                case ".mat":
                    return RedshiftManagedAssetType.Material;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".exr":
                case ".hdr":
                case ".tif":
                case ".tiff":
                    return RedshiftManagedAssetType.Texture;
                case ".prefab":
                    return RedshiftManagedAssetType.Prefab;
                case ".anim":
                    return RedshiftManagedAssetType.Animation;
                case ".controller":
                    return RedshiftManagedAssetType.Controller;
                case ".overridecontroller":
                    return RedshiftManagedAssetType.OverrideController;
                case ".mask":
                    return RedshiftManagedAssetType.AvatarMask;
                case ".wav":
                case ".mp3":
                case ".ogg":
                case ".aif":
                case ".aiff":
                    return RedshiftManagedAssetType.Audio;
                case ".unity":
                    return RedshiftManagedAssetType.Scene;
                case ".shader":
                case ".shadergraph":
                case ".compute":
                    return RedshiftManagedAssetType.Shader;
                case ".cs":
                    return RedshiftManagedAssetType.Script;
                default:
                    return RedshiftManagedAssetType.Other;
            }
        }

        public static bool IsGoverned(string path)
        {
            return TryGetExpectedPrefix(path, out _);
        }

        public static bool IsExcluded(string assetPath)
        {
            string guid = string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(assetPath);

            return IsExcluded(assetPath, guid);
        }

        public static bool IsExcluded(string assetPath, string guid)
        {
            ExclusionState state = GetState();

            if (!string.IsNullOrWhiteSpace(guid) &&
                state.IgnoredGuids.Any(value =>
                    string.Equals(
                        value,
                        guid,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string normalizedPath = NormalizeAssetPath(assetPath);

            foreach (string rawFolder in state.ExcludedFolders)
            {
                string folder = NormalizeAssetPath(rawFolder).TrimEnd('/');

                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                if (normalizedPath.Equals(
                        folder,
                        StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.StartsWith(
                        folder + "/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<string> GetExcludedFolders()
        {
            return GetState()
                .ExcludedFolders
                .Select(NormalizeAssetPath)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static void SetExcludedFolders(IEnumerable<string> folders)
        {
            ExclusionState state = GetState();

            state.ExcludedFolders = (folders ?? Enumerable.Empty<string>())
                .Select(NormalizeAssetPath)
                .Select(value => value.Trim().TrimEnd('/'))
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value) &&
                    (value.Equals("Assets", StringComparison.OrdinalIgnoreCase) ||
                     value.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();

            SaveState();
        }

        public static bool IsGuidIgnored(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return false;
            }

            return GetState().IgnoredGuids.Any(value =>
                string.Equals(value, guid, StringComparison.OrdinalIgnoreCase));
        }

        public static void IgnoreGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid) || IsGuidIgnored(guid))
            {
                return;
            }

            GetState().IgnoredGuids.Add(guid);
            SaveState();
        }

        public static void UnignoreGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            ExclusionState state = GetState();
            state.IgnoredGuids.RemoveAll(value =>
                string.Equals(value, guid, StringComparison.OrdinalIgnoreCase));
            SaveState();
        }

        public static int IgnoredGuidCount
        {
            get { return GetState().IgnoredGuids.Count; }
        }

        public static void ClearIgnoredGuids()
        {
            GetState().IgnoredGuids.Clear();
            SaveState();
        }

        public static string StripLegacyPrefix(
            string name,
            bool stripAnyShortPrefix)
        {
            string result = (name ?? string.Empty).Trim();

            for (int pass = 0; pass < 4; pass++)
            {
                string before = result;

                foreach (string prefix in KnownLegacyPrefixes
                             .OrderByDescending(value => value.Length))
                {
                    if (result.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        result = result.Substring(prefix.Length);
                        break;
                    }
                }

                if (stripAnyShortPrefix)
                {
                    int underscore = result.IndexOf('_');

                    if (underscore > 0 && underscore <= 2)
                    {
                        string token = result.Substring(0, underscore);

                        if (token.All(char.IsLetter))
                        {
                            result = result.Substring(underscore + 1);
                        }
                    }
                }

                if (result.Equals(before, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return result.TrimStart('_').Trim();
        }

        public static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty)
                .Trim()
                .Replace('\\', '/');
        }

        private static ExclusionState GetState()
        {
            if (_state != null)
            {
                return _state;
            }

            string json = EditorPrefs.GetString(
                ExclusionStateKey,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    _state = JsonUtility.FromJson<ExclusionState>(json);
                }
                catch
                {
                    _state = null;
                }
            }

            if (_state == null)
            {
                _state = new ExclusionState();
            }

            if (_state.IgnoredGuids == null)
            {
                _state.IgnoredGuids = new List<string>();
            }

            if (_state.ExcludedFolders == null)
            {
                _state.ExcludedFolders = new List<string>();
            }

            return _state;
        }

        private static void SaveState()
        {
            EditorPrefs.SetString(
                ExclusionStateKey,
                JsonUtility.ToJson(GetState(), false));
        }
    }
}
