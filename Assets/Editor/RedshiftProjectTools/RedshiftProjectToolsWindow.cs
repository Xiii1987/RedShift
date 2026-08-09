using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal sealed class RedshiftProjectToolsWindow : EditorWindow
    {
        private enum ToolTab
        {
            Dashboard,
            Audit,
            BatchRename,
            UnusedAssets,
            Settings
        }

        private readonly string[] _tabLabels =
        {
            "Dashboard",
            "Audit",
            "Batch Rename",
            "Unused Assets",
            "Settings"
        };

        private RedshiftToolSettings _settings;
        private ToolTab _tab;

        private RedshiftProjectStats _stats;
        private List<RedshiftAuditIssue> _auditIssues =
            new List<RedshiftAuditIssue>();
        private Vector2 _auditScroll;
        private string _auditSearch = string.Empty;
        private int _auditTypeFilter;

        private List<string> _renameAssetPaths =
            new List<string>();
        private List<RedshiftRenamePreview> _renamePreview =
            new List<RedshiftRenamePreview>();
        private Vector2 _renameScroll;
        private string _renamePrefix = string.Empty;
        private string _renameSuffix = string.Empty;
        private string _renameFind = string.Empty;
        private string _renameReplace = string.Empty;
        private bool _renameUseNumbering;
        private int _renameStartingNumber = 1;
        private int _renameNumberPadding = 3;

        private RedshiftUnusedScanResult _unusedResult;
        private Vector2 _unusedScroll;
        private string _unusedSearch = string.Empty;
        private string _unusedCategoryFilter = "All";

        private Vector2 _settingsScroll;

        [MenuItem("Redshift/Project Tools")]
        private static void OpenWindow()
        {
            var window =
                GetWindow<RedshiftProjectToolsWindow>(
                    "Redshift Project Tools");

            window.minSize = new Vector2(760f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            _settings = new RedshiftToolSettings();
            _settings.Load();
            RefreshStats();
        }

        private void OnDisable()
        {
            _settings?.Save();
        }

        private void OnGUI()
        {
            DrawHeader();

            _tab = (ToolTab)GUILayout.Toolbar(
                (int)_tab,
                _tabLabels,
                GUILayout.Height(24f));

            EditorGUILayout.Space(6f);

            switch (_tab)
            {
                case ToolTab.Dashboard:
                    DrawDashboard();
                    break;
                case ToolTab.Audit:
                    DrawAudit();
                    break;
                case ToolTab.BatchRename:
                    DrawBatchRename();
                    break;
                case ToolTab.UnusedAssets:
                    DrawUnusedAssets();
                    break;
                case ToolTab.Settings:
                    DrawSettings();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                "REDSHIFT PROJECT TOOLS — V1",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                "Refresh Stats",
                EditorStyles.toolbarButton,
                GUILayout.Width(90f)))
            {
                RefreshStats();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDashboard()
        {
            EditorGUILayout.HelpBox(
                "A conservative project-health toolkit. It reports issues, " +
                "previews renames and quarantines unused candidates instead " +
                "of deleting them.",
                MessageType.Info);

            if (_stats == null)
            {
                RefreshStats();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Project Statistics",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawStatRow("Total assets", _stats.TotalAssets.ToString());
            DrawStatRow("Models", _stats.Models.ToString());
            DrawStatRow("Materials", _stats.Materials.ToString());
            DrawStatRow("Textures", _stats.Textures.ToString());
            DrawStatRow("Prefabs", _stats.Prefabs.ToString());
            DrawStatRow("Scenes", _stats.Scenes.ToString());
            DrawStatRow("Audio", _stats.Audio.ToString());
            DrawStatRow("Animation assets", _stats.Animations.ToString());
            DrawStatRow("Scripts", _stats.Scripts.ToString());
            DrawStatRow(
                "Source asset size",
                RedshiftAssetUtility.FormatBytes(_stats.SourceFileBytes));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Recommended Cleanup Order",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField("1. Commit or create a cleanup branch.");
            EditorGUILayout.LabelField("2. Run Audit and fix missing references.");
            EditorGUILayout.LabelField("3. Batch rename in small, reviewed groups.");
            EditorGUILayout.LabelField("4. Scan unused candidates.");
            EditorGUILayout.LabelField("5. Quarantine candidates and test the game.");
        }

        private void DrawAudit()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "Run Full Audit",
                GUILayout.Height(28f),
                GUILayout.Width(130f)))
            {
                _auditIssues = RedshiftProjectScanner.RunAudit(_settings);
                Repaint();
            }

            GUILayout.Space(8f);

            _auditSearch = EditorGUILayout.TextField(
                "Search",
                _auditSearch);

            string[] filterNames =
                new[] { "All" }
                    .Concat(
                        Enum.GetNames(
                            typeof(RedshiftAuditIssueType)))
                    .ToArray();

            _auditTypeFilter = EditorGUILayout.Popup(
                _auditTypeFilter,
                filterNames,
                GUILayout.Width(150f));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            if (_auditIssues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No audit results yet. The missing-reference scan checks " +
                    "all prefabs and currently open scenes; it does not open " +
                    "closed scenes.",
                    MessageType.None);
                return;
            }

            List<RedshiftAuditIssue> visibleIssues =
                GetVisibleAuditIssues();

            EditorGUILayout.LabelField(
                visibleIssues.Count + " visible issue(s) — " +
                _auditIssues.Count + " total",
                EditorStyles.miniBoldLabel);

            _auditScroll = EditorGUILayout.BeginScrollView(_auditScroll);

            foreach (RedshiftAuditIssue issue in visibleIssues)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(
                    issue.Type.ToString(),
                    EditorStyles.boldLabel,
                    GUILayout.Width(135f));

                GUILayout.Label(
                    issue.AssetPath,
                    EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    RedshiftAssetUtility.PingAsset(issue.AssetPath);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(
                    issue.Message,
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private List<RedshiftAuditIssue> GetVisibleAuditIssues()
        {
            IEnumerable<RedshiftAuditIssue> query = _auditIssues;

            if (_auditTypeFilter > 0)
            {
                var selectedType =
                    (RedshiftAuditIssueType)(_auditTypeFilter - 1);

                query = query.Where(issue => issue.Type == selectedType);
            }

            if (!string.IsNullOrWhiteSpace(_auditSearch))
            {
                query = query.Where(issue =>
                    issue.AssetPath.IndexOf(
                        _auditSearch,
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || issue.Message.IndexOf(
                        _auditSearch,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.ToList();
        }

        private void DrawBatchRename()
        {
            EditorGUILayout.HelpBox(
                "Select assets or folders in the Project window, then load " +
                "the selection. Unity renames through AssetDatabase so GUID " +
                "references are preserved. Scripts are excluded by default.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "Load Project Selection",
                GUILayout.Height(26f),
                GUILayout.Width(165f)))
            {
                _renameAssetPaths =
                    RedshiftBatchRenamer.CollectSelectedAssetPaths(
                        _settings.IncludeFolderContentsInRenameSelection,
                        _settings.AllowScriptRenaming);

                RebuildRenamePreview();
            }

            GUILayout.Label(
                _renameAssetPaths.Count + " asset(s) loaded",
                EditorStyles.miniBoldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", GUILayout.Width(60f)))
            {
                _renameAssetPaths.Clear();
                _renamePreview.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();

            _renamePrefix = EditorGUILayout.TextField(
                "Add prefix",
                _renamePrefix);

            _renameSuffix = EditorGUILayout.TextField(
                "Add suffix",
                _renameSuffix);

            _renameFind = EditorGUILayout.TextField(
                "Find",
                _renameFind);

            _renameReplace = EditorGUILayout.TextField(
                "Replace with",
                _renameReplace);

            _renameUseNumbering = EditorGUILayout.Toggle(
                "Add sequential number",
                _renameUseNumbering);

            if (_renameUseNumbering)
            {
                _renameStartingNumber = EditorGUILayout.IntField(
                    "Starting number",
                    _renameStartingNumber);

                _renameNumberPadding = Mathf.Clamp(
                    EditorGUILayout.IntField(
                        "Number padding",
                        _renameNumberPadding),
                    1,
                    8);
            }

            if (EditorGUI.EndChangeCheck())
            {
                RebuildRenamePreview();
            }

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select Valid", GUILayout.Width(90f)))
            {
                foreach (RedshiftRenamePreview preview in _renamePreview)
                {
                    preview.Selected = preview.IsValid;
                }
            }

            if (GUILayout.Button("Select None", GUILayout.Width(90f)))
            {
                foreach (RedshiftRenamePreview preview in _renamePreview)
                {
                    preview.Selected = false;
                }
            }

            GUILayout.FlexibleSpace();

            GUI.enabled = _renamePreview.Any(
                preview => preview.Selected && preview.IsValid);

            if (GUILayout.Button(
                "Apply Selected Renames",
                GUILayout.Height(26f),
                GUILayout.Width(170f)))
            {
                ApplyRenames();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _renameScroll = EditorGUILayout.BeginScrollView(_renameScroll);

            foreach (RedshiftRenamePreview preview in _renamePreview)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                preview.Selected = EditorGUILayout.Toggle(
                    preview.Selected,
                    GUILayout.Width(18f));

                GUILayout.Label(
                    preview.CurrentName,
                    GUILayout.MinWidth(160f));

                GUILayout.Label("→", GUILayout.Width(18f));

                GUILayout.Label(
                    preview.NewName,
                    preview.IsValid
                        ? EditorStyles.boldLabel
                        : EditorStyles.label,
                    GUILayout.MinWidth(160f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    RedshiftAssetUtility.PingAsset(preview.AssetPath);
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Label(
                    preview.AssetPath,
                    EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(preview.Error))
                {
                    EditorGUILayout.HelpBox(
                        preview.Error,
                        MessageType.Error);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RebuildRenamePreview()
        {
            _renamePreview = RedshiftBatchRenamer.BuildPreview(
                _renameAssetPaths,
                _renamePrefix,
                _renameSuffix,
                _renameFind,
                _renameReplace,
                _renameUseNumbering,
                _renameStartingNumber,
                _renameNumberPadding);
        }

        private void ApplyRenames()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Apply asset renames?",
                "This will rename the selected valid assets through Unity's " +
                "AssetDatabase. Commit to source control first.",
                "Rename",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            int renamedCount =
                RedshiftBatchRenamer.ApplyPreview(
                    _renamePreview,
                    out List<string> errors);

            string message = renamedCount + " asset(s) renamed.";

            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" +
                    string.Join("\n", errors.Take(20));

                Debug.LogError(
                    "Redshift batch rename errors:\n" +
                    string.Join("\n", errors));
            }

            EditorUtility.DisplayDialog(
                "Batch Rename Complete",
                message,
                "OK");

            _renameAssetPaths =
                RedshiftBatchRenamer.CollectSelectedAssetPaths(
                    _settings.IncludeFolderContentsInRenameSelection,
                    _settings.AllowScriptRenaming);

            RebuildRenamePreview();
            RefreshStats();
        }

        private void DrawUnusedAssets()
        {
            EditorGUILayout.HelpBox(
                "This is a candidate scan, not proof that an asset is unused. " +
                "String-based loading, custom editor code, Addressables and " +
                "external systems may not create normal AssetDatabase " +
                "dependencies. Review and quarantine before deleting.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "Scan Unused Candidates",
                GUILayout.Height(28f),
                GUILayout.Width(175f)))
            {
                _unusedResult =
                    RedshiftUnusedAssetScanner.Scan(_settings);
            }

            GUILayout.FlexibleSpace();

            if (_unusedResult != null)
            {
                GUILayout.Label(
                    _unusedResult.Candidates.Count +
                    " candidate(s)",
                    EditorStyles.miniBoldLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (_unusedResult == null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Roots: " + _unusedResult.RootCount +
                "    Dependencies marked used: " +
                _unusedResult.UsedAssetCount +
                "    Project assets scanned: " +
                _unusedResult.ScannedAssetCount,
                EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();

            _unusedSearch = EditorGUILayout.TextField(
                "Search",
                _unusedSearch);

            string[] categories =
                new[] { "All" }
                    .Concat(
                        _unusedResult.Candidates
                            .Select(candidate => candidate.Category)
                            .Distinct()
                            .OrderBy(value => value))
                    .ToArray();

            int categoryIndex =
                Math.Max(
                    0,
                    Array.IndexOf(
                        categories,
                        _unusedCategoryFilter));

            int newCategoryIndex = EditorGUILayout.Popup(
                categoryIndex,
                categories,
                GUILayout.Width(120f));

            _unusedCategoryFilter = categories[newCategoryIndex];

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select Visible", GUILayout.Width(100f)))
            {
                foreach (RedshiftUnusedCandidate candidate
                    in GetVisibleUnusedCandidates())
                {
                    candidate.Selected = true;
                }
            }

            if (GUILayout.Button("Clear Visible", GUILayout.Width(100f)))
            {
                foreach (RedshiftUnusedCandidate candidate
                    in GetVisibleUnusedCandidates())
                {
                    candidate.Selected = false;
                }
            }

            GUILayout.FlexibleSpace();

            int selectedCount = _unusedResult.Candidates.Count(
                candidate => candidate.Selected);

            long selectedBytes = _unusedResult.Candidates
                .Where(candidate => candidate.Selected)
                .Sum(candidate => candidate.FileSizeBytes);

            GUILayout.Label(
                selectedCount + " selected — " +
                RedshiftAssetUtility.FormatBytes(selectedBytes),
                EditorStyles.miniBoldLabel);

            GUI.enabled = selectedCount > 0;

            if (GUILayout.Button(
                "Move Selected to Quarantine",
                GUILayout.Height(26f),
                GUILayout.Width(205f)))
            {
                QuarantineSelectedCandidates();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _unusedScroll = EditorGUILayout.BeginScrollView(_unusedScroll);

            foreach (RedshiftUnusedCandidate candidate
                in GetVisibleUnusedCandidates())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                candidate.Selected = EditorGUILayout.Toggle(
                    candidate.Selected,
                    GUILayout.Width(18f));

                GUILayout.Label(
                    candidate.Category,
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(75f));

                GUILayout.Label(
                    candidate.AssetPath,
                    GUILayout.MinWidth(350f));

                GUILayout.FlexibleSpace();

                GUILayout.Label(
                    RedshiftAssetUtility.FormatBytes(
                        candidate.FileSizeBytes),
                    EditorStyles.miniLabel,
                    GUILayout.Width(80f));

                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    RedshiftAssetUtility.PingAsset(candidate.AssetPath);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private List<RedshiftUnusedCandidate> GetVisibleUnusedCandidates()
        {
            if (_unusedResult == null)
            {
                return new List<RedshiftUnusedCandidate>();
            }

            IEnumerable<RedshiftUnusedCandidate> query =
                _unusedResult.Candidates;

            if (!string.Equals(
                _unusedCategoryFilter,
                "All",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(candidate =>
                    candidate.Category.Equals(
                        _unusedCategoryFilter,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(_unusedSearch))
            {
                query = query.Where(candidate =>
                    candidate.AssetPath.IndexOf(
                        _unusedSearch,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.ToList();
        }

        private void QuarantineSelectedCandidates()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Quarantine selected assets?",
                "The selected assets will be moved into a dated folder. " +
                "Unity GUID references are preserved, but you must test the " +
                "project afterwards.",
                "Move to Quarantine",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            int movedCount =
                RedshiftUnusedAssetScanner.MoveSelectedToQuarantine(
                    _unusedResult.Candidates,
                    _settings,
                    out string quarantineFolder,
                    out List<string> errors);

            string message =
                movedCount + " asset(s) moved to:\n" +
                quarantineFolder;

            if (errors.Count > 0)
            {
                message += "\n\n" + errors.Count +
                    " error(s). See Console.";

                Debug.LogError(
                    "Redshift quarantine errors:\n" +
                    string.Join("\n", errors));
            }

            EditorUtility.DisplayDialog(
                "Quarantine Complete",
                message,
                "OK");

            _unusedResult =
                RedshiftUnusedAssetScanner.Scan(_settings);

            RefreshStats();
            RedshiftAssetUtility.PingAsset(quarantineFolder);
        }

        private void DrawSettings()
        {
            _settingsScroll =
                EditorGUILayout.BeginScrollView(_settingsScroll);

            EditorGUILayout.LabelField(
                "Audit",
                EditorStyles.boldLabel);

            _settings.OversizedTextureThreshold = Mathf.Max(
                256,
                EditorGUILayout.IntField(
                    "Large texture threshold",
                    _settings.OversizedTextureThreshold));

            _settings.ScanPrefabsForMissingReferences =
                EditorGUILayout.Toggle(
                    "Scan all prefabs",
                    _settings.ScanPrefabsForMissingReferences);

            _settings.ScanOpenScenesForMissingReferences =
                EditorGUILayout.Toggle(
                    "Scan open scenes",
                    _settings.ScanOpenScenesForMissingReferences);

            EditorGUILayout.LabelField(
                "Ignored audit folders",
                EditorStyles.miniBoldLabel);

            _settings.IgnoredAuditFolders =
                EditorGUILayout.TextArea(
                    _settings.IgnoredAuditFolders,
                    GUILayout.MinHeight(90f));

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Batch Rename",
                EditorStyles.boldLabel);

            _settings.IncludeFolderContentsInRenameSelection =
                EditorGUILayout.Toggle(
                    "Include selected folders",
                    _settings.IncludeFolderContentsInRenameSelection);

            _settings.AllowScriptRenaming =
                EditorGUILayout.Toggle(
                    "Allow .cs renaming",
                    _settings.AllowScriptRenaming);

            if (_settings.AllowScriptRenaming)
            {
                EditorGUILayout.HelpBox(
                    "Renaming a C# file without renaming its matching class " +
                    "can break the MonoBehaviour link. Leave this disabled " +
                    "unless you are handling the class rename too.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Unused Asset Scan",
                EditorStyles.boldLabel);

            _settings.IncludeAllScenesAsUnusedRoots =
                EditorGUILayout.Toggle(
                    "Treat all scenes as roots",
                    _settings.IncludeAllScenesAsUnusedRoots);

            EditorGUILayout.HelpBox(
                _settings.IncludeAllScenesAsUnusedRoots
                    ? "Every scene in Assets protects its dependencies."
                    : "Only enabled Build Settings scenes protect their dependencies.",
                MessageType.None);

            EditorGUILayout.LabelField(
                "Always-keep folders",
                EditorStyles.miniBoldLabel);

            _settings.AlwaysKeepFolders =
                EditorGUILayout.TextArea(
                    _settings.AlwaysKeepFolders,
                    GUILayout.MinHeight(110f));

            _settings.QuarantineRoot =
                EditorGUILayout.TextField(
                    "Quarantine root",
                    _settings.QuarantineRoot);

            EditorGUILayout.Space(12f);

            if (GUILayout.Button(
                "Save Settings",
                GUILayout.Height(26f),
                GUILayout.Width(120f)))
            {
                _settings.Save();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshStats()
        {
            _stats = RedshiftProjectScanner.BuildProjectStats();
            Repaint();
        }

        private static void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                value,
                EditorStyles.boldLabel,
                GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
        }
    }
}
