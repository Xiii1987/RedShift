using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private RedshiftToolSettings _toolSettings;
        private RedshiftAssetManagerState _state;
        private readonly Dictionary<string, RedshiftAssetFlags> _flagsByGuid =
            new Dictionary<string, RedshiftAssetFlags>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _clearedGuids =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<RedshiftManagedAssetRecord> _records =
            new List<RedshiftManagedAssetRecord>();

        private ManagerSection _section;
        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _violationsOnly = true;
        private bool _analysisHasRun;
        private string _lastScanSummary = "No analysis run yet.";
        private string _folderExclusionsText = string.Empty;

        [MenuItem("Redshift/Asset Manager")]
        private static void OpenWindow()
        {
            var window = GetWindow<RedshiftAssetManagerWindow>(
                "Redshift Asset Manager");
            window.minSize = new Vector2(960f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            _toolSettings = new RedshiftToolSettings();
            _toolSettings.Load();
            LoadState();
            RefreshFolderExclusionText();
        }

        private void OnDisable()
        {
            SaveState();
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.HelpBox(
                "Asset Manager V2 is an assisted production checklist. Naming health " +
                "uses the same required-prefix rules and naming exclusions as Project " +
                "Health. Suggestions speed up the work; you still approve every rename.",
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
            GUILayout.Label("REDSHIFT ASSET MANAGER — V2", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(_lastScanSummary, EditorStyles.miniLabel);

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
            EditorGUILayout.LabelField("Section", GUILayout.Width(55f));

            _section = (ManagerSection)EditorGUILayout.EnumPopup(
                _section,
                GUILayout.Width(190f));

            GUILayout.Space(8f);

            if (_section != ManagerSection.Overview &&
                _section != ManagerSection.Rules)
            {
                _search = EditorGUILayout.TextField("Search", _search);

                _violationsOnly = EditorGUILayout.ToggleLeft(
                    "Violations only",
                    _violationsOnly,
                    GUILayout.Width(110f));

                if (GUILayout.Button("Restore Cleared", GUILayout.Width(105f)))
                {
                    _clearedGuids.Clear();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoAnalysisState()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "NO ASSET MANAGER REPORT YET",
                RedshiftAssetManagerGUI.CenteredBold(16));
            EditorGUILayout.LabelField(
                "Run an analysis after restructuring folders. The manager will build " +
                "prefix/material/texture suggestions and a review queue; nothing is " +
                "renamed until you press COMMIT on an individual asset.",
                RedshiftAssetManagerGUI.CenteredWrapped());
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Run Asset Analysis", GUILayout.Height(34f)))
            {
                RunAnalysis();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            List<RedshiftManagedAssetRecord> governed = _records
                .Where(record => record.Naming != RedshiftAssetNamingState.NotGoverned)
                .ToList();

            int compliant = governed.Count(
                record => record.Naming == RedshiftAssetNamingState.Compliant);
            int violations = governed.Count(
                record => record.Naming == RedshiftAssetNamingState.Violation);
            int excluded = governed.Count(
                record => record.Naming == RedshiftAssetNamingState.Excluded);
            int activeTotal = compliant + violations;
            float compliance = activeTotal <= 0
                ? 100f
                : compliant / (float)activeTotal * 100f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("NAMING HEALTH", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                compliance.ToString("0.0") + "%",
                RedshiftAssetManagerGUI.CenteredBold(28));
            EditorGUILayout.LabelField(
                "SHARED WITH PROJECT HEALTH PREFIX COMPLIANCE",
                RedshiftAssetManagerGUI.CenteredMiniBold());

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
                activeTotal + " governed");

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            RedshiftAssetManagerGUI.DrawMetric("Compliant", compliant.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Violations", violations.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Excluded", excluded.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Inventory", _records.Count.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Naming Visualisation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(360f));
            EditorGUILayout.LabelField(
                "Naming Progress",
                RedshiftAssetManagerGUI.CenteredBold(13));

            Rect progressChartRect = GUILayoutUtility.GetRect(
                250f,
                250f,
                GUILayout.ExpandWidth(true));

            RedshiftAssetManagerGUI.DrawDonutChart(
                progressChartRect,
                new[]
                {
                    new RedshiftChartSlice(
                        "Compliant",
                        compliant,
                        RedshiftAssetManagerGUI.CompliantColor),
                    new RedshiftChartSlice(
                        "Violations",
                        violations,
                        RedshiftAssetManagerGUI.ViolationColor)
                },
                compliance.ToString("0.0") + "%",
                "compliant");

            RedshiftAssetManagerGUI.DrawLegendRow(
                "Compliant",
                compliant,
                activeTotal,
                RedshiftAssetManagerGUI.CompliantColor);
            RedshiftAssetManagerGUI.DrawLegendRow(
                "Violations",
                violations,
                activeTotal,
                RedshiftAssetManagerGUI.ViolationColor);
            RedshiftAssetManagerGUI.DrawLegendRow(
                "Excluded",
                excluded,
                Math.Max(1, governed.Count),
                RedshiftAssetManagerGUI.ExcludedColor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(360f));
            EditorGUILayout.LabelField(
                "Violation Breakdown",
                RedshiftAssetManagerGUI.CenteredBold(13));

            var violationGroups = _records
                .Where(record => record.Naming == RedshiftAssetNamingState.Violation)
                .GroupBy(record => record.Type)
                .OrderByDescending(group => group.Count())
                .ToList();

            int violationTotal = violationGroups.Sum(group => group.Count());
            Rect breakdownRect = GUILayoutUtility.GetRect(
                250f,
                250f,
                GUILayout.ExpandWidth(true));

            RedshiftAssetManagerGUI.DrawDonutChart(
                breakdownRect,
                violationGroups
                    .Select(group => new RedshiftChartSlice(
                        RedshiftAssetManagerGUI.FriendlyTypeName(group.Key),
                        group.Count(),
                        RedshiftAssetManagerGUI.TypeColor(group.Key)))
                    .ToList(),
                violationTotal.ToString(),
                "violations");

            foreach (IGrouping<RedshiftManagedAssetType, RedshiftManagedAssetRecord> group
                     in violationGroups)
            {
                RedshiftAssetManagerGUI.DrawLegendRow(
                    RedshiftAssetManagerGUI.FriendlyTypeName(group.Key),
                    group.Count(),
                    violationTotal,
                    RedshiftAssetManagerGUI.TypeColor(group.Key));
            }

            if (violationGroups.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "No naming violations found.",
                    RedshiftAssetManagerGUI.CenteredMiniBold());
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);
            DrawOverviewTable();
        }

        private void DrawOverviewTable()
        {
            EditorGUILayout.LabelField("Inventory by Type", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (IGrouping<RedshiftManagedAssetType, RedshiftManagedAssetRecord> group
                     in _records
                         .GroupBy(record => record.Type)
                         .OrderBy(group =>
                             RedshiftAssetManagerGUI.FriendlyTypeName(group.Key)))
            {
                int compliant = group.Count(
                    record => record.Naming == RedshiftAssetNamingState.Compliant);
                int violations = group.Count(
                    record => record.Naming == RedshiftAssetNamingState.Violation);
                int excluded = group.Count(
                    record => record.Naming == RedshiftAssetNamingState.Excluded);

                EditorGUILayout.BeginHorizontal();
                Rect swatch = GUILayoutUtility.GetRect(
                    12f,
                    12f,
                    GUILayout.Width(12f),
                    GUILayout.Height(12f));
                EditorGUI.DrawRect(
                    swatch,
                    RedshiftAssetManagerGUI.TypeColor(group.Key));
                GUILayout.Space(5f);
                EditorGUILayout.LabelField(
                    RedshiftAssetManagerGUI.FriendlyTypeName(group.Key),
                    GUILayout.Width(145f));
                GUILayout.Label(group.Count() + " assets", GUILayout.Width(85f));

                if (compliant + violations == 0)
                {
                    GUILayout.Label("not governed", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label(
                        compliant + " compliant / " +
                        violations + " violations / " +
                        excluded + " excluded",
                        EditorStyles.miniLabel);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            int linkedTextures = _records.Count(record =>
                record.Type == RedshiftManagedAssetType.Texture &&
                record.MaterialLinks.Count > 0);
            int unlinkedTextures = _records.Count(record =>
                record.Type == RedshiftManagedAssetType.Texture &&
                record.MaterialLinks.Count == 0);
            int unusedTextures = _records.Count(record =>
                record.Type == RedshiftManagedAssetType.Texture &&
                record.IsUnusedCandidate &&
                !record.Flags.RuntimeLoaded &&
                !record.Flags.IgnoreUnused);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Texture Usage Snapshot", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            RedshiftAssetManagerGUI.DrawMetric(
                "Material-linked textures",
                linkedTextures.ToString());
            RedshiftAssetManagerGUI.DrawMetric(
                "No material link",
                unlinkedTextures.ToString());
            RedshiftAssetManagerGUI.DrawMetric(
                "Unused candidates",
                unusedTextures.ToString());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetSection()
        {
            RedshiftManagedAssetType[] types = TypesForSection(_section);
            List<RedshiftManagedAssetRecord> sectionRecords = _records
                .Where(record => types.Contains(record.Type))
                .ToList();

            DrawSectionProgress(sectionRecords);
            EditorGUILayout.Space(8f);

            List<RedshiftManagedAssetRecord> visible = sectionRecords
                .Where(record => !_clearedGuids.Contains(record.Guid))
                .Where(record =>
                    !_violationsOnly ||
                    record.Naming == RedshiftAssetNamingState.Violation)
                .Where(record =>
                    string.IsNullOrWhiteSpace(_search) ||
                    record.Path.IndexOf(
                        _search,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    record.Name.IndexOf(
                        _search,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    record.SuggestedName.IndexOf(
                        _search,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(record => record.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int violations = sectionRecords.Count(record =>
                record.Naming == RedshiftAssetNamingState.Violation);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_section + " Review Queue", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                visible.Count + " visible  •  " +
                violations + " violation(s)  •  " +
                sectionRecords.Count + " total",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            if (visible.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    violations == 0
                        ? "No naming violations remain in this section."
                        : "No records match the current filter. Use Restore Cleared or change the search/filter.",
                    MessageType.None);
                return;
            }

            foreach (RedshiftManagedAssetRecord record in visible)
            {
                DrawAssetRecord(record);
            }
        }

        private void DrawSectionProgress(
            IReadOnlyList<RedshiftManagedAssetRecord> sectionRecords)
        {
            int compliant = sectionRecords.Count(record =>
                record.Naming == RedshiftAssetNamingState.Compliant);
            int violations = sectionRecords.Count(record =>
                record.Naming == RedshiftAssetNamingState.Violation);
            int excluded = sectionRecords.Count(record =>
                record.Naming == RedshiftAssetNamingState.Excluded);
            int activeTotal = compliant + violations;
            float percentage = activeTotal <= 0
                ? 100f
                : compliant / (float)activeTotal * 100f;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(330f));
            EditorGUILayout.LabelField(
                _section + " Naming Progress",
                RedshiftAssetManagerGUI.CenteredBold(13));

            Rect chartRect = GUILayoutUtility.GetRect(
                210f,
                210f,
                GUILayout.ExpandWidth(true));

            RedshiftAssetManagerGUI.DrawDonutChart(
                chartRect,
                new[]
                {
                    new RedshiftChartSlice(
                        "Compliant",
                        compliant,
                        RedshiftAssetManagerGUI.CompliantColor),
                    new RedshiftChartSlice(
                        "Violations",
                        violations,
                        RedshiftAssetManagerGUI.ViolationColor)
                },
                percentage.ToString("0.0") + "%",
                "compliant");

            RedshiftAssetManagerGUI.DrawLegendRow(
                "Compliant",
                compliant,
                activeTotal,
                RedshiftAssetManagerGUI.CompliantColor);
            RedshiftAssetManagerGUI.DrawLegendRow(
                "Violations",
                violations,
                activeTotal,
                RedshiftAssetManagerGUI.ViolationColor);
            RedshiftAssetManagerGUI.DrawLegendRow(
                "Excluded",
                excluded,
                Math.Max(1, sectionRecords.Count),
                RedshiftAssetManagerGUI.ExcludedColor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Section Status", EditorStyles.boldLabel);
            RedshiftAssetManagerGUI.DrawMetric("Compliant", compliant.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Violations", violations.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Excluded", excluded.ToString());
            RedshiftAssetManagerGUI.DrawMetric("Cleared this session", sectionRecords.Count(
                record => _clearedGuids.Contains(record.Guid)).ToString());
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetRecord(RedshiftManagedAssetRecord record)
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
                RedshiftAssetManagerGUI.NamingColor(record.Naming));
            GUILayout.Space(5f);
            GUILayout.Label(record.Name, EditorStyles.boldLabel, GUILayout.MinWidth(220f));
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                RedshiftAssetManagerGUI.NamingLabel(record.Naming),
                EditorStyles.miniBoldLabel,
                GUILayout.Width(100f));

            if (GUILayout.Button("Ping", GUILayout.Width(50f)))
            {
                RedshiftAssetUtility.PingAsset(record.Path);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(record.Path, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                "Folder: " + record.ParentFolder,
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrWhiteSpace(record.ExpectedPrefix))
            {
                GUILayout.Label(
                    "Required prefix: " + record.ExpectedPrefix,
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (record.Naming != RedshiftAssetNamingState.NotGoverned)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    record.Ambiguous ? "Suggested (review):" : "Suggested:",
                    GUILayout.Width(120f));
                GUILayout.Label(record.SuggestedName, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                if (record.SuggestionCollision)
                {
                    EditorGUILayout.HelpBox(
                        "DUPLICATE SUGGESTION RESOLVED — a numeric suffix was added. " +
                        "Review this asset before committing.",
                        MessageType.Warning);
                }

                if (record.Ambiguous)
                {
                    EditorGUILayout.HelpBox(
                        "REVIEW SUGGESTION — " + record.SuggestionReason,
                        MessageType.Warning);
                }
                else if (!string.IsNullOrWhiteSpace(record.SuggestionReason))
                {
                    EditorGUILayout.LabelField(
                        record.SuggestionReason,
                        EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (record.Type == RedshiftManagedAssetType.Texture)
            {
                DrawTextureDetails(record);
            }
            else if (record.Type == RedshiftManagedAssetType.Material)
            {
                DrawMaterialDetails(record);
            }

            if (record.Naming == RedshiftAssetNamingState.Violation)
            {
                DrawRenameDecision(record);
            }
            else if (record.Naming == RedshiftAssetNamingState.Excluded)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(
                    "This asset is excluded from naming compliance. Other Project " +
                    "Health checks still apply.",
                    MessageType.None);

                if (GUILayout.Button("Remove GUID Ignore", GUILayout.Width(145f)))
                {
                    RedshiftNamingPolicy.UnignoreGuid(record.Guid);
                    RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
                    Repaint();
                }
            }

            DrawRecordUtilityButtons(record);
            EditorGUILayout.EndVertical();
        }

        private void DrawRenameDecision(RedshiftManagedAssetRecord record)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField("Rename Decision", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            DrawDecisionToggle(
                record,
                RedshiftRenameDecision.Suggested,
                "Rename Suggested");
            DrawDecisionToggle(
                record,
                RedshiftRenameDecision.Override,
                "Override Suggested");
            DrawDecisionToggle(
                record,
                RedshiftRenameDecision.Ignore,
                "Skip & Ignore");

            EditorGUILayout.EndHorizontal();

            record.OverrideName = EditorGUILayout.TextField(
                "Override name",
                record.OverrideName ?? string.Empty);

            string validationMessage;
            bool canCommit = CanCommitDecision(record, out validationMessage);

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditorGUILayout.HelpBox(
                    validationMessage,
                    canCommit ? MessageType.None : MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!canCommit);

            if (GUILayout.Button("COMMIT", GUILayout.Height(34f)))
            {
                CommitRecordDecision(record);
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawDecisionToggle(
            RedshiftManagedAssetRecord record,
            RedshiftRenameDecision decision,
            string label)
        {
            bool selected = record.Decision == decision;
            bool toggled = GUILayout.Toggle(
                selected,
                label,
                "Button",
                GUILayout.Height(26f));

            if (toggled && !selected)
            {
                record.Decision = decision;
            }
            else if (!toggled && selected)
            {
                record.Decision = RedshiftRenameDecision.None;
            }
        }

        private bool CanCommitDecision(
            RedshiftManagedAssetRecord record,
            out string message)
        {
            message = string.Empty;

            switch (record.Decision)
            {
                case RedshiftRenameDecision.Suggested:
                    return ValidateRenameTarget(
                        record,
                        record.SuggestedName,
                        out message);

                case RedshiftRenameDecision.Override:
                    if (string.IsNullOrWhiteSpace(record.OverrideName))
                    {
                        message = "Enter an override name before committing.";
                        return false;
                    }

                    return ValidateRenameTarget(
                        record,
                        record.OverrideName,
                        out message);

                case RedshiftRenameDecision.Ignore:
                    message =
                        "COMMIT will persist a GUID naming exception. Project Health " +
                        "will also stop reporting this asset as a naming violation.";
                    return true;

                default:
                    message = "Choose Rename Suggested, Override Suggested or Skip & Ignore.";
                    return false;
            }
        }

        private bool ValidateRenameTarget(
            RedshiftManagedAssetRecord record,
            string rawTargetName,
            out string message)
        {
            string targetName = NormalizeRequestedName(record, rawTargetName);

            if (string.IsNullOrWhiteSpace(targetName))
            {
                message = "The target name is empty.";
                return false;
            }

            if (targetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                targetName.Contains("/") ||
                targetName.Contains("\\"))
            {
                message = "The target contains invalid filename characters.";
                return false;
            }

            if (RedshiftNamingPolicy.TryGetExpectedPrefix(
                    record.Path,
                    out string requiredPrefix) &&
                !string.IsNullOrWhiteSpace(requiredPrefix) &&
                !targetName.StartsWith(
                    requiredPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                message =
                    "Override must keep the required prefix " + requiredPrefix +
                    " so Asset Manager and Project Health remain consistent. Use " +
                    "Skip & Ignore for a deliberate exception.";
                return false;
            }

            if (targetName.Equals(record.Name, StringComparison.OrdinalIgnoreCase))
            {
                message = "The target name is unchanged.";
                return false;
            }

            string targetPath = RedshiftAssetManagerAnalyzer.GetDirectory(record.Path) +
                "/" + targetName + Path.GetExtension(record.Path);

            Object existing = AssetDatabase.LoadMainAssetAtPath(targetPath);

            if (existing != null &&
                !targetPath.Equals(record.Path, StringComparison.OrdinalIgnoreCase))
            {
                message = "Another asset already exists at the target path.";
                return false;
            }

            message = "Ready to commit: " + targetName;
            return true;
        }

        private void CommitRecordDecision(RedshiftManagedAssetRecord record)
        {
            if (record.Decision == RedshiftRenameDecision.Ignore)
            {
                RedshiftNamingPolicy.IgnoreGuid(record.Guid);
                _clearedGuids.Add(record.Guid);
                record.Decision = RedshiftRenameDecision.None;
                RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
                Repaint();
                return;
            }

            string requestedName = record.Decision == RedshiftRenameDecision.Override
                ? record.OverrideName
                : record.SuggestedName;
            string targetName = NormalizeRequestedName(record, requestedName);

            if (!ValidateRenameTarget(record, targetName, out string validationMessage))
            {
                EditorUtility.DisplayDialog(
                    "Rename blocked",
                    validationMessage,
                    "OK");
                return;
            }

            string oldPath = record.Path;
            string error = AssetDatabase.RenameAsset(oldPath, targetName);

            if (!string.IsNullOrWhiteSpace(error))
            {
                EditorUtility.DisplayDialog(
                    "Rename failed",
                    error,
                    "OK");
                return;
            }

            string newPath = RedshiftAssetManagerAnalyzer.GetDirectory(oldPath) +
                "/" + targetName + Path.GetExtension(oldPath);

            RedshiftAssetManagerAnalyzer.UpdateLinksAfterRename(
                _records,
                oldPath,
                newPath,
                record.Type);

            record.Path = newPath;
            record.Name = targetName;
            record.ParentFolder =
                RedshiftAssetManagerAnalyzer.GetParentFolder(newPath);
            record.OverrideName = string.Empty;
            record.Decision = RedshiftRenameDecision.None;

            AssetDatabase.SaveAssets();
            RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
            Repaint();
        }

        private static string NormalizeRequestedName(
            RedshiftManagedAssetRecord record,
            string rawName)
        {
            string result = (rawName ?? string.Empty).Trim();
            string extension = Path.GetExtension(record.Path);

            if (!string.IsNullOrWhiteSpace(extension) &&
                result.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - extension.Length);
            }

            return result.Trim();
        }

        private void DrawRecordUtilityButtons(RedshiftManagedAssetRecord record)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear From List", GUILayout.Width(115f)))
            {
                _clearedGuids.Add(record.Guid);
                Repaint();
            }

            GUILayout.FlexibleSpace();
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.86f, 0.34f, 0.30f);

            if (GUILayout.Button("Delete Asset", GUILayout.Width(95f)))
            {
                DeleteAsset(record);
            }

            GUI.backgroundColor = previousBackground;
            EditorGUILayout.EndHorizontal();
        }

        private void DeleteAsset(RedshiftManagedAssetRecord record)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete asset?",
                "Permanently delete this asset through Unity's AssetDatabase?\n\n" +
                record.Path +
                "\n\nUse this for assets you are certain are not needed. Source control " +
                "should remain your recovery path.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            string oldPath = record.Path;
            RedshiftManagedAssetType type = record.Type;

            if (!AssetDatabase.DeleteAsset(oldPath))
            {
                EditorUtility.DisplayDialog(
                    "Delete failed",
                    "Unity could not delete:\n" + oldPath,
                    "OK");
                return;
            }

            RedshiftAssetManagerAnalyzer.RemoveLinksForDeletedAsset(
                _records,
                oldPath,
                type);
            _records.Remove(record);
            _clearedGuids.Remove(record.Guid);
            RedshiftNamingPolicy.UnignoreGuid(record.Guid);
            AssetDatabase.SaveAssets();
            RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
            _lastScanSummary = _records.Count + " managed assets in current report";
            Repaint();
        }

        private void DrawTextureDetails(RedshiftManagedAssetRecord record)
        {
            EditorGUILayout.Space(4f);

            if (record.MaterialLinks.Count > 0)
            {
                EditorGUILayout.LabelField("Material Links", EditorStyles.miniBoldLabel);

                foreach (RedshiftMaterialTextureLink link in record.MaterialLinks)
                {
                    EditorGUILayout.LabelField(
                        "• " + Path.GetFileNameWithoutExtension(link.MaterialPath) +
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

            if (record.IsUnusedCandidate)
            {
                bool suppressed =
                    record.Flags.RuntimeLoaded ||
                    record.Flags.IgnoreUnused;

                EditorGUILayout.HelpBox(
                    suppressed
                        ? "Unused candidate suppressed by asset flag."
                        : "UNUSED CANDIDATE — review before deleting.",
                    suppressed ? MessageType.None : MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            bool runtime = EditorGUILayout.ToggleLeft(
                "Runtime Loaded",
                record.Flags.RuntimeLoaded,
                GUILayout.Width(120f));
            bool ignoreUnused = EditorGUILayout.ToggleLeft(
                "Ignore Unused",
                record.Flags.IgnoreUnused,
                GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
            string note = EditorGUILayout.TextField(
                "Asset note",
                record.Flags.Note ?? string.Empty);

            if (EditorGUI.EndChangeCheck())
            {
                record.Flags.RuntimeLoaded = runtime;
                record.Flags.IgnoreUnused = ignoreUnused;
                record.Flags.Note = note;
                SaveState();
            }
        }

        private static void DrawMaterialDetails(RedshiftManagedAssetRecord record)
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

        private void DrawRules()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "Canonical prefixes are shared with Project Health. Scripts are not " +
                "governed. Asset Manager suggestions add convenience rules on top, " +
                "but naming compliance itself remains a simple prefix contract.",
                MessageType.Info);

            EditorGUILayout.LabelField("Canonical Prefixes", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawRuleRow("Models", "M_");
            DrawRuleRow("Materials", "MAT_");
            DrawRuleRow("Textures", "T_");
            DrawRuleRow("Prefabs", "PF_");
            DrawRuleRow("Animation Clips", "ANIM_");
            DrawRuleRow("Animator Controllers", "AC_");
            DrawRuleRow("Animator Overrides", "AOC_");
            DrawRuleRow("Avatar Masks", "MASK_");
            DrawRuleRow("Audio", "AUD_");
            DrawRuleRow("Scenes", "SCN_");
            DrawRuleRow("Shaders", "SH_");
            DrawRuleRow("Compute Shaders", "CSH_");
            DrawRuleRow("Scripts", "Class name / not governed");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Suggestion Rules", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Models",
                "M_ + parent folder + useful trailing identifier; old 1–2 letter prefixes are removed; duplicate suggestions become _02, _03, etc.");
            EditorGUILayout.LabelField(
                "Materials",
                "MAT_ + folder above linked Textures + second underscore token from linked texture name where available.");
            EditorGUILayout.LabelField(
                "Textures",
                "T_ + linked material canonical name + normalised texture role (Albedo, Normal, Metallic, AO, Emission, Height, Opacity).");
            EditorGUILayout.LabelField(
                "Other governed types",
                "Canonical prefix + existing base name after recognised legacy prefix cleanup.");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Naming Folder Exclusions",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "One Assets/... folder per line. These exclusions apply only to naming. " +
                "Project Health will still report missing scripts, missing materials, " +
                "oversized textures and other non-naming problems inside these folders.",
                MessageType.None);

            _folderExclusionsText = EditorGUILayout.TextArea(
                _folderExclusionsText,
                GUILayout.MinHeight(100f));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Selected Folder", GUILayout.Width(145f)))
            {
                AddSelectedFolderExclusion();
            }

            if (GUILayout.Button("Save Folder Exclusions", GUILayout.Width(160f)))
            {
                SaveFolderExclusions();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("GUID Naming Exceptions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                RedshiftNamingPolicy.IgnoredGuidCount +
                " asset(s) currently ignored by GUID.");
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(
                RedshiftNamingPolicy.IgnoredGuidCount == 0);

            if (GUILayout.Button("Clear All GUID Ignores", GUILayout.Width(145f)))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear all naming exceptions?",
                    "All GUID-based Skip & Ignore decisions will return to the naming audit.",
                    "Clear",
                    "Cancel"))
                {
                    RedshiftNamingPolicy.ClearIgnoredGuids();

                    if (_analysisHasRun)
                    {
                        RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRuleRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                value,
                EditorStyles.boldLabel,
                GUILayout.Width(190f));
            EditorGUILayout.EndHorizontal();
        }

        private void AddSelectedFolderExclusion()
        {
            Object selected = Selection.activeObject;
            string path = selected == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrWhiteSpace(path))
            {
                EditorUtility.DisplayDialog(
                    "No project selection",
                    "Select a folder or an asset inside the folder you want to exclude.",
                    "OK");
                return;
            }

            string folder = AssetDatabase.IsValidFolder(path)
                ? path
                : RedshiftAssetManagerAnalyzer.GetDirectory(path);

            var folders = ParseFolderLines(_folderExclusionsText).ToList();

            if (!folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(folder);
            }

            _folderExclusionsText = string.Join(
                "\n",
                folders.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        }

        private void SaveFolderExclusions()
        {
            RedshiftNamingPolicy.SetExcludedFolders(
                ParseFolderLines(_folderExclusionsText));
            RefreshFolderExclusionText();

            if (_analysisHasRun)
            {
                RedshiftAssetManagerAnalyzer.RefreshNaming(_records);
            }

            Repaint();
        }

        private void RefreshFolderExclusionText()
        {
            _folderExclusionsText = string.Join(
                "\n",
                RedshiftNamingPolicy.GetExcludedFolders());
        }

        private static IEnumerable<string> ParseFolderLines(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<string>();
            }

            return value
                .Split(
                    new[] { '\r', '\n', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(RedshiftNamingPolicy.NormalizeAssetPath)
                .Select(path => path.Trim().TrimEnd('/'))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void RunAnalysis()
        {
            _clearedGuids.Clear();

            try
            {
                _records = RedshiftAssetManagerAnalyzer.Analyze(
                    _toolSettings,
                    GetOrCreateFlags);
                _analysisHasRun = true;
                _lastScanSummary = _records.Count + " managed assets analysed";
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Asset analysis failed",
                    exception.Message,
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        private RedshiftAssetFlags GetOrCreateFlags(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return new RedshiftAssetFlags();
            }

            if (_flagsByGuid.TryGetValue(guid, out RedshiftAssetFlags flags))
            {
                return flags;
            }

            flags = new RedshiftAssetFlags
            {
                Guid = guid,
                Note = string.Empty
            };

            _state.Flags.Add(flags);
            _flagsByGuid[guid] = flags;
            return flags;
        }

        private void LoadState()
        {
            string json = EditorPrefs.GetString(StateKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    _state = JsonUtility.FromJson<RedshiftAssetManagerState>(json);
                }
                catch
                {
                    _state = null;
                }
            }

            if (_state == null)
            {
                _state = new RedshiftAssetManagerState();
            }

            if (_state.Flags == null)
            {
                _state.Flags = new List<RedshiftAssetFlags>();
            }

            _flagsByGuid.Clear();

            foreach (RedshiftAssetFlags flags in _state.Flags)
            {
                if (flags == null || string.IsNullOrWhiteSpace(flags.Guid))
                {
                    continue;
                }

                _flagsByGuid[flags.Guid] = flags;
            }
        }

        private void SaveState()
        {
            EditorPrefs.SetString(
                StateKey,
                JsonUtility.ToJson(_state, false));
        }

        private static RedshiftManagedAssetType[] TypesForSection(
            ManagerSection section)
        {
            switch (section)
            {
                case ManagerSection.Models:
                    return new[] { RedshiftManagedAssetType.Model };
                case ManagerSection.Materials:
                    return new[] { RedshiftManagedAssetType.Material };
                case ManagerSection.Textures:
                    return new[] { RedshiftManagedAssetType.Texture };
                case ManagerSection.Prefabs:
                    return new[] { RedshiftManagedAssetType.Prefab };
                case ManagerSection.Animations:
                    return new[]
                    {
                        RedshiftManagedAssetType.Animation,
                        RedshiftManagedAssetType.AvatarMask
                    };
                case ManagerSection.Controllers:
                    return new[]
                    {
                        RedshiftManagedAssetType.Controller,
                        RedshiftManagedAssetType.OverrideController
                    };
                case ManagerSection.Audio:
                    return new[] { RedshiftManagedAssetType.Audio };
                case ManagerSection.Scenes:
                    return new[]
                    {
                        RedshiftManagedAssetType.Scene,
                        RedshiftManagedAssetType.Shader
                    };
                case ManagerSection.Scripts:
                    return new[] { RedshiftManagedAssetType.Script };
                default:
                    return Array.Empty<RedshiftManagedAssetType>();
            }
        }
    }
}
