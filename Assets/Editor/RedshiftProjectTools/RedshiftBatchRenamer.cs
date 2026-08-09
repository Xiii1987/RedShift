using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Redshift.EditorTools
{
    internal static class RedshiftBatchRenamer
    {
        public static List<string> CollectSelectedAssetPaths(
            bool includeFolderContents,
            bool allowScripts)
        {
            var paths =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(path))
                {
                    if (!includeFolderContents)
                    {
                        continue;
                    }

                    foreach (string childGuid in AssetDatabase.FindAssets(
                        string.Empty,
                        new[] { path }))
                    {
                        string childPath =
                            AssetDatabase.GUIDToAssetPath(childGuid);

                        AddPathIfAllowed(childPath, allowScripts, paths);
                    }

                    continue;
                }

                AddPathIfAllowed(path, allowScripts, paths);
            }

            return paths
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<RedshiftRenamePreview> BuildPreview(
            IReadOnlyList<string> assetPaths,
            string prefix,
            string suffix,
            string find,
            string replace,
            bool useNumbering,
            int startingNumber,
            int numberPadding)
        {
            var previews = new List<RedshiftRenamePreview>();

            for (int i = 0; i < assetPaths.Count; i++)
            {
                string path = assetPaths[i];
                string currentName = Path.GetFileNameWithoutExtension(path);
                string transformedName = currentName;

                if (!string.IsNullOrEmpty(find))
                {
                    transformedName = ReplaceOrdinalIgnoreCase(
                        transformedName,
                        find,
                        replace ?? string.Empty);
                }

                transformedName =
                    (prefix ?? string.Empty) +
                    transformedName +
                    (suffix ?? string.Empty);

                if (useNumbering)
                {
                    int number = startingNumber + i;
                    transformedName += "_" +
                        number.ToString().PadLeft(
                            Math.Max(1, numberPadding),
                            '0');
                }

                previews.Add(new RedshiftRenamePreview
                {
                    AssetPath = path,
                    CurrentName = currentName,
                    NewName = transformedName,
                    Selected = true
                });
            }

            ValidatePreview(previews);
            return previews;
        }

        public static int ApplyPreview(
            IReadOnlyList<RedshiftRenamePreview> previews,
            out List<string> errors)
        {
            errors = new List<string>();
            int renamedCount = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (RedshiftRenamePreview preview in previews)
                {
                    if (!preview.Selected || !preview.IsValid)
                    {
                        continue;
                    }

                    string error = AssetDatabase.RenameAsset(
                        preview.AssetPath,
                        preview.NewName);

                    if (!string.IsNullOrEmpty(error))
                    {
                        errors.Add(preview.AssetPath + ": " + error);
                        continue;
                    }

                    renamedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return renamedCount;
        }

        private static void AddPathIfAllowed(
            string path,
            bool allowScripts,
            ISet<string> paths)
        {
            if (!RedshiftAssetUtility.IsProjectAssetFile(path))
            {
                return;
            }

            if (!allowScripts
                && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            paths.Add(path);
        }

        private static void ValidatePreview(
            IList<RedshiftRenamePreview> previews)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            foreach (RedshiftRenamePreview preview in previews)
            {
                if (string.IsNullOrWhiteSpace(preview.NewName))
                {
                    preview.Error = "The new name is empty.";
                    continue;
                }

                if (preview.NewName.IndexOfAny(invalidCharacters) >= 0
                    || preview.NewName.Contains("/")
                    || preview.NewName.Contains("\\"))
                {
                    preview.Error =
                        "The new name contains invalid filename characters.";
                }
            }

            var targetGroups = previews
                .GroupBy(
                    preview =>
                        Path.GetDirectoryName(preview.AssetPath) + "/" +
                        preview.NewName +
                        Path.GetExtension(preview.AssetPath),
                    StringComparer.OrdinalIgnoreCase);

            foreach (IGrouping<string, RedshiftRenamePreview> group in targetGroups)
            {
                if (group.Count() < 2)
                {
                    continue;
                }

                foreach (RedshiftRenamePreview preview in group)
                {
                    preview.Error =
                        "Multiple selected assets would receive the same path.";
                }
            }

            foreach (RedshiftRenamePreview preview in previews)
            {
                if (!string.IsNullOrEmpty(preview.Error)
                    || preview.CurrentName == preview.NewName)
                {
                    continue;
                }

                string directory =
                    Path.GetDirectoryName(preview.AssetPath)
                        ?.Replace('\\', '/') ?? "Assets";

                string targetPath =
                    directory + "/" +
                    preview.NewName +
                    Path.GetExtension(preview.AssetPath);

                Object existing =
                    AssetDatabase.LoadMainAssetAtPath(targetPath);

                if (existing != null
                    && !preview.AssetPath.Equals(
                        targetPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    preview.Error =
                        "An asset already exists at the target path. " +
                        "Rename chains and swaps are intentionally blocked in V1.";
                }
            }
        }

        private static string ReplaceOrdinalIgnoreCase(
            string source,
            string find,
            string replacement)
        {
            if (string.IsNullOrEmpty(source)
                || string.IsNullOrEmpty(find))
            {
                return source;
            }

            int index = 0;
            var result = new System.Text.StringBuilder();

            while (true)
            {
                int match = source.IndexOf(
                    find,
                    index,
                    StringComparison.OrdinalIgnoreCase);

                if (match < 0)
                {
                    result.Append(source, index, source.Length - index);
                    break;
                }

                result.Append(source, index, match - index);
                result.Append(replacement);
                index = match + find.Length;
            }

            return result.ToString();
        }
    }
}
