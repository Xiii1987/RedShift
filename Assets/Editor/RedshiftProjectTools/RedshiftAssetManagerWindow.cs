using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal sealed class RedshiftAssetManagerWindow : EditorWindow
    {
        private const string StateKey = "Redshift.AssetManagerV1.State";

        private enum ManagerSection
        {
            Overview,
            Models,
            Materials,
            Textures,
            Prefabs,
            Animations,
            Controllers,
            Audio,
            Scenes,
            Scripts,
            Rules
        }

        private enum ManagedAssetType
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

        private enum NamingState
        {
            Compliant,
            Violation,
            Excluded,
            NotGoverned
        }

        [Serializable]
        private sealed class AssetManagerState
        {
            public List<AssetRule> Rules = new List<AssetRule>();
            public List<AssetFlags> Flags = new List<AssetFlags>();
        }

        [Serializable]
        private sealed class AssetRule
        {
            public string Type;
            public bool Enabled;
            public string Prefix;
            public string Pattern;
        }

        [Serializable]
        private sealed class AssetFlags
        {
            public string Guid;
            public bool RuntimeLoaded;
            public bool IgnoreNaming;
            public bool IgnoreUnused;
            public string Note;
        }

        private sealed class AssetRecord
        {
            public string Path;
            public string Guid;
            public string Name;
            public string ParentFolder;
            public ManagedAssetType Type;
            public NamingState Naming;
            public string ExpectedPrefix;
            public string SuggestedName;
            public string SuggestionReason;
            public bool Ambiguous;
            public bool IsUnusedCandidate;
            public AssetFlags Flags;
            public readonly List<MaterialTextureLink> MaterialLinks =
                new List<MaterialTextureLink>();
            public readonly List<string> LinkedTextures =
                new List<string>();
        }

        private sealed class MaterialTextureLink
        {
            public string MaterialPath;
            public string PropertyName;
        }

        private struct ChartSlice
        {
            public readonly string Label;
            public readonly int Value;
            public readonly Color Color;

            public ChartSlice(string label, int value, Color color)
            {
                Label = label;
                Value = value;
                Color = color;
            }
        }

        private static readonly Color CompliantColor =
            new Color(0.20f, 0.68f, 0.34f);
        private static readonly Color ViolationColor =
            new Color(0.82f, 0.27f, 0.23f);
        private static readonly Color ExcludedColor =
            new Color(0.52f, 0.55f, 0.60f);

        private readonly Dictionary<ManagedAssetType, Color> _typeColors =
            new Dictionary<ManagedAssetType, Color>
            {
                { ManagedAssetType.Model, new Color(0.35f, 0.60f, 0.90f) },
                { ManagedAssetType.Material, new Color(0.95f, 0.55f, 0.22f) },
                { ManagedAssetType.Texture, new Color(0.58f, 0.42f, 0.84f) },
                { ManagedAssetType.Prefab, new Color(0.24f, 0.74f, 0.74f) },
                { ManagedAssetType.Animation, new Color(0.90f, 0.40f, 0.62f) },
                { ManagedAssetType.Controller, new Color(0.78f, 0.47f, 0.30f) },
                { ManagedAssetType.OverrideController, new Color(0.72f, 0.40f, 0.32f) },
                { ManagedAssetType.AvatarMask, new Color(0.68f, 0.50f, 0.28f) },
                { ManagedAssetType.Audio, new Color(0.45f, 0.72f, 0.32f) },
                { ManagedAssetType.Scene, new Color(0.82f, 0.72f, 0.25f) },
                { ManagedAssetType.Shader, new Color(0.38f, 0.48f, 0.82f) },
                { ManagedAssetType.Script, new Color(0.45f, 0.58f, 0.68f) },
                { ManagedAssetType.Other, new Color(0.50f, 0.50f, 0.50f) }
            };

        private RedshiftToolSettings _toolSettings;
        private AssetManagerState _state;
        private readonly List<AssetRecord> _records = new List<AssetRecord>();
        private readonly Dictionary<string, AssetFlags> _flagsByGuid =
            new Dictionary<string, AssetFlags>(StringComparer.OrdinalIgnoreCase);

        private ManagerSection _section;
        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _violationsOnly;
        private bool _analysisHasRun;
        private string _lastScanSummary = "No analysis run yet.";

        [MenuItem("Redshift/Asset Manager")]
        private static void OpenWindow()
        {
            var window = GetWindow<RedshiftAssetManagerWindow>(
                "Redshift Asset Manager");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            _toolSettings = new RedshiftToolSettings();
            _toolSettings.Load();
            LoadState();
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.HelpBox(
                "Asset Manager V1 is report-only. It discovers naming compliance, " +
                "material/texture relationships, unused candidates and GUID-based " +
                "exceptions. It does not rename, move or delete assets.",
                MessageType.Info);

            DrawSectionPicker();
            EditorGUILayout.Space(6f);

            if (_section == ManagerSection.Rules)
            {
                DrawRules();
                return;
            }

            if (!_analysisHasRun)
            {
                DrawNoAnalysisState();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_section == ManagerSection.Overview)
            {
                DrawOverview();
            }
            else
            {
                DrawAssetSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                "REDSHIFT ASSET MANAGER — V1",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                _lastScanSummary,
                EditorStyles.miniLabel);

            if (GUILayout.Button(
                "Run Asset Analysis",
                EditorStyles.toolbarButton,
                GUILayout.Width(125f)))
            {
                RunAnalysis();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSectionPicker()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                "Section",
                GUILayout.Width(55f));

            _section = (ManagerSection)EditorGUILayout.EnumPopup(
                _section,
                GUILayout.Width(190f));

            GUILayout.Space(8f);

            if (_section != ManagerSection.Overview)
            {
                _search = EditorGUILayout.TextField(
                    "Search",
                    _search);

                _violationsOnly = EditorGUILayout.ToggleLeft(
                    "Violations only",
                    _violationsOnly,
                    GUILayout.Width(110f));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoAnalysisState()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "NO ASSET MANAGER REPORT YET",
                GetCenteredBoldStyle(16));

            EditorGUILayout.LabelField(
                "Run an analysis to inventory governed assets, inspect material " +
                "texture links and compare assets against the current naming rules.",
                GetCenteredWrappedStyle());

            EditorGUILayout.Space(8f);

            if (GUILayout.Button(
                "Run Asset Analysis",
                GUILayout.Height(34f)))
            {
                RunAnalysis();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            List<AssetRecord> governed = _records
                .Where(record => record.Naming != NamingState.NotGoverned)
                .ToList();

            int compliant = governed.Count(
                record => record.Naming == NamingState.Compliant);
            int violations = governed.Count(
                record => record.Naming == NamingState.Violation);
            int excluded = governed.Count(
                record => record.Naming == NamingState.Excluded);
            int total = governed.Count;

            float compliance = total <= 0
                ? 100f
                : compliant / (float)total * 100f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "NAMING HEALTH",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                compliance.ToString("0.0") + "%",
                GetCenteredBoldStyle(28));
            EditorGUILayout.LabelField(
                "ASSET MANAGER NAMING COMPLIANCE",
                GetCenteredMiniBoldStyle());

            Rect progressRect = GUILayoutUtility.GetRect(
                10f,
                22f,
                GUILayout.ExpandWidth(true));

            EditorGUI.ProgressBar(
                progressRect,
                Mathf.Clamp01(compliance / 100f),
                compliant + " compliant  •  " +
                violations + " violations  •  " +
                excluded + " excluded  •  " +
                total + " governed");

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawMetric("Compliant", compliant.ToString());
            DrawMetric("Violations", violations.ToString());
            DrawMetric("Excluded", excluded.ToString());
            DrawMetric("Inventory", _records.Count.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Naming Visualisation",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(360f));

            EditorGUILayout.LabelField(
                "Naming Progress",
                GetCenteredBoldStyle(13));

            Rect progressChartRect = GUILayoutUtility.GetRect(
                250f,
                250f,
                GUILayout.ExpandWidth(true));

            DrawDonutChart(
                progressChartRect,
                new[]
                {
                    new ChartSlice("Compliant", compliant, CompliantColor),
                    new ChartSlice("Violations", violations, ViolationColor),
                    new ChartSlice("Excluded", excluded, ExcludedColor)
                },
                compliance.ToString("0.0") + "%",
                "compliant");

            DrawLegendRow(
                "Compliant",
                compliant,
                total,
                CompliantColor);
            DrawLegendRow(
                "Violations",
                violations,
                total,
                ViolationColor);
            DrawLegendRow(
                "Excluded",
                excluded,
                total,
                ExcludedColor);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(360f));

            EditorGUILayout.LabelField(
                "Violation Breakdown",
                GetCenteredBoldStyle(13));

            var violationGroups = _records
                .Where(record => record.Naming == NamingState.Violation)
                .GroupBy(record => record.Type)
                .OrderByDescending(group => group.Count())
                .ToList();

            int violationTotal = violationGroups.Sum(group => group.Count());

            Rect breakdownRect = GUILayoutUtility.GetRect(
                250f,
                250f,
                GUILayout.ExpandWidth(true));

            DrawDonutChart(
                breakdownRect,
                violationGroups
                    .Select(group => new ChartSlice(
                        FriendlyTypeName(group.Key),
                        group.Count(),
                        GetTypeColor(group.Key)))
                    .ToList(),
                violationTotal.ToString(),
                "violations");

            foreach (IGrouping<ManagedAssetType, AssetRecord> group
                     in violationGroups)
            {
                DrawLegendRow(
                    FriendlyTypeName(group.Key),
                    group.Count(),
                    violationTotal,
                    GetTypeColor(group.Key));
            }

            if (violationGroups.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "No naming violations found.",
                    GetCenteredMiniBoldStyle());
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);
            DrawOverviewTable();
        }

        private void DrawOverviewTable()
        {
            EditorGUILayout.LabelField(
                "Inventory by Type",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (IGrouping<ManagedAssetType, AssetRecord> group
                     in _records
                         .GroupBy(record => record.Type)
                         .OrderBy(group => FriendlyTypeName(group.Key)))
            {
                int governed = group.Count(
                    record => record.Naming != NamingState.NotGoverned);
                int compliant = group.Count(
                    record => record.Naming == NamingState.Compliant);
                int violations = group.Count(
                    record => record.Naming == NamingState.Violation);
                int excluded = group.Count(
                    record => record.Naming == NamingState.Excluded);

                EditorGUILayout.BeginHorizontal();

                Rect swatch = GUILayoutUtility.GetRect(
                    12f,
                    12f,
                    GUILayout.Width(12f),
                    GUILayout.Height(12f));
                EditorGUI.DrawRect(swatch, GetTypeColor(group.Key));

                GUILayout.Space(5f);

                EditorGUILayout.LabelField(
                    FriendlyTypeName(group.Key),
                    GUILayout.Width(145f));

                GUILayout.Label(
                    group.Count() + " assets",
                    GUILayout.Width(85f));

                GUILayout.Label(
                    governed == 0
                        ? "not governed"
                        : compliant + " valid / " +
                          violations + " violations / " +
                          excluded + " excluded",
                    EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            int unusedTextures = _records.Count(
                record =>
                    record.Type == ManagedAssetType.Texture &&
                    record.IsUnusedCandidate &&
                    !record.Flags.RuntimeLoaded &&
                    !record.Flags.IgnoreUnused);

            int linkedTextures = _records.Count(
                record =>
                    record.Type == ManagedAssetType.Texture &&
                    record.MaterialLinks.Count > 0);

            int unlinkedTextures = _records.Count(
                record =>
                    record.Type == ManagedAssetType.Texture &&
                    record.MaterialLinks.Count == 0);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Texture Usage Snapshot",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawMetric("Material-linked textures", linkedTextures.ToString());
            DrawMetric("No material link", unlinkedTextures.ToString());
            DrawMetric("Unused candidates", unusedTextures.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "\"No material link\" does not mean unused. UI, scripts, sprites, " +
                "runtime loading and other systems can use textures without a material. " +
                "Unused Candidate comes from the conservative dependency scan.",
                MessageType.None);
        }

        private void DrawAssetSection()
        {
            ManagedAssetType[] types = TypesForSection(_section);

            List<AssetRecord> visible = _records
                .Where(record => types.Contains(record.Type))
                .Where(record =>
                    !_violationsOnly ||
                    record.Naming == NamingState.Violation)
                .Where(record =>
                    string.IsNullOrWhiteSpace(_search) ||
                    record.Path.IndexOf(
                        _search,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    record.Name.IndexOf(
                        _search,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(record => record.Path)
                .ToList();

            int total = _records.Count(record => types.Contains(record.Type));
            int violations = _records.Count(record =>
                types.Contains(record.Type) &&
                record.Naming == NamingState.Violation);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                _section + " Report",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                visible.Count + " visible  •  " +
                violations + " violation(s)  •  " +
                total + " total",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.EndHorizontal();

            if (_section == ManagerSection.Textures)
            {
                EditorGUILayout.HelpBox(
                    "Texture suggestions use material links and texture-role evidence. " +
                    "AMBIGUOUS means the manager cannot confidently infer a canonical " +
                    "name and requires review. Runtime Loaded and Ignore Unused flags " +
                    "suppress unused warnings but do not excuse naming.",
                    MessageType.Info);
            }

            foreach (AssetRecord record in visible)
            {
                DrawAssetRecord(record);
            }
        }

        private void DrawAssetRecord(AssetRecord record)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            Rect swatch = GUILayoutUtility.GetRect(
                12f,
                12f,
                GUILayout.Width(12f),
                GUILayout.Height(12f));

            EditorGUI.DrawRect(
                swatch,
                NamingColor(record.Naming));

            GUILayout.Space(5f);

            GUILayout.Label(
                record.Name,
                EditorStyles.boldLabel,
                GUILayout.MinWidth(220f));

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                NamingLabel(record.Naming),
                EditorStyles.miniBoldLabel,
                GUILayout.Width(100f));

            if (GUILayout.Button(
                "Ping",
                GUILayout.Width(50f)))
            {
                RedshiftAssetUtility.PingAsset(record.Path);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                record.Path,
                EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(
                "Folder: " + record.ParentFolder,
                EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrWhiteSpace(record.ExpectedPrefix))
            {
                GUILayout.Label(
                    "Rule: " + record.ExpectedPrefix + "…",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(record.SuggestedName) &&
                !record.SuggestedName.Equals(
                    record.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    record.Ambiguous
                        ? "Suggested (review):"
                        : "Suggested:",
                    GUILayout.Width(115f));

                GUILayout.Label(
                    record.SuggestedName,
                    EditorStyles.boldLabel);

                EditorGUILayout.EndHorizontal();
            }

            if (record.Ambiguous)
            {
                EditorGUILayout.HelpBox(
                    "AMBIGUOUS — REVIEW REQUIRED\n" +
                    record.SuggestionReason,
                    MessageType.Warning);
            }
            else if (!string.IsNullOrWhiteSpace(record.SuggestionReason))
            {
                EditorGUILayout.LabelField(
                    record.SuggestionReason,
                    EditorStyles.wordWrappedMiniLabel);
            }

            if (record.Type == ManagedAssetType.Texture)
            {
                DrawTextureDetails(record);
            }
            else if (record.Type == ManagedAssetType.Material)
            {
                DrawMaterialDetails(record);
            }

            DrawFlags(record);

            EditorGUILayout.EndVertical();
        }

        private void DrawTextureDetails(AssetRecord record)
        {
            EditorGUILayout.Space(4f);

            if (record.MaterialLinks.Count > 0)
            {
                EditorGUILayout.LabelField(
                    "Material Links",
                    EditorStyles.miniBoldLabel);

                foreach (MaterialTextureLink link in record.MaterialLinks)
                {
                    string materialName =
                        Path.GetFileNameWithoutExtension(link.MaterialPath);

                    EditorGUILayout.LabelField(
                        "• " + materialName +
                        "   [" + link.PropertyName + "]",
                        EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Material links: none detected",
                    EditorStyles.miniLabel);
            }

            bool suppressed =
                record.Flags.RuntimeLoaded ||
                record.Flags.IgnoreUnused;

            if (record.IsUnusedCandidate)
            {
                EditorGUILayout.HelpBox(
                    suppressed
                        ? "Unused candidate suppressed by asset flag."
                        : "UNUSED CANDIDATE — no dependency was found from the " +
                          "configured scan roots. Review before quarantine/deletion.",
                    suppressed
                        ? MessageType.None
                        : MessageType.Warning);
            }
        }

        private void DrawMaterialDetails(AssetRecord record)
        {
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField(
                "Linked Textures: " + record.LinkedTextures.Count,
                EditorStyles.miniBoldLabel);

            foreach (string texturePath in record.LinkedTextures.Take(8))
            {
                EditorGUILayout.LabelField(
                    "• " + Path.GetFileName(texturePath),
                    EditorStyles.miniLabel);
            }

            if (record.LinkedTextures.Count > 8)
            {
                EditorGUILayout.LabelField(
                    "… and " + (record.LinkedTextures.Count - 8) + " more",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawFlags(AssetRecord record)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Asset Flags (GUID-based)",
                EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            bool runtime = EditorGUILayout.ToggleLeft(
                "Runtime Loaded",
                record.Flags.RuntimeLoaded,
                GUILayout.Width(120f));

            bool ignoreNaming = EditorGUILayout.ToggleLeft(
                "Ignore Naming",
                record.Flags.IgnoreNaming,
                GUILayout.Width(120f));

            bool ignoreUnused = EditorGUILayout.ToggleLeft(
                "Ignore Unused",
                record.Flags.IgnoreUnused,
                GUILayout.Width(120f));

            EditorGUILayout.EndHorizontal();

            string note = EditorGUILayout.TextField(
                "Note",
                record.Flags.Note ?? string.Empty);

            if (EditorGUI.EndChangeCheck())
            {
                record.Flags.RuntimeLoaded = runtime;
                record.Flags.IgnoreNaming = ignoreNaming;
                record.Flags.IgnoreUnused = ignoreUnused;
                record.Flags.Note = note;

                RecalculateRecord(record);
                SaveState();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRules()
        {
            EditorGUILayout.HelpBox(
                "These are the Asset Manager V1 naming rules. They are stored " +
                "locally in EditorPrefs and can be changed without editing code. " +
                "V1 only reports against them; it does not rename assets.",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (AssetRule rule in _state.Rules)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();

                rule.Enabled = EditorGUILayout.Toggle(
                    "Enabled",
                    rule.Enabled);

                EditorGUILayout.LabelField(
                    "Asset Type",
                    rule.Type);

                rule.Prefix = EditorGUILayout.TextField(
                    "Prefix",
                    rule.Prefix ?? string.Empty);

                rule.Pattern = EditorGUILayout.TextField(
                    "Pattern",
                    rule.Pattern ?? string.Empty);

                if (EditorGUI.EndChangeCheck())
                {
                    SaveState();

                    if (_analysisHasRun)
                    {
                        RecalculateAllNaming();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "Reset Default Rules",
                GUILayout.Height(28f),
                GUILayout.Width(145f)))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset naming rules?",
                    "Restore the Asset Manager V1 default rule list?",
                    "Reset",
                    "Cancel"))
                {
                    _state.Rules = CreateDefaultRules();
                    SaveState();

                    if (_analysisHasRun)
                    {
                        RecalculateAllNaming();
                    }
                }
            }

            if (GUILayout.Button(
                "Save Rules",
                GUILayout.Height(28f),
                GUILayout.Width(100f)))
            {
                SaveState();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);

            EditorGUILayout.HelpBox(
                "Texture role vocabulary in V1: Albedo, Normal, Metallic, AO, " +
                "Emission, Height and Opacity. Common aliases such as BaseColor, " +
                "Diffuse, AlbedoTransparency, NormalGL/DX, MetallicSmoothness and " +
                "AmbientOcclusion are normalised for suggestions. Unknown or " +
                "conflicting evidence remains AMBIGUOUS.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private void RunAnalysis()
        {
            _records.Clear();

            string[] allAssets =
                RedshiftAssetUtility.GetProjectAssetPaths();

            Dictionary<string, List<MaterialTextureLink>> textureLinks =
                BuildTextureMaterialLinks(allAssets);

            Dictionary<string, List<string>> materialTextures =
                BuildMaterialTextureLists(textureLinks);

            var unusedTexturePaths =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                RedshiftUnusedScanResult unusedResult =
                    RedshiftUnusedAssetScanner.Scan(_toolSettings);

                foreach (RedshiftUnusedCandidate candidate
                         in unusedResult.Candidates)
                {
                    if (candidate.Category.Equals(
                        "Texture",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        unusedTexturePaths.Add(candidate.AssetPath);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Redshift Asset Manager could not complete the unused " +
                    "candidate scan. " + exception.Message);
            }

            foreach (string path in allAssets)
            {
                ManagedAssetType type = GetManagedAssetType(path);

                if (type == ManagedAssetType.Other)
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                AssetFlags flags = GetOrCreateFlags(guid);

                var record = new AssetRecord
                {
                    Path = path,
                    Guid = guid,
                    Name = Path.GetFileNameWithoutExtension(path),
                    ParentFolder = GetParentFolder(path),
                    Type = type,
                    Flags = flags,
                    IsUnusedCandidate = unusedTexturePaths.Contains(path)
                };

                if (textureLinks.TryGetValue(
                    path,
                    out List<MaterialTextureLink> links))
                {
                    record.MaterialLinks.AddRange(links);
                }

                if (materialTextures.TryGetValue(
                    path,
                    out List<string> textures))
                {
                    record.LinkedTextures.AddRange(textures);
                }

                RecalculateRecord(record);
                _records.Add(record);
            }

            _records.Sort((left, right) =>
                string.Compare(
                    left.Path,
                    right.Path,
                    StringComparison.OrdinalIgnoreCase));

            _analysisHasRun = true;
            _lastScanSummary =
                _records.Count + " managed assets analysed";

            Repaint();
        }

        private Dictionary<string, List<MaterialTextureLink>>
            BuildTextureMaterialLinks(IEnumerable<string> allAssets)
        {
            var result =
                new Dictionary<string, List<MaterialTextureLink>>(
                    StringComparer.OrdinalIgnoreCase);

            string[] materialPaths = allAssets
                .Where(path => path.EndsWith(
                    ".mat",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

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

                string[] propertyNames;

                try
                {
                    propertyNames = material.GetTexturePropertyNames();
                }
                catch
                {
                    continue;
                }

                foreach (string propertyName in propertyNames)
                {
                    Texture texture = material.GetTexture(propertyName);

                    if (texture == null)
                    {
                        continue;
                    }

                    string texturePath =
                        AssetDatabase.GetAssetPath(texture);

                    if (string.IsNullOrWhiteSpace(texturePath) ||
                        !texturePath.StartsWith(
                            "Assets/",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!result.TryGetValue(
                        texturePath,
                        out List<MaterialTextureLink> links))
                    {
                        links = new List<MaterialTextureLink>();
                        result.Add(texturePath, links);
                    }

                    if (!links.Any(link =>
                        link.MaterialPath.Equals(
                            materialPath,
                            StringComparison.OrdinalIgnoreCase) &&
                        link.PropertyName.Equals(
                            propertyName,
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        links.Add(new MaterialTextureLink
                        {
                            MaterialPath = materialPath,
                            PropertyName = propertyName
                        });
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            return result;
        }

        private static Dictionary<string, List<string>>
            BuildMaterialTextureLists(
                Dictionary<string, List<MaterialTextureLink>> textureLinks)
        {
            var result =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, List<MaterialTextureLink>> pair
                     in textureLinks)
            {
                string texturePath = pair.Key;

                foreach (MaterialTextureLink link in pair.Value)
                {
                    if (!result.TryGetValue(
                        link.MaterialPath,
                        out List<string> textures))
                    {
                        textures = new List<string>();
                        result.Add(link.MaterialPath, textures);
                    }

                    if (!textures.Contains(
                        texturePath,
                        StringComparer.OrdinalIgnoreCase))
                    {
                        textures.Add(texturePath);
                    }
                }
            }

            foreach (List<string> textures in result.Values)
            {
                textures.Sort(StringComparer.OrdinalIgnoreCase);
            }

            return result;
        }

        private void RecalculateAllNaming()
        {
            foreach (AssetRecord record in _records)
            {
                RecalculateRecord(record);
            }

            Repaint();
        }

        private void RecalculateRecord(AssetRecord record)
        {
            AssetRule rule = GetRule(record.Type);

            record.ExpectedPrefix = rule == null
                ? string.Empty
                : rule.Prefix ?? string.Empty;

            record.SuggestedName = string.Empty;
            record.SuggestionReason = string.Empty;
            record.Ambiguous = false;

            if (record.Flags.IgnoreNaming)
            {
                record.Naming = NamingState.Excluded;
            }
            else if (rule == null || !rule.Enabled)
            {
                record.Naming = NamingState.NotGoverned;
            }
            else
            {
                bool complies = string.IsNullOrEmpty(rule.Prefix) ||
                    record.Name.StartsWith(
                        rule.Prefix,
                        StringComparison.OrdinalIgnoreCase);

                record.Naming = complies
                    ? NamingState.Compliant
                    : NamingState.Violation;
            }

            if (record.Type == ManagedAssetType.Texture)
            {
                BuildTextureSuggestion(record, rule);
            }
            else if (rule != null &&
                     rule.Enabled &&
                     record.Naming == NamingState.Violation)
            {
                string baseName =
                    StripKnownPrefix(record.Name);

                record.SuggestedName =
                    (rule.Prefix ?? string.Empty) + baseName;

                record.SuggestionReason =
                    "Low-risk prefix suggestion only. Folder structure and " +
                    "canonical asset vocabulary are not inferred for this " +
                    "asset type in V1.";
            }
        }

        private void BuildTextureSuggestion(
            AssetRecord record,
            AssetRule rule)
        {
            if (rule == null || !rule.Enabled)
            {
                return;
            }

            List<string> materialBases = record.MaterialLinks
                .Select(link =>
                    StripKnownPrefix(
                        Path.GetFileNameWithoutExtension(
                            link.MaterialPath)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<string> roles = new List<string>();

            foreach (MaterialTextureLink link in record.MaterialLinks)
            {
                string role = InferTextureRole(
                    link.PropertyName,
                    record.Name);

                if (!string.IsNullOrWhiteSpace(role) &&
                    !roles.Contains(
                        role,
                        StringComparer.OrdinalIgnoreCase))
                {
                    roles.Add(role);
                }
            }

            if (roles.Count == 0)
            {
                string filenameRole =
                    InferTextureRole(string.Empty, record.Name);

                if (!string.IsNullOrWhiteSpace(filenameRole))
                {
                    roles.Add(filenameRole);
                }
            }

            if (materialBases.Count == 1 && roles.Count == 1)
            {
                record.SuggestedName =
                    (rule.Prefix ?? "T_") +
                    materialBases[0] +
                    "_" +
                    roles[0];

                record.SuggestionReason =
                    "High-confidence texture suggestion from linked material " +
                    "\"" + materialBases[0] + "\" and detected texture role " +
                    "\"" + roles[0] + "\".";
                record.Ambiguous = false;
                return;
            }

            record.Ambiguous = true;

            if (materialBases.Count == 0 && roles.Count == 0)
            {
                record.SuggestionReason =
                    "No material link and no recognised texture role were found.";
            }
            else if (materialBases.Count == 0)
            {
                record.SuggestionReason =
                    "Texture role \"" +
                    string.Join(", ", roles) +
                    "\" was detected, but no linked material identifies the " +
                    "canonical asset name.";
            }
            else if (materialBases.Count > 1)
            {
                record.SuggestionReason =
                    "Texture is referenced by multiple material names: " +
                    string.Join(", ", materialBases) +
                    ". A canonical owner must be chosen.";
            }
            else if (roles.Count > 1)
            {
                record.SuggestionReason =
                    "Conflicting texture-role evidence was found: " +
                    string.Join(", ", roles) + ".";
            }
            else
            {
                record.SuggestionReason =
                    "The manager has incomplete evidence for a deterministic " +
                    "texture name.";
            }

            string fallbackBase =
                materialBases.Count == 1
                    ? materialBases[0]
                    : StripKnownPrefix(record.Name);

            string fallbackRole =
                roles.Count == 1
                    ? "_" + roles[0]
                    : string.Empty;

            record.SuggestedName =
                (rule.Prefix ?? "T_") +
                fallbackBase +
                fallbackRole;
        }

        private static string InferTextureRole(
            string propertyName,
            string fileName)
        {
            string evidence =
                ((propertyName ?? string.Empty) + " " +
                 (fileName ?? string.Empty))
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (ContainsAny(
                evidence,
                "basemap",
                "basecolor",
                "basecolour",
                "albedo",
                "diffuse"))
            {
                return "Albedo";
            }

            if (ContainsAny(
                evidence,
                "bumpmap",
                "normalmap",
                "normalgl",
                "normaldx",
                "normal"))
            {
                return "Normal";
            }

            if (ContainsAny(
                evidence,
                "metallicglossmap",
                "metallicsmoothness",
                "metalsmooth",
                "metallic"))
            {
                return "Metallic";
            }

            if (ContainsAny(
                evidence,
                "occlusionmap",
                "ambientocclusion",
                "occlusion",
                "ao"))
            {
                return "AO";
            }

            if (ContainsAny(
                evidence,
                "emissionmap",
                "emissive",
                "emission"))
            {
                return "Emission";
            }

            if (ContainsAny(
                evidence,
                "parallaxmap",
                "heightmap",
                "height"))
            {
                return "Height";
            }

            if (ContainsAny(
                evidence,
                "opacity",
                "transparency",
                "alpha"))
            {
                return "Opacity";
            }

            return string.Empty;
        }

        private static bool ContainsAny(
            string source,
            params string[] values)
        {
            foreach (string value in values)
            {
                if (source.IndexOf(
                    value,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private AssetRule GetRule(ManagedAssetType type)
        {
            string typeName = type.ToString();

            return _state.Rules.FirstOrDefault(rule =>
                string.Equals(
                    rule.Type,
                    typeName,
                    StringComparison.OrdinalIgnoreCase));
        }

        private AssetFlags GetOrCreateFlags(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return new AssetFlags();
            }

            if (_flagsByGuid.TryGetValue(
                guid,
                out AssetFlags flags))
            {
                return flags;
            }

            flags = new AssetFlags
            {
                Guid = guid,
                Note = string.Empty
            };

            _state.Flags.Add(flags);
            _flagsByGuid.Add(guid, flags);

            return flags;
        }

        private void LoadState()
        {
            string json = EditorPrefs.GetString(
                StateKey,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    _state =
                        JsonUtility.FromJson<AssetManagerState>(json);
                }
                catch
                {
                    _state = null;
                }
            }

            if (_state == null)
            {
                _state = new AssetManagerState();
            }

            if (_state.Rules == null || _state.Rules.Count == 0)
            {
                _state.Rules = CreateDefaultRules();
            }

            if (_state.Flags == null)
            {
                _state.Flags = new List<AssetFlags>();
            }

            _flagsByGuid.Clear();

            foreach (AssetFlags flags in _state.Flags)
            {
                if (flags == null ||
                    string.IsNullOrWhiteSpace(flags.Guid))
                {
                    continue;
                }

                _flagsByGuid[flags.Guid] = flags;
            }

            EnsureAllDefaultRuleTypesExist();
        }

        private void EnsureAllDefaultRuleTypesExist()
        {
            foreach (AssetRule defaultRule in CreateDefaultRules())
            {
                if (_state.Rules.Any(rule =>
                    string.Equals(
                        rule.Type,
                        defaultRule.Type,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _state.Rules.Add(defaultRule);
            }
        }

        private void SaveState()
        {
            string json = JsonUtility.ToJson(
                _state,
                false);

            EditorPrefs.SetString(
                StateKey,
                json);
        }

        private static List<AssetRule> CreateDefaultRules()
        {
            return new List<AssetRule>
            {
                Rule(ManagedAssetType.Model, true, "M_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Material, true, "MAT_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Texture, true, "T_", "{Prefix}{Asset}_{TextureRole}"),
                Rule(ManagedAssetType.Prefab, true, "PF_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Animation, true, "ANIM_", "{Prefix}{Character}_{Action}"),
                Rule(ManagedAssetType.Controller, true, "AC_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.OverrideController, true, "AOC_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.AvatarMask, true, "MASK_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Audio, true, "AUD_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Scene, true, "SCN_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Shader, true, "SH_", "{Prefix}{Asset}"),
                Rule(ManagedAssetType.Script, false, "", "{ClassName}")
            };
        }

        private static AssetRule Rule(
            ManagedAssetType type,
            bool enabled,
            string prefix,
            string pattern)
        {
            return new AssetRule
            {
                Type = type.ToString(),
                Enabled = enabled,
                Prefix = prefix,
                Pattern = pattern
            };
        }

        private static ManagedAssetType GetManagedAssetType(
            string path)
        {
            string extension =
                Path.GetExtension(path).ToLowerInvariant();

            switch (extension)
            {
                case ".fbx":
                case ".obj":
                case ".blend":
                    return ManagedAssetType.Model;

                case ".mat":
                    return ManagedAssetType.Material;

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".exr":
                case ".hdr":
                case ".tif":
                case ".tiff":
                    return ManagedAssetType.Texture;

                case ".prefab":
                    return ManagedAssetType.Prefab;

                case ".anim":
                    return ManagedAssetType.Animation;

                case ".controller":
                    return ManagedAssetType.Controller;

                case ".overridecontroller":
                    return ManagedAssetType.OverrideController;

                case ".mask":
                    return ManagedAssetType.AvatarMask;

                case ".wav":
                case ".mp3":
                case ".ogg":
                case ".aif":
                case ".aiff":
                    return ManagedAssetType.Audio;

                case ".unity":
                    return ManagedAssetType.Scene;

                case ".shader":
                case ".shadergraph":
                case ".compute":
                    return ManagedAssetType.Shader;

                case ".cs":
                    return ManagedAssetType.Script;

                default:
                    return ManagedAssetType.Other;
            }
        }

        private static ManagedAssetType[] TypesForSection(
            ManagerSection section)
        {
            switch (section)
            {
                case ManagerSection.Models:
                    return new[] { ManagedAssetType.Model };

                case ManagerSection.Materials:
                    return new[] { ManagedAssetType.Material };

                case ManagerSection.Textures:
                    return new[] { ManagedAssetType.Texture };

                case ManagerSection.Prefabs:
                    return new[] { ManagedAssetType.Prefab };

                case ManagerSection.Animations:
                    return new[]
                    {
                        ManagedAssetType.Animation,
                        ManagedAssetType.AvatarMask
                    };

                case ManagerSection.Controllers:
                    return new[]
                    {
                        ManagedAssetType.Controller,
                        ManagedAssetType.OverrideController
                    };

                case ManagerSection.Audio:
                    return new[] { ManagedAssetType.Audio };

                case ManagerSection.Scenes:
                    return new[]
                    {
                        ManagedAssetType.Scene,
                        ManagedAssetType.Shader
                    };

                case ManagerSection.Scripts:
                    return new[] { ManagedAssetType.Script };

                default:
                    return Array.Empty<ManagedAssetType>();
            }
        }

        private static string StripKnownPrefix(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string[] prefixes =
            {
                "MAT_",
                "ANIM_",
                "AOC_",
                "MASK_",
                "AUD_",
                "SCN_",
                "CSH_",
                "AC_",
                "PF_",
                "SH_",
                "T_",
                "M_"
            };

            foreach (string prefix in prefixes
                         .OrderByDescending(value => value.Length))
            {
                if (name.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(prefix.Length);
                }
            }

            return name;
        }

        private static string GetParentFolder(string path)
        {
            string directory =
                Path.GetDirectoryName(path)
                    ?.Replace('\\', '/') ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(directory))
            {
                return "Assets";
            }

            int slash = directory.LastIndexOf('/');

            return slash >= 0
                ? directory.Substring(slash + 1)
                : directory;
        }

        private Color GetTypeColor(ManagedAssetType type)
        {
            return _typeColors.TryGetValue(
                type,
                out Color color)
                ? color
                : Color.gray;
        }

        private static Color NamingColor(NamingState state)
        {
            switch (state)
            {
                case NamingState.Compliant:
                    return CompliantColor;
                case NamingState.Violation:
                    return ViolationColor;
                case NamingState.Excluded:
                    return ExcludedColor;
                default:
                    return new Color(0.42f, 0.45f, 0.48f);
            }
        }

        private static string NamingLabel(NamingState state)
        {
            switch (state)
            {
                case NamingState.Compliant:
                    return "COMPLIANT";
                case NamingState.Violation:
                    return "VIOLATION";
                case NamingState.Excluded:
                    return "EXCLUDED";
                default:
                    return "NOT GOVERNED";
            }
        }

        private static string FriendlyTypeName(ManagedAssetType type)
        {
            switch (type)
            {
                case ManagedAssetType.OverrideController:
                    return "Animator Override";
                case ManagedAssetType.AvatarMask:
                    return "Avatar Mask";
                default:
                    return type.ToString();
            }
        }

        private static void DrawMetric(
            string label,
            string value)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(125f));

            EditorGUILayout.LabelField(
                value,
                GetCenteredBoldStyle(18));

            EditorGUILayout.LabelField(
                label,
                GetCenteredMiniBoldStyle());

            EditorGUILayout.EndVertical();
        }

        private static void DrawDonutChart(
            Rect rect,
            IReadOnlyList<ChartSlice> slices,
            string centreValue,
            string centreLabel)
        {
            float radius =
                Mathf.Min(rect.width, rect.height) * 0.40f;

            Vector3 centre =
                new Vector3(
                    rect.center.x,
                    rect.center.y,
                    0f);

            float total =
                slices == null
                    ? 0f
                    : slices.Sum(
                        slice => Mathf.Max(0, slice.Value));

            Handles.BeginGUI();

            Color previousColor = Handles.color;

            if (total <= 0f)
            {
                Handles.color =
                    new Color(0.40f, 0.40f, 0.40f, 0.55f);

                Handles.DrawSolidDisc(
                    centre,
                    Vector3.forward,
                    radius);
            }
            else
            {
                Vector3 startDirection = Vector3.up;

                foreach (ChartSlice slice in slices)
                {
                    if (slice.Value <= 0)
                    {
                        continue;
                    }

                    float sweep =
                        slice.Value / total * 360f;

                    Handles.color = slice.Color;

                    Handles.DrawSolidArc(
                        centre,
                        Vector3.forward,
                        startDirection,
                        sweep,
                        radius);

                    startDirection =
                        Quaternion.AngleAxis(
                            sweep,
                            Vector3.forward) *
                        startDirection;
                }
            }

            Handles.color =
                EditorGUIUtility.isProSkin
                    ? new Color(0.18f, 0.18f, 0.18f, 1f)
                    : new Color(0.82f, 0.82f, 0.82f, 1f);

            Handles.DrawSolidDisc(
                centre,
                Vector3.forward,
                radius * 0.58f);

            Handles.color = previousColor;
            Handles.EndGUI();

            Rect valueRect =
                new Rect(
                    rect.center.x - radius * 0.55f,
                    rect.center.y - 22f,
                    radius * 1.10f,
                    28f);

            Rect labelRect =
                new Rect(
                    rect.center.x - radius * 0.55f,
                    rect.center.y + 6f,
                    radius * 1.10f,
                    20f);

            GUI.Label(
                valueRect,
                centreValue,
                GetCenteredBoldStyle(22));

            GUI.Label(
                labelRect,
                centreLabel,
                GetCenteredMiniBoldStyle());
        }

        private static void DrawLegendRow(
            string label,
            int value,
            int total,
            Color color)
        {
            EditorGUILayout.BeginHorizontal();

            Rect swatch = GUILayoutUtility.GetRect(
                12f,
                12f,
                GUILayout.Width(12f),
                GUILayout.Height(12f));

            EditorGUI.DrawRect(swatch, color);
            GUILayout.Space(4f);

            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();

            float percentage =
                total <= 0
                    ? 0f
                    : value / (float)total * 100f;

            EditorGUILayout.LabelField(
                value + "  (" +
                percentage.ToString("0.0") +
                "%)",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(110f));

            EditorGUILayout.EndHorizontal();
        }

        private static GUIStyle GetCenteredBoldStyle(int fontSize)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize
            };
        }

        private static GUIStyle GetCenteredMiniBoldStyle()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static GUIStyle GetCenteredWrappedStyle()
        {
            return new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
