using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Redshift.EditorTools
{
    internal sealed class RedshiftToolSettings
    {
        private const string KeyPrefix = "Redshift.ProjectTools.";

        public int OversizedTextureThreshold = 4096;
        public bool ScanPrefabsForMissingReferences = true;
        public bool ScanOpenScenesForMissingReferences = true;
        public bool IncludeAllScenesAsUnusedRoots = true;
        public bool IncludeFolderContentsInRenameSelection = true;
        public bool AllowScriptRenaming = false;

        public string IgnoredAuditFolders =
            "Assets/Editor\n" +
            "Assets/Plugins\n" +
            "Assets/ThirdParty\n" +
            "Assets/TextMesh Pro\n" +
            "Assets/_Quarantine";

        public string AlwaysKeepFolders =
            "Assets/Editor\n" +
            "Assets/Plugins\n" +
            "Assets/Resources\n" +
            "Assets/StreamingAssets\n" +
            "Assets/Gizmos\n" +
            "Assets/Editor Default Resources\n" +
            "Assets/ThirdParty\n" +
            "Assets/TextMesh Pro\n" +
            "Assets/_Quarantine";

        public string QuarantineRoot = "Assets/_Quarantine/RedshiftTools";

        public void Load()
        {
            OversizedTextureThreshold = EditorPrefs.GetInt(
                KeyPrefix + "OversizedTextureThreshold",
                OversizedTextureThreshold);

            ScanPrefabsForMissingReferences = EditorPrefs.GetBool(
                KeyPrefix + "ScanPrefabs",
                ScanPrefabsForMissingReferences);

            ScanOpenScenesForMissingReferences = EditorPrefs.GetBool(
                KeyPrefix + "ScanOpenScenes",
                ScanOpenScenesForMissingReferences);

            IncludeAllScenesAsUnusedRoots = EditorPrefs.GetBool(
                KeyPrefix + "AllScenesAsRoots",
                IncludeAllScenesAsUnusedRoots);

            IncludeFolderContentsInRenameSelection = EditorPrefs.GetBool(
                KeyPrefix + "RenameFolderContents",
                IncludeFolderContentsInRenameSelection);

            AllowScriptRenaming = EditorPrefs.GetBool(
                KeyPrefix + "AllowScriptRenaming",
                AllowScriptRenaming);

            IgnoredAuditFolders = EditorPrefs.GetString(
                KeyPrefix + "IgnoredAuditFolders",
                IgnoredAuditFolders);

            AlwaysKeepFolders = EditorPrefs.GetString(
                KeyPrefix + "AlwaysKeepFolders",
                AlwaysKeepFolders);

            QuarantineRoot = EditorPrefs.GetString(
                KeyPrefix + "QuarantineRoot",
                QuarantineRoot);
        }

        public void Save()
        {
            EditorPrefs.SetInt(
                KeyPrefix + "OversizedTextureThreshold",
                OversizedTextureThreshold);

            EditorPrefs.SetBool(
                KeyPrefix + "ScanPrefabs",
                ScanPrefabsForMissingReferences);

            EditorPrefs.SetBool(
                KeyPrefix + "ScanOpenScenes",
                ScanOpenScenesForMissingReferences);

            EditorPrefs.SetBool(
                KeyPrefix + "AllScenesAsRoots",
                IncludeAllScenesAsUnusedRoots);

            EditorPrefs.SetBool(
                KeyPrefix + "RenameFolderContents",
                IncludeFolderContentsInRenameSelection);

            EditorPrefs.SetBool(
                KeyPrefix + "AllowScriptRenaming",
                AllowScriptRenaming);

            EditorPrefs.SetString(
                KeyPrefix + "IgnoredAuditFolders",
                IgnoredAuditFolders ?? string.Empty);

            EditorPrefs.SetString(
                KeyPrefix + "AlwaysKeepFolders",
                AlwaysKeepFolders ?? string.Empty);

            EditorPrefs.SetString(
                KeyPrefix + "QuarantineRoot",
                QuarantineRoot ?? "Assets/_Quarantine/RedshiftTools");
        }

        public IReadOnlyList<string> GetIgnoredAuditFolders()
        {
            return ParseFolderList(IgnoredAuditFolders);
        }

        public IReadOnlyList<string> GetAlwaysKeepFolders()
        {
            return ParseFolderList(AlwaysKeepFolders);
        }

        private static IReadOnlyList<string> ParseFolderList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty)
                .Trim()
                .TrimEnd('/')
                .Replace('\\', '/');
        }
    }
}
