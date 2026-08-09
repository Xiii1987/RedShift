using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal static class RedshiftAssetManagerAnalyzer
    {
        public static List<RedshiftManagedAssetRecord> Analyze(
            RedshiftToolSettings settings,
            Func<string, RedshiftAssetFlags> flagsForGuid)
        {
            string[] allAssets = RedshiftAssetUtility.GetProjectAssetPaths();
            Dictionary<string, List<RedshiftMaterialTextureLink>> textureLinks =
                BuildTextureMaterialLinks(allAssets);
            Dictionary<string, List<string>> materialTextures =
                BuildMaterialTextureLists(textureLinks);
            HashSet<string> unusedTextures = BuildUnusedTextureSet(settings);
            var records = new List<RedshiftManagedAssetRecord>();

            foreach (string path in allAssets)
            {
                RedshiftManagedAssetType type =
                    RedshiftNamingPolicy.GetAssetType(path);

                if (type == RedshiftManagedAssetType.Other)
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                var record = new RedshiftManagedAssetRecord
                {
                    Path = path,
                    Guid = guid,
                    Name = Path.GetFileNameWithoutExtension(path),
                    ParentFolder = GetParentFolder(path),
                    Type = type,
                    Flags = flagsForGuid(guid),
                    IsUnusedCandidate = unusedTextures.Contains(path),
                    Decision = RedshiftRenameDecision.None
                };

                if (textureLinks.TryGetValue(
                    path,
                    out List<RedshiftMaterialTextureLink> links))
                {
                    record.MaterialLinks.AddRange(links);
                }

                if (materialTextures.TryGetValue(
                    path,
                    out List<string> textures))
                {
                    record.LinkedTextures.AddRange(textures);
                }

                records.Add(record);
            }

            records.Sort((left, right) => string.Compare(
                left.Path,
                right.Path,
                StringComparison.OrdinalIgnoreCase));

            RefreshNaming(records);
            return records;
        }

        public static void RefreshNaming(
            List<RedshiftManagedAssetRecord> records)
        {
            foreach (RedshiftManagedAssetRecord record in records)
            {
                record.ExpectedPrefix = string.Empty;
                record.SuggestedName = record.Name;
                record.SuggestionReason = string.Empty;
                record.Ambiguous = false;
                record.SuggestionCollision = false;

                RedshiftNamingPolicy.TryGetExpectedPrefix(
                    record.Path,
                    out record.ExpectedPrefix);
            }

            foreach (RedshiftManagedAssetRecord record in records)
            {
                if (record.Type != RedshiftManagedAssetType.Texture)
                {
                    BuildSuggestion(record);
                }
            }

            foreach (RedshiftManagedAssetRecord record in records.Where(
                item => item.Type == RedshiftManagedAssetType.Texture))
            {
                BuildTextureSuggestion(record, records);
            }

            ResolveSuggestionCollisions(records);

            foreach (RedshiftManagedAssetRecord record in records)
            {
                if (RedshiftNamingPolicy.IsExcluded(record.Path, record.Guid))
                {
                    record.Naming = RedshiftAssetNamingState.Excluded;
                }
                else if (!RedshiftNamingPolicy.IsGoverned(record.Path))
                {
                    record.Naming = RedshiftAssetNamingState.NotGoverned;
                }
                else
                {
                    // This intentionally mirrors Project Health. Suggestions are
                    // workflow shortcuts; the compliance contract is the prefix.
                    record.Naming =
                        string.IsNullOrWhiteSpace(record.ExpectedPrefix) ||
                        record.Name.StartsWith(
                            record.ExpectedPrefix,
                            StringComparison.OrdinalIgnoreCase)
                            ? RedshiftAssetNamingState.Compliant
                            : RedshiftAssetNamingState.Violation;
                }
            }
        }

        public static void UpdateLinksAfterRename(
            List<RedshiftManagedAssetRecord> records,
            string oldPath,
            string newPath,
            RedshiftManagedAssetType type)
        {
            if (type == RedshiftManagedAssetType.Material)
            {
                foreach (RedshiftManagedAssetRecord texture in records.Where(
                    item => item.Type == RedshiftManagedAssetType.Texture))
                {
                    foreach (RedshiftMaterialTextureLink link in texture.MaterialLinks)
                    {
                        if (link.MaterialPath.Equals(
                            oldPath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            link.MaterialPath = newPath;
                        }
                    }
                }
            }
            else if (type == RedshiftManagedAssetType.Texture)
            {
                foreach (RedshiftManagedAssetRecord material in records.Where(
                    item => item.Type == RedshiftManagedAssetType.Material))
                {
                    for (int i = 0; i < material.LinkedTextures.Count; i++)
                    {
                        if (material.LinkedTextures[i].Equals(
                            oldPath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            material.LinkedTextures[i] = newPath;
                        }
                    }
                }
            }
        }

        public static void RemoveLinksForDeletedAsset(
            List<RedshiftManagedAssetRecord> records,
            string oldPath,
            RedshiftManagedAssetType type)
        {
            if (type == RedshiftManagedAssetType.Material)
            {
                foreach (RedshiftManagedAssetRecord texture in records.Where(
                    item => item.Type == RedshiftManagedAssetType.Texture))
                {
                    texture.MaterialLinks.RemoveAll(link =>
                        link.MaterialPath.Equals(
                            oldPath,
                            StringComparison.OrdinalIgnoreCase));
                }
            }
            else if (type == RedshiftManagedAssetType.Texture)
            {
                foreach (RedshiftManagedAssetRecord material in records.Where(
                    item => item.Type == RedshiftManagedAssetType.Material))
                {
                    material.LinkedTextures.RemoveAll(path =>
                        path.Equals(oldPath, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        public static string GetParentFolder(string path)
        {
            return LastSegment(GetDirectory(path));
        }

        public static string GetDirectory(string path)
        {
            return Path.GetDirectoryName(path ?? string.Empty)
                ?.Replace('\\', '/') ?? "Assets";
        }

        private static void BuildSuggestion(RedshiftManagedAssetRecord record)
        {
            if (!RedshiftNamingPolicy.IsGoverned(record.Path))
            {
                return;
            }

            switch (record.Type)
            {
                case RedshiftManagedAssetType.Model:
                    BuildModelSuggestion(record);
                    break;
                case RedshiftManagedAssetType.Material:
                    BuildMaterialSuggestion(record);
                    break;
                default:
                    BuildPrefixSuggestion(record);
                    break;
            }
        }

        private static void BuildModelSuggestion(
            RedshiftManagedAssetRecord record)
        {
            string prefix = string.IsNullOrWhiteSpace(record.ExpectedPrefix)
                ? "M_"
                : record.ExpectedPrefix;

            string owner = CanonicalParent(
                record.Path,
                new[] { "Models", "Model", "Meshes", "Mesh" });

            string cleaned = RedshiftNamingPolicy.StripLegacyPrefix(
                record.Name,
                true);

            string identifier = string.Empty;

            if (!string.IsNullOrWhiteSpace(owner) &&
                cleaned.StartsWith(owner, StringComparison.OrdinalIgnoreCase))
            {
                identifier = cleaned.Substring(owner.Length).TrimStart('_');
            }
            else
            {
                int separator = cleaned.IndexOf('_');

                if (separator >= 0 && separator < cleaned.Length - 1)
                {
                    identifier = cleaned.Substring(separator + 1).TrimStart('_');
                }
            }

            string baseName = string.IsNullOrWhiteSpace(owner)
                ? cleaned
                : owner;

            if (!string.IsNullOrWhiteSpace(identifier) &&
                !identifier.Equals(baseName, StringComparison.OrdinalIgnoreCase))
            {
                baseName += "_" + identifier;
            }

            record.SuggestedName = prefix + baseName;
            record.SuggestionReason =
                "M_ + parent folder; old 1–2 letter prefixes are removed and useful trailing identifiers are retained.";
        }

        private static void BuildMaterialSuggestion(
            RedshiftManagedAssetRecord record)
        {
            string prefix = string.IsNullOrWhiteSpace(record.ExpectedPrefix)
                ? "MAT_"
                : record.ExpectedPrefix;

            List<string> owners = record.LinkedTextures
                .Select(path => FolderAbove(path, "Textures"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            string owner = MostCommon(owners);

            if (string.IsNullOrWhiteSpace(owner))
            {
                owner = CanonicalParent(
                    record.Path,
                    new[] { "Materials", "Material" });
            }

            List<string> tokens = record.LinkedTextures
                .Select(path => SecondTextureToken(path))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            string token = MostCommon(tokens);
            string baseName = string.IsNullOrWhiteSpace(owner)
                ? RedshiftNamingPolicy.StripLegacyPrefix(record.Name, false)
                : owner;

            if (!string.IsNullOrWhiteSpace(token) &&
                !token.Equals(baseName, StringComparison.OrdinalIgnoreCase))
            {
                baseName += "_" + token;
            }

            record.SuggestedName = prefix + baseName;
            record.Ambiguous =
                owners.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1 ||
                tokens.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;

            record.SuggestionReason = record.LinkedTextures.Count > 0
                ? "MAT_ + folder above linked Textures + second underscore token from the texture name."
                : "No linked texture evidence; parent-folder fallback used.";
        }

        private static void BuildTextureSuggestion(
            RedshiftManagedAssetRecord record,
            List<RedshiftManagedAssetRecord> records)
        {
            string prefix = string.IsNullOrWhiteSpace(record.ExpectedPrefix)
                ? "T_"
                : record.ExpectedPrefix;

            List<string> materialBases = record.MaterialLinks
                .Select(link => records.FirstOrDefault(item =>
                    item.Type == RedshiftManagedAssetType.Material &&
                    item.Path.Equals(
                        link.MaterialPath,
                        StringComparison.OrdinalIgnoreCase)))
                .Where(material => material != null)
                .Select(material => RedshiftNamingPolicy.StripLegacyPrefix(
                    string.IsNullOrWhiteSpace(material.SuggestedName)
                        ? material.Name
                        : material.SuggestedName,
                    false))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roles = new List<string>();

            foreach (RedshiftMaterialTextureLink link in record.MaterialLinks)
            {
                string role = InferTextureRole(link.PropertyName, record.Name);

                if (!string.IsNullOrWhiteSpace(role) &&
                    !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    roles.Add(role);
                }
            }

            if (roles.Count == 0)
            {
                string role = InferTextureRole(string.Empty, record.Name);
                if (!string.IsNullOrWhiteSpace(role))
                {
                    roles.Add(role);
                }
            }

            string materialBase = materialBases.Count > 0
                ? materialBases[0]
                : RedshiftNamingPolicy.StripLegacyPrefix(record.Name, false);

            string roleName = roles.Count > 0 ? roles[0] : string.Empty;

            record.SuggestedName =
                prefix + materialBase +
                (string.IsNullOrWhiteSpace(roleName)
                    ? string.Empty
                    : "_" + roleName);

            record.Ambiguous = materialBases.Count != 1 || roles.Count != 1;
            record.SuggestionReason = materialBases.Count == 1 && roles.Count == 1
                ? "T_ + linked material name + normalised texture type."
                : "Material/type evidence is incomplete or conflicting; review the suggestion or override it.";
        }

        private static void BuildPrefixSuggestion(
            RedshiftManagedAssetRecord record)
        {
            bool stripShort =
                record.Type == RedshiftManagedAssetType.Animation ||
                record.Type == RedshiftManagedAssetType.Controller ||
                record.Type == RedshiftManagedAssetType.OverrideController ||
                record.Type == RedshiftManagedAssetType.AvatarMask;

            string baseName = RedshiftNamingPolicy.StripLegacyPrefix(
                record.Name,
                stripShort);

            record.SuggestedName =
                (record.ExpectedPrefix ?? string.Empty) + baseName;
            record.SuggestionReason =
                "Prefix-only suggestion; the existing base name is preserved.";
        }

        private static void ResolveSuggestionCollisions(
            List<RedshiftManagedAssetRecord> records)
        {
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (RedshiftManagedAssetRecord record in records
                .Where(item => RedshiftNamingPolicy.IsGoverned(item.Path))
                .OrderByDescending(item => item.Name.Equals(
                    item.SuggestedName,
                    StringComparison.OrdinalIgnoreCase))
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                string extension = Path.GetExtension(record.Path).ToLowerInvariant();
                string baseSuggestion = record.SuggestedName;
                string candidate = baseSuggestion;
                int number = 2;

                while (!reserved.Add(extension + "|" + candidate))
                {
                    candidate = baseSuggestion + "_" + number.ToString("00");
                    number++;
                    record.SuggestionCollision = true;
                }

                record.SuggestedName = candidate;
            }
        }

        private static HashSet<string> BuildUnusedTextureSet(
            RedshiftToolSettings settings)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                RedshiftUnusedScanResult scan =
                    RedshiftUnusedAssetScanner.Scan(settings);

                foreach (RedshiftUnusedCandidate candidate in scan.Candidates)
                {
                    if (candidate.Category.Equals(
                        "Texture",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(candidate.AssetPath);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Redshift Asset Manager unused scan failed: " +
                    exception.Message);
            }

            return result;
        }

        private static Dictionary<string, List<RedshiftMaterialTextureLink>>
            BuildTextureMaterialLinks(IEnumerable<string> allAssets)
        {
            var result =
                new Dictionary<string, List<RedshiftMaterialTextureLink>>(
                    StringComparer.OrdinalIgnoreCase);

            string[] materialPaths = allAssets
                .Where(path => path.EndsWith(
                    ".mat",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            try
            {
                for (int i = 0; i < materialPaths.Length; i++)
                {
                    string materialPath = materialPaths[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Redshift Asset Manager",
                        "Reading material textures: " + materialPath,
                        materialPaths.Length == 0
                            ? 1f
                            : i / (float)materialPaths.Length))
                    {
                        break;
                    }

                    Material material =
                        AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (material == null)
                    {
                        continue;
                    }

                    string[] properties;

                    try
                    {
                        properties = material.GetTexturePropertyNames();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string property in properties)
                    {
                        Texture texture = material.GetTexture(property);

                        if (texture == null)
                        {
                            continue;
                        }

                        string texturePath = AssetDatabase.GetAssetPath(texture);

                        if (string.IsNullOrWhiteSpace(texturePath) ||
                            !texturePath.StartsWith(
                                "Assets/",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!result.TryGetValue(
                            texturePath,
                            out List<RedshiftMaterialTextureLink> links))
                        {
                            links = new List<RedshiftMaterialTextureLink>();
                            result.Add(texturePath, links);
                        }

                        if (!links.Any(link =>
                            link.MaterialPath.Equals(
                                materialPath,
                                StringComparison.OrdinalIgnoreCase) &&
                            link.PropertyName.Equals(
                                property,
                                StringComparison.OrdinalIgnoreCase)))
                        {
                            links.Add(new RedshiftMaterialTextureLink
                            {
                                MaterialPath = materialPath,
                                PropertyName = property
                            });
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return result;
        }

        private static Dictionary<string, List<string>>
            BuildMaterialTextureLists(
                Dictionary<string, List<RedshiftMaterialTextureLink>> textureLinks)
        {
            var result = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, List<RedshiftMaterialTextureLink>> pair
                     in textureLinks)
            {
                foreach (RedshiftMaterialTextureLink link in pair.Value)
                {
                    if (!result.TryGetValue(
                        link.MaterialPath,
                        out List<string> textures))
                    {
                        textures = new List<string>();
                        result.Add(link.MaterialPath, textures);
                    }

                    if (!textures.Contains(
                        pair.Key,
                        StringComparer.OrdinalIgnoreCase))
                    {
                        textures.Add(pair.Key);
                    }
                }
            }

            return result;
        }

        private static string CanonicalParent(
            string assetPath,
            IEnumerable<string> organisationalFolders)
        {
            string directory = GetDirectory(assetPath);
            string folder = LastSegment(directory);

            if ((organisationalFolders ?? Enumerable.Empty<string>())
                .Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                folder = LastSegment(GetDirectory(directory));
            }

            return folder;
        }

        private static string FolderAbove(
            string assetPath,
            string folderName)
        {
            string[] parts = GetDirectory(assetPath)
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = parts.Length - 1; i > 0; i--)
            {
                if (parts[i].Equals(
                    folderName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return parts[i - 1];
                }
            }

            return string.Empty;
        }

        private static string SecondTextureToken(string texturePath)
        {
            string name = RedshiftNamingPolicy.StripLegacyPrefix(
                Path.GetFileNameWithoutExtension(texturePath),
                false);

            string[] tokens = name.Split(
                new[] { '_' },
                StringSplitOptions.RemoveEmptyEntries);

            return tokens.Length < 2 ? string.Empty : tokens[1];
        }

        private static string MostCommon(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Key)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string InferTextureRole(
            string propertyName,
            string fileName)
        {
            string evidence =
                ((propertyName ?? string.Empty) + (fileName ?? string.Empty))
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (Has(evidence, "basemap", "basecolor", "basecolour", "albedo", "diffuse"))
                return "Albedo";
            if (Has(evidence, "bumpmap", "normalmap", "normalgl", "normaldx", "normal"))
                return "Normal";
            if (Has(evidence, "metallicglossmap", "metallicsmoothness", "metalsmooth", "metallic"))
                return "Metallic";
            if (Has(evidence, "occlusionmap", "ambientocclusion", "occlusion", "ao"))
                return "AO";
            if (Has(evidence, "emissionmap", "emissive", "emission"))
                return "Emission";
            if (Has(evidence, "parallaxmap", "heightmap", "height"))
                return "Height";
            if (Has(evidence, "opacity", "transparency", "alpha"))
                return "Opacity";

            return string.Empty;
        }

        private static bool Has(string source, params string[] values)
        {
            return values.Any(value =>
                source.IndexOf(
                    value,
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string LastSegment(string path)
        {
            string value = (path ?? string.Empty).TrimEnd('/');

            if (string.IsNullOrWhiteSpace(value))
            {
                return "Assets";
            }

            int slash = value.LastIndexOf('/');
            return slash >= 0 ? value.Substring(slash + 1) : value;
        }
    }
}
