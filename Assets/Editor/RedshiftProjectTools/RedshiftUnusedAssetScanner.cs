using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal static class RedshiftUnusedAssetScanner
    {
        public static RedshiftUnusedScanResult Scan(
            RedshiftToolSettings settings)
        {
            string[] allAssets = RedshiftAssetUtility.GetProjectAssetPaths();
            var roots =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var used =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddSceneRoots(settings, roots);
            AddImplicitKeepRoots(allAssets, settings, roots);

            string[] rootArray = roots.ToArray();

            try
            {
                for (int i = 0; i < rootArray.Length; i++)
                {
                    string root = rootArray[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Redshift Unused Asset Scan",
                        "Following dependencies from " + root,
                        rootArray.Length == 0
                            ? 1f
                            : (float)i / rootArray.Length))
                    {
                        break;
                    }

                    string[] dependencies =
                        AssetDatabase.GetDependencies(root, true);

                    foreach (string dependency in dependencies)
                    {
                        if (dependency.StartsWith(
                            "Assets/",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            used.Add(dependency);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            IReadOnlyList<string> alwaysKeepFolders =
                settings.GetAlwaysKeepFolders();

            var result = new RedshiftUnusedScanResult
            {
                RootCount = roots.Count,
                UsedAssetCount = used.Count,
                ScannedAssetCount = allAssets.Length
            };

            foreach (string path in allAssets)
            {
                if (!RedshiftAssetUtility.IsConservativeUnusedCandidateType(path))
                {
                    continue;
                }

                if (used.Contains(path))
                {
                    continue;
                }

                if (RedshiftAssetUtility.IsUnderAnyFolder(
                    path,
                    alwaysKeepFolders))
                {
                    continue;
                }

                result.Candidates.Add(new RedshiftUnusedCandidate
                {
                    AssetPath = path,
                    Category = RedshiftAssetUtility.GetCategory(path),
                    FileSizeBytes =
                        RedshiftAssetUtility.GetSourceFileSize(path),
                    Selected = false
                });
            }

            result.Candidates.Sort((left, right) =>
            {
                int categoryComparison = string.Compare(
                    left.Category,
                    right.Category,
                    StringComparison.OrdinalIgnoreCase);

                return categoryComparison != 0
                    ? categoryComparison
                    : string.Compare(
                        left.AssetPath,
                        right.AssetPath,
                        StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        public static int MoveSelectedToQuarantine(
            IReadOnlyList<RedshiftUnusedCandidate> candidates,
            RedshiftToolSettings settings,
            out string quarantineFolder,
            out List<string> errors)
        {
            errors = new List<string>();
            int movedCount = 0;

            string root = string.IsNullOrWhiteSpace(settings.QuarantineRoot)
                ? "Assets/_Quarantine/RedshiftTools"
                : settings.QuarantineRoot.TrimEnd('/').Replace('\\', '/');

            quarantineFolder =
                root + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            if (!RedshiftAssetUtility.EnsureAssetFolder(quarantineFolder))
            {
                errors.Add(
                    "Could not create quarantine folder: " +
                    quarantineFolder);
                return 0;
            }

            var moves = new List<KeyValuePair<string, string>>();

            foreach (RedshiftUnusedCandidate candidate in candidates)
            {
                if (!candidate.Selected)
                {
                    continue;
                }

                string sourcePath = candidate.AssetPath;
                string relativePath =
                    sourcePath.StartsWith(
                        "Assets/",
                        StringComparison.OrdinalIgnoreCase)
                        ? sourcePath.Substring("Assets/".Length)
                        : Path.GetFileName(sourcePath);

                string relativeDirectory =
                    Path.GetDirectoryName(relativePath)
                        ?.Replace('\\', '/') ?? string.Empty;

                string destinationFolder =
                    string.IsNullOrEmpty(relativeDirectory)
                        ? quarantineFolder
                        : quarantineFolder + "/" + relativeDirectory;

                if (!RedshiftAssetUtility.EnsureAssetFolder(
                    destinationFolder))
                {
                    errors.Add(
                        sourcePath +
                        ": Could not create destination folder.");
                    continue;
                }

                string destinationPath =
                    destinationFolder + "/" +
                    Path.GetFileName(sourcePath);

                destinationPath =
                    AssetDatabase.GenerateUniqueAssetPath(destinationPath);

                moves.Add(
                    new KeyValuePair<string, string>(
                        sourcePath,
                        destinationPath));
            }

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (KeyValuePair<string, string> move in moves)
                {
                    string moveError =
                        AssetDatabase.MoveAsset(move.Key, move.Value);

                    if (!string.IsNullOrEmpty(moveError))
                    {
                        errors.Add(move.Key + ": " + moveError);
                        continue;
                    }

                    movedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return movedCount;
        }

        private static void AddSceneRoots(
            RedshiftToolSettings settings,
            ISet<string> roots)
        {
            if (settings.IncludeAllScenesAsUnusedRoots)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (path.StartsWith(
                        "Assets/",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        roots.Add(path);
                    }
                }

                return;
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled
                    && !string.IsNullOrWhiteSpace(scene.path))
                {
                    roots.Add(scene.path);
                }
            }
        }

        private static void AddImplicitKeepRoots(
            IEnumerable<string> allAssets,
            RedshiftToolSettings settings,
            ISet<string> roots)
        {
            IReadOnlyList<string> keepFolders =
                settings.GetAlwaysKeepFolders();

            foreach (string path in allAssets)
            {
                if (RedshiftAssetUtility.IsUnderAnyFolder(path, keepFolders)
                    || IsInsideSpecialRuntimeFolder(path))
                {
                    roots.Add(path);
                }
            }
        }

        private static bool IsInsideSpecialRuntimeFolder(string path)
        {
            string normalized = "/" +
                path.Replace('\\', '/').Trim('/') +
                "/";

            return normalized.IndexOf(
                       "/Resources/",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf(
                       "/StreamingAssets/",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
