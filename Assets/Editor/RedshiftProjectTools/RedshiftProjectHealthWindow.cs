using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal sealed class RedshiftProjectHealthWindow : EditorWindow
    {
        private static readonly Color HealthyColor = new Color(0.22f, 0.68f, 0.34f);
        private static readonly Color AffectedColor = new Color(0.78f, 0.24f, 0.22f);

        private static readonly Dictionary<RedshiftAuditIssueType, Color> IssueColors =
            new Dictionary<RedshiftAuditIssueType, Color>
            {
                { RedshiftAuditIssueType.MissingScript, new Color(0.86f, 0.24f, 0.20f) },
                { RedshiftAuditIssueType.MissingMaterial, new Color(0.95f, 0.52f, 0.18f) },
                { RedshiftAuditIssueType.OversizedTexture, new Color(0.95f, 0.78f, 0.20f) },
                { RedshiftAuditIssueType.NamingViolation, new Color(0.36f, 0.58f, 0.90f) },
                { RedshiftAuditIssueType.DuplicateName, new Color(0.58f, 0.38f, 0.82f) },
                { RedshiftAuditIssueType.EmptyFolder, new Color(0.48f, 0.55f, 0.62f) }
            };

        private RedshiftToolSettings _settings;
        private RedshiftProjectStats _stats;
        private List<RedshiftAuditIssue> _issues = new List<RedshiftAuditIssue>();
        private RedshiftHealthReport _report;
        private Vector2 _scroll;
        private bool _hasAuditResults;

        [MenuItem("Redshift/Project Health")]
        private static void OpenWindow()
        {
            var window = GetWindow<RedshiftProjectHealthWindow>("Redshift Project Health");
            window.minSize = new Vector2(760f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            _settings = new RedshiftToolSettings();
            _settings.Load();
            RefreshStatsOnly();
        }

        private void OnGUI()
        {
            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "Strict health scoring: an asset is unhealthy if it has one or more audit findings. " +
                "Multiple findings on the same asset still count as one affected asset. Empty folders " +
                "are reported, but do not reduce asset health because they are not assets.",
                MessageType.Info);

            EditorGUILayout.Space(6f);

            if (!_hasAuditResults || _report == null)
            {
                DrawNoAuditState();
                DrawProjectStatistics();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawHealthSummary();
            EditorGUILayout.Space(10f);
            DrawCharts();
            EditorGUILayout.Space(10f);
            DrawAuditReport();
            EditorGUILayout.Space(10f);
            DrawProjectStatistics();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("REDSHIFT PROJECT HEALTH — V1.1", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh Stats", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                RefreshStatsOnly();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoAuditState()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("PROJECT HEALTH", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "NO AUDIT DATA — RUN A FULL HEALTH AUDIT",
                GetCenteredBoldStyle(15));
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Run Full Health Audit", GUILayout.Height(34f)))
            {
                RunHealthAudit();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHealthSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PROJECT HEALTH", EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                _report.HealthPercentage.ToString("0.0") + "%",
                GetCenteredBoldStyle(28));
            EditorGUILayout.LabelField(
                "STRICT ASSET HEALTH",
                GetCenteredMiniBoldStyle());

            Rect progressRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(
                progressRect,
                Mathf.Clamp01(_report.HealthPercentage / 100f),
                _report.HealthyAssets + " healthy  •  " +
                _report.AffectedAssets + " affected  •  " +
                _report.TotalAssets + " total assets");

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawSummaryMetric("Healthy assets", _report.HealthyAssets.ToString());
            DrawSummaryMetric("Affected assets", _report.AffectedAssets.ToString());
            DrawSummaryMetric("Audit findings", _report.TotalFindings.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Run Full Health Audit", GUILayout.Height(30f)))
            {
                RunHealthAudit();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCharts()
        {
            EditorGUILayout.LabelField("Health Visualisation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(330f));
            EditorGUILayout.LabelField("Asset Health", GetCenteredBoldStyle(13));

            Rect healthChartRect = GUILayoutUtility.GetRect(
                240f,
                240f,
                GUILayout.ExpandWidth(true));

            DrawDonutChart(
                healthChartRect,
                new[]
                {
                    new ChartSlice("Healthy", _report.HealthyAssets, HealthyColor),
                    new ChartSlice("Affected", _report.AffectedAssets, AffectedColor)
                },
                _report.HealthPercentage.ToString("0.0") + "%",
                "healthy");

            DrawLegendRow("Healthy", _report.HealthyAssets, _report.TotalAssets, HealthyColor);
            DrawLegendRow("Affected", _report.AffectedAssets, _report.TotalAssets, AffectedColor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(330f));
            EditorGUILayout.LabelField("Audit Finding Breakdown", GetCenteredBoldStyle(13));

            Rect auditChartRect = GUILayoutUtility.GetRect(
                240f,
                240f,
                GUILayout.ExpandWidth(true));

            List<ChartSlice> issueSlices = Enum
                .GetValues(typeof(RedshiftAuditIssueType))
                .Cast<RedshiftAuditIssueType>()
                .Select(type => new ChartSlice(
                    GetFriendlyIssueName(type),
                    _report.GetCount(type),
                    GetIssueColor(type)))
                .Where(slice => slice.Value > 0)
                .ToList();

            DrawDonutChart(
                auditChartRect,
                issueSlices,
                _report.TotalFindings.ToString(),
                "findings");

            foreach (RedshiftAuditIssueType type in Enum.GetValues(typeof(RedshiftAuditIssueType)))
            {
                DrawLegendRow(
                    GetFriendlyIssueName(type),
                    _report.GetCount(type),
                    _report.TotalFindings,
                    GetIssueColor(type));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAuditReport()
        {
            EditorGUILayout.LabelField("Audit Report", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawReportRow("Total assets", _report.TotalAssets.ToString(), string.Empty);
            DrawReportRow("Healthy assets", _report.HealthyAssets.ToString(), string.Empty);
            DrawReportRow("Affected assets", _report.AffectedAssets.ToString(), string.Empty);
            DrawReportRow("Project health", _report.HealthPercentage.ToString("0.0") + "%", string.Empty);
            DrawReportRow("Total findings", _report.TotalFindings.ToString(), "100.0%");

            EditorGUILayout.Space(6f);
            DrawSeparator();
            EditorGUILayout.Space(4f);

            foreach (RedshiftAuditIssueType type in Enum.GetValues(typeof(RedshiftAuditIssueType)))
            {
                int count = _report.GetCount(type);
                float percentage = _report.TotalFindings <= 0
                    ? 0f
                    : count / (float)_report.TotalFindings * 100f;

                DrawReportRow(
                    GetFriendlyIssueName(type),
                    count.ToString(),
                    percentage.ToString("0.0") + "%",
                    GetIssueColor(type));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Finding percentages use total audit findings as the denominator. Asset health uses " +
                "distinct affected asset paths, so one badly broken asset never counts more than once " +
                "against the health percentage.",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawProjectStatistics()
        {
            if (_stats == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Project Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawSimpleRow("Total assets", _stats.TotalAssets.ToString());
            DrawSimpleRow("Models", _stats.Models.ToString());
            DrawSimpleRow("Materials", _stats.Materials.ToString());
            DrawSimpleRow("Textures", _stats.Textures.ToString());
            DrawSimpleRow("Prefabs", _stats.Prefabs.ToString());
            DrawSimpleRow("Scenes", _stats.Scenes.ToString());
            DrawSimpleRow("Audio", _stats.Audio.ToString());
            DrawSimpleRow("Animation assets", _stats.Animations.ToString());
            DrawSimpleRow("Scripts", _stats.Scripts.ToString());
            DrawSimpleRow("Source asset size", RedshiftAssetUtility.FormatBytes(_stats.SourceFileBytes));

            EditorGUILayout.EndVertical();
        }

        private void RunHealthAudit()
        {
            _stats = RedshiftProjectScanner.BuildProjectStats();
            _issues = RedshiftProjectScanner.RunAudit(_settings);
            _report = BuildHealthReport(_stats, _issues);
            _hasAuditResults = true;
            Repaint();
        }

        private void RefreshStatsOnly()
        {
            _stats = RedshiftProjectScanner.BuildProjectStats();

            if (_hasAuditResults)
            {
                _report = BuildHealthReport(_stats, _issues);
            }

            Repaint();
        }

        private static RedshiftHealthReport BuildHealthReport(
            RedshiftProjectStats stats,
            IReadOnlyList<RedshiftAuditIssue> issues)
        {
            var report = new RedshiftHealthReport();
            report.TotalAssets = stats == null ? 0 : stats.TotalAssets;
            report.TotalFindings = issues == null ? 0 : issues.Count;

            var affectedAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (issues != null)
            {
                foreach (RedshiftAuditIssue issue in issues)
                {
                    if (RedshiftAssetUtility.IsProjectAssetFile(issue.AssetPath))
                    {
                        affectedAssetPaths.Add(issue.AssetPath);
                    }

                    if (!report.FindingCounts.ContainsKey(issue.Type))
                    {
                        report.FindingCounts.Add(issue.Type, 0);
                    }

                    report.FindingCounts[issue.Type]++;
                }
            }

            report.AffectedAssets = Mathf.Min(affectedAssetPaths.Count, report.TotalAssets);
            report.HealthyAssets = Mathf.Max(0, report.TotalAssets - report.AffectedAssets);
            report.HealthPercentage = report.TotalAssets <= 0
                ? 100f
                : report.HealthyAssets / (float)report.TotalAssets * 100f;

            return report;
        }

        private static void DrawDonutChart(
            Rect rect,
            IReadOnlyList<ChartSlice> slices,
            string centreValue,
            string centreLabel)
        {
            float radius = Mathf.Min(rect.width, rect.height) * 0.40f;
            Vector3 centre = new Vector3(rect.center.x, rect.center.y, 0f);
            float total = slices == null ? 0f : slices.Sum(slice => Mathf.Max(0, slice.Value));

            Handles.BeginGUI();

            Color previousColor = Handles.color;

            if (total <= 0f)
            {
                Handles.color = new Color(0.40f, 0.40f, 0.40f, 0.55f);
                Handles.DrawSolidDisc(centre, Vector3.forward, radius);
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

                    float sweep = slice.Value / total * 360f;
                    Handles.color = slice.Color;
                    Handles.DrawSolidArc(
                        centre,
                        Vector3.forward,
                        startDirection,
                        sweep,
                        radius);

                    startDirection = Quaternion.AngleAxis(sweep, Vector3.forward) * startDirection;
                }
            }

            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.82f, 0.82f, 0.82f, 1f);
            Handles.DrawSolidDisc(centre, Vector3.forward, radius * 0.58f);
            Handles.color = previousColor;

            Handles.EndGUI();

            GUIStyle valueStyle = GetCenteredBoldStyle(22);
            GUIStyle labelStyle = GetCenteredMiniBoldStyle();

            Rect valueRect = new Rect(
                rect.center.x - radius * 0.55f,
                rect.center.y - 22f,
                radius * 1.10f,
                28f);

            Rect labelRect = new Rect(
                rect.center.x - radius * 0.55f,
                rect.center.y + 6f,
                radius * 1.10f,
                20f);

            GUI.Label(valueRect, centreValue, valueStyle);
            GUI.Label(labelRect, centreLabel, labelStyle);
        }

        private static void DrawLegendRow(string label, int value, int total, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            Rect swatchRect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            EditorGUI.DrawRect(swatchRect, color);

            GUILayout.Space(4f);
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();

            float percentage = total <= 0 ? 0f : value / (float)total * 100f;
            EditorGUILayout.LabelField(
                value + "  (" + percentage.ToString("0.0") + "%)",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(110f));

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSummaryMetric(string label, string value)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(140f));
            EditorGUILayout.LabelField(value, GetCenteredBoldStyle(18));
            EditorGUILayout.LabelField(label, GetCenteredMiniBoldStyle());
            EditorGUILayout.EndVertical();
        }

        private static void DrawReportRow(string label, string count, string percentage)
        {
            DrawReportRow(label, count, percentage, Color.clear);
        }

        private static void DrawReportRow(string label, string count, string percentage, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            if (color.a > 0f)
            {
                Rect swatchRect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
                EditorGUI.DrawRect(swatchRect, color);
                GUILayout.Space(4f);
            }

            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(count, EditorStyles.boldLabel, GUILayout.Width(80f));
            EditorGUILayout.LabelField(percentage, EditorStyles.miniBoldLabel, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSimpleRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(
                rect,
                EditorGUIUtility.isProSkin
                    ? new Color(0.35f, 0.35f, 0.35f)
                    : new Color(0.65f, 0.65f, 0.65f));
        }

        private static Color GetIssueColor(RedshiftAuditIssueType type)
        {
            if (IssueColors.TryGetValue(type, out Color color))
            {
                return color;
            }

            return Color.gray;
        }

        private static string GetFriendlyIssueName(RedshiftAuditIssueType type)
        {
            switch (type)
            {
                case RedshiftAuditIssueType.MissingScript:
                    return "Missing Script";
                case RedshiftAuditIssueType.MissingMaterial:
                    return "Missing Material";
                case RedshiftAuditIssueType.OversizedTexture:
                    return "Oversized Texture";
                case RedshiftAuditIssueType.NamingViolation:
                    return "Naming Violation";
                case RedshiftAuditIssueType.DuplicateName:
                    return "Duplicate Name";
                case RedshiftAuditIssueType.EmptyFolder:
                    return "Empty Folder";
                default:
                    return type.ToString();
            }
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

        private sealed class RedshiftHealthReport
        {
            public int TotalAssets;
            public int HealthyAssets;
            public int AffectedAssets;
            public int TotalFindings;
            public float HealthPercentage;

            public readonly Dictionary<RedshiftAuditIssueType, int> FindingCounts =
                new Dictionary<RedshiftAuditIssueType, int>();

            public int GetCount(RedshiftAuditIssueType type)
            {
                return FindingCounts.TryGetValue(type, out int count) ? count : 0;
            }
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
    }
}
