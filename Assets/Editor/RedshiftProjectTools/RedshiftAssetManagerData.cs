using System;
using System.Collections.Generic;

namespace Redshift.EditorTools
{
    internal enum RedshiftAssetNamingState
    {
        Compliant,
        Violation,
        Excluded,
        NotGoverned
    }

    internal enum RedshiftRenameDecision
    {
        None,
        Suggested,
        Override,
        Ignore
    }

    [Serializable]
    internal sealed class RedshiftAssetManagerState
    {
        public List<RedshiftAssetFlags> Flags = new List<RedshiftAssetFlags>();
    }

    [Serializable]
    internal sealed class RedshiftAssetFlags
    {
        public string Guid;
        public bool RuntimeLoaded;
        public bool IgnoreUnused;
        public string Note;
    }

    internal sealed class RedshiftManagedAssetRecord
    {
        public string Path;
        public string Guid;
        public string Name;
        public string ParentFolder;
        public RedshiftManagedAssetType Type;
        public RedshiftAssetNamingState Naming;
        public string ExpectedPrefix;
        public string SuggestedName;
        public string SuggestionReason;
        public bool Ambiguous;
        public bool SuggestionCollision;
        public bool IsUnusedCandidate;
        public RedshiftAssetFlags Flags;
        public RedshiftRenameDecision Decision;
        public string OverrideName = string.Empty;

        public readonly List<RedshiftMaterialTextureLink> MaterialLinks =
            new List<RedshiftMaterialTextureLink>();

        public readonly List<string> LinkedTextures = new List<string>();
    }

    internal sealed class RedshiftMaterialTextureLink
    {
        public string MaterialPath;
        public string PropertyName;
    }

    internal struct RedshiftChartSlice
    {
        public readonly string Label;
        public readonly int Value;
        public readonly UnityEngine.Color Color;

        public RedshiftChartSlice(
            string label,
            int value,
            UnityEngine.Color color)
        {
            Label = label;
            Value = value;
            Color = color;
        }
    }
}
