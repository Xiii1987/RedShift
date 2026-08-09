using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Redshift.EditorTools
{
    internal static class RedshiftAssetManagerGUI
    {
        public static readonly Color CompliantColor =
            new Color(0.20f, 0.68f, 0.34f);
        public static readonly Color ViolationColor =
            new Color(0.82f, 0.27f, 0.23f);
        public static readonly Color ExcludedColor =
            new Color(0.52f, 0.55f, 0.60f);
        public static readonly Color PendingColor =
            new Color(0.86f, 0.67f, 0.24f);

        private static readonly Dictionary<RedshiftManagedAssetType, Color> TypeColors =
            new Dictionary<RedshiftManagedAssetType, Color>
            {
                { RedshiftManagedAssetType.Model, new Color(0.35f, 0.60f, 0.90f) },
                { RedshiftManagedAssetType.Material, new Color(0.95f, 0.55f, 0.22f) },
                { RedshiftManagedAssetType.Texture, new Color(0.58f, 0.42f, 0.84f) },
                { RedshiftManagedAssetType.Prefab, new Color(0.24f, 0.74f, 0.74f) },
                { RedshiftManagedAssetType.Animation, new Color(0.90f, 0.40f, 0.62f) },
                { RedshiftManagedAssetType.Controller, new Color(0.78f, 0.47f, 0.30f) },
                { RedshiftManagedAssetType.OverrideController, new Color(0.72f, 0.40f, 0.32f) },
                { RedshiftManagedAssetType.AvatarMask, new Color(0.68f, 0.50f, 0.28f) },
                { RedshiftManagedAssetType.Audio, new Color(0.45f, 0.72f, 0.32f) },
                { RedshiftManagedAssetType.Scene, new Color(0.82f, 0.72f, 0.25f) },
                { RedshiftManagedAssetType.Shader, new Color(0.38f, 0.48f, 0.82f) },
                { RedshiftManagedAssetType.Script, new Color(0.45f, 0.58f, 0.68f) },
                { RedshiftManagedAssetType.Other, new Color(0.50f, 0.50f, 0.50f) }
            };

        public static Color TypeColor(RedshiftManagedAssetType type)
        {
            return TypeColors.TryGetValue(type, out Color color)
                ? color
                : Color.gray;
        }

        public static Color NamingColor(RedshiftAssetNamingState state)
        {
            switch (state)
            {
                case RedshiftAssetNamingState.Compliant:
                    return CompliantColor;
                case RedshiftAssetNamingState.Violation:
                    return ViolationColor;
                case RedshiftAssetNamingState.Excluded:
                    return ExcludedColor;
                default:
                    return new Color(0.42f, 0.45f, 0.48f);
            }
        }

        public static string NamingLabel(RedshiftAssetNamingState state)
        {
            switch (state)
            {
                case RedshiftAssetNamingState.Compliant:
                    return "COMPLIANT";
                case RedshiftAssetNamingState.Violation:
                    return "VIOLATION";
                case RedshiftAssetNamingState.Excluded:
                    return "EXCLUDED";
                default:
                    return "NOT GOVERNED";
            }
        }

        public static string FriendlyTypeName(RedshiftManagedAssetType type)
        {
            switch (type)
            {
                case RedshiftManagedAssetType.OverrideController:
                    return "Animator Override";
                case RedshiftManagedAssetType.AvatarMask:
                    return "Avatar Mask";
                default:
                    return type.ToString();
            }
        }

        public static void DrawMetric(string label, string value)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.MinWidth(125f));

            EditorGUILayout.LabelField(value, CenteredBold(18));
            EditorGUILayout.LabelField(label, CenteredMiniBold());
            EditorGUILayout.EndVertical();
        }

        public static void DrawDonutChart(
            Rect rect,
            IReadOnlyList<RedshiftChartSlice> slices,
            string centreValue,
            string centreLabel)
        {
            float radius = Mathf.Min(rect.width, rect.height) * 0.40f;
            Vector3 centre = new Vector3(rect.center.x, rect.center.y, 0f);
            float total = slices == null
                ? 0f
                : slices.Sum(slice => Mathf.Max(0, slice.Value));

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

                foreach (RedshiftChartSlice slice in slices)
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

                    startDirection =
                        Quaternion.AngleAxis(sweep, Vector3.forward) *
                        startDirection;
                }
            }

            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.82f, 0.82f, 0.82f, 1f);

            Handles.DrawSolidDisc(centre, Vector3.forward, radius * 0.58f);
            Handles.color = previousColor;
            Handles.EndGUI();

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

            GUI.Label(valueRect, centreValue, CenteredBold(22));
            GUI.Label(labelRect, centreLabel, CenteredMiniBold());
        }

        public static void DrawLegendRow(
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

            float percentage = total <= 0
                ? 0f
                : value / (float)total * 100f;

            EditorGUILayout.LabelField(
                value + "  (" + percentage.ToString("0.0") + "%)",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(110f));

            EditorGUILayout.EndHorizontal();
        }

        public static GUIStyle CenteredBold(int fontSize)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize
            };
        }

        public static GUIStyle CenteredMiniBold()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        public static GUIStyle CenteredWrapped()
        {
            return new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
