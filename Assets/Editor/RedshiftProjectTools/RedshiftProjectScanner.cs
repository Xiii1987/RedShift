using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Redshift.EditorTools
{
    internal static class RedshiftProjectScanner
    {
        public static RedshiftProjectStats BuildProjectStats()
        {
            string[] paths = RedshiftAssetUtility.GetProjectAssetPaths();
            var stats = new RedshiftProjectStats();

            foreach (string path in paths)
            {
                stats.TotalAssets++;
                stats.SourceFileBytes += RedshiftAssetUtility.GetSourceFileSize(path);

                string extension = Path.GetExtension(path);

                if (RedshiftAssetUtility.IsTexture(path))
                {
                    stats.Textures++;
                }
                else if (RedshiftAssetUtility.IsModel(path))
                {
                    stats.Models++;
                }
                else if (RedshiftAssetUtility.IsAudio(path))
                {
                    stats.Audio++;
                }
                else if (RedshiftAssetUtility.IsAnimation(path))
                {
                    stats.Animations++;
                }
                else if (extension.Equals(".mat", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Materials++;
                }
                else if (extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Prefabs++;
                }
                else if (extension.Equals(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Scenes++;
                }
                else if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Scripts++;
                }
            }

            return stats;
        }

        public static List<RedshiftAuditIssue> RunAudit(
            RedshiftToolSettings settings)
        {
            var issues = new List<RedshiftAuditIssue>();
            string[] assetPaths = RedshiftAssetUtility.GetProjectAssetPaths();
            IReadOnlyList<string> ignoredFolders =
                settings.GetIgnoredAuditFolders();

            var duplicateNames =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                for (int i = 0; i < assetPaths.Length; i++)
                {
                    string path = assetPaths[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Redshift Project Audit",
                        "Checking " + path,
                        assetPaths.Length == 0 ? 1f : (float)i / assetPaths.Length))
                    {
                        break;
                    }

                    if (RedshiftAssetUtility.IsUnderAnyFolder(path, ignoredFolders))
                    {
                        continue;
                    }

                    CheckNaming(path, issues);
                    CheckOversizedTexture(path, settings, issues);
                    AddDuplicateNameEntry(path, duplicateNames);
                }

                AddDuplicateNameIssues(duplicateNames, issues);
                AddEmptyFolderIssues(ignoredFolders, issues);

                if (settings.ScanPrefabsForMissingReferences)
                {
                    ScanPrefabsForMissingReferences(assetPaths, ignoredFolders, issues);
                }

                if (settings.ScanOpenScenesForMissingReferences)
                {
                    ScanOpenScenesForMissingReferences(issues);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return issues
                .OrderBy(issue => issue.Type)
                .ThenBy(issue => issue.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void CheckNaming(
            string path,
            ICollection<RedshiftAuditIssue> issues)
        {
            if (RedshiftNamingRules.ObeysRule(path, out string expectedPrefix))
            {
                return;
            }

            issues.Add(new RedshiftAuditIssue(
                RedshiftAuditIssueType.NamingViolation,
                path,
                "Expected prefix \"" + expectedPrefix + "\"."));
        }

        private static void CheckOversizedTexture(
            string path,
            RedshiftToolSettings settings,
            ICollection<RedshiftAuditIssue> issues)
        {
            if (!RedshiftAssetUtility.IsTexture(path))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return;
            }

            try
            {
                importer.GetSourceTextureWidthAndHeight(
                    out int width,
                    out int height);

                int largestDimension = Math.Max(width, height);

                if (largestDimension < settings.OversizedTextureThreshold)
                {
                    return;
                }

                issues.Add(new RedshiftAuditIssue(
                    RedshiftAuditIssueType.OversizedTexture,
                    path,
                    width + " × " + height +
                    " source texture. Threshold: " +
                    settings.OversizedTextureThreshold + " px."));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Redshift Project Tools could not read texture dimensions for "
                    + path + ". " + exception.Message);
            }
        }

        private static void AddDuplicateNameEntry(
            string path,
            IDictionary<string, List<string>> duplicateNames)
        {
            string fileName = Path.GetFileName(path);

            if (!duplicateNames.TryGetValue(fileName, out List<string> matches))
            {
                matches = new List<string>();
                duplicateNames.Add(fileName, matches);
            }

            matches.Add(path);
        }

        private static void AddDuplicateNameIssues(
            IDictionary<string, List<string>> duplicateNames,
            ICollection<RedshiftAuditIssue> issues)
        {
            foreach (KeyValuePair<string, List<string>> pair in duplicateNames)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                string locations = string.Join(
                    ", ",
                    pair.Value.Select(Path.GetDirectoryName));

                foreach (string path in pair.Value)
                {
                    issues.Add(new RedshiftAuditIssue(
                        RedshiftAuditIssueType.DuplicateName,
                        path,
                        "Same filename exists in " + pair.Value.Count +
                        " locations: " + locations));
                }
            }
        }

        private static void AddEmptyFolderIssues(
            IReadOnlyList<string> ignoredFolders,
            ICollection<RedshiftAuditIssue> issues)
        {
            if (!Directory.Exists(Application.dataPath))
            {
                return;
            }

            string[] folders;

            try
            {
                folders = Directory.GetDirectories(
                    Application.dataPath,
                    "*",
                    SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Redshift Project Tools could not enumerate folders. "
                    + exception.Message);
                return;
            }

            foreach (string fullFolderPath in folders)
            {
                string assetFolderPath =
                    "Assets" +
                    fullFolderPath
                        .Substring(Application.dataPath.Length)
                        .Replace('\\', '/');

                if (RedshiftAssetUtility.IsUnderAnyFolder(
                    assetFolderPath,
                    ignoredFolders))
                {
                    continue;
                }

                bool hasContent;

                try
                {
                    hasContent = Directory
                        .EnumerateFileSystemEntries(fullFolderPath)
                        .Any(entry =>
                            !entry.EndsWith(
                                ".meta",
                                StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    continue;
                }

                if (!hasContent)
                {
                    issues.Add(new RedshiftAuditIssue(
                        RedshiftAuditIssueType.EmptyFolder,
                        assetFolderPath,
                        "Folder contains no assets or subfolders."));
                }
            }
        }

        private static void ScanPrefabsForMissingReferences(
            IEnumerable<string> assetPaths,
            IReadOnlyList<string> ignoredFolders,
            ICollection<RedshiftAuditIssue> issues)
        {
            string[] prefabPaths = assetPaths
                .Where(path =>
                    path.EndsWith(
                        ".prefab",
                        StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                    !RedshiftAssetUtility.IsUnderAnyFolder(
                        path,
                        ignoredFolders))
                .ToArray();

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                string path = prefabPaths[i];

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Redshift Project Audit",
                    "Checking prefab references: " + path,
                    prefabPaths.Length == 0 ? 1f : (float)i / prefabPaths.Length))
                {
                    return;
                }

                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (root == null)
                {
                    continue;
                }

                ScanGameObjectHierarchy(root, path, issues);
            }
        }

        private static void ScanOpenScenesForMissingReferences(
            ICollection<RedshiftAuditIssue> issues)
        {
            for (int sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if (!scene.isLoaded)
                {
                    continue;
                }

                string scenePath = string.IsNullOrEmpty(scene.path)
                    ? "<Unsaved Scene: " + scene.name + ">"
                    : scene.path;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ScanGameObjectHierarchy(root, scenePath, issues);
                }
            }
        }

        private static void ScanGameObjectHierarchy(
            GameObject root,
            string assetPath,
            ICollection<RedshiftAuditIssue> issues)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform current in transforms)
            {
                int missingScriptCount =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        current.gameObject);

                if (missingScriptCount > 0)
                {
                    issues.Add(new RedshiftAuditIssue(
                        RedshiftAuditIssueType.MissingScript,
                        assetPath,
                        GetHierarchyPath(current, root.transform) +
                        " has " + missingScriptCount +
                        " missing script component(s)."));
                }
            }

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    if (materials[materialIndex] != null)
                    {
                        continue;
                    }

                    issues.Add(new RedshiftAuditIssue(
                        RedshiftAuditIssueType.MissingMaterial,
                        assetPath,
                        GetHierarchyPath(renderer.transform, root.transform) +
                        " has an empty material slot at index " +
                        materialIndex + "."));
                }
            }
        }

        private static string GetHierarchyPath(
            Transform current,
            Transform root)
        {
            var names = new Stack<string>();
            Transform cursor = current;

            while (cursor != null)
            {
                names.Push(cursor.name);

                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", names);
        }
    }
}
