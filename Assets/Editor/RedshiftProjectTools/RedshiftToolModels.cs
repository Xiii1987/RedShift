using System;
using System.Collections.Generic;

namespace Redshift.EditorTools
{
    internal enum RedshiftAuditIssueType
    {
        MissingScript,
        MissingMaterial,
        OversizedTexture,
        NamingViolation,
        DuplicateName,
        EmptyFolder
    }

    [Serializable]
    internal sealed class RedshiftAuditIssue
    {
        public RedshiftAuditIssueType Type;
        public string AssetPath;
        public string Message;

        public RedshiftAuditIssue(
            RedshiftAuditIssueType type,
            string assetPath,
            string message)
        {
            Type = type;
            AssetPath = assetPath;
            Message = message;
        }
    }

    [Serializable]
    internal sealed class RedshiftRenamePreview
    {
        public bool Selected = true;
        public string AssetPath;
        public string CurrentName;
        public string NewName;
        public string Error;

        public bool IsValid
        {
            get { return string.IsNullOrEmpty(Error) && CurrentName != NewName; }
        }
    }

    [Serializable]
    internal sealed class RedshiftUnusedCandidate
    {
        public bool Selected;
        public string AssetPath;
        public string Category;
        public long FileSizeBytes;
    }

    [Serializable]
    internal sealed class RedshiftProjectStats
    {
        public int TotalAssets;
        public int Models;
        public int Materials;
        public int Textures;
        public int Prefabs;
        public int Scenes;
        public int Audio;
        public int Animations;
        public int Scripts;
        public long SourceFileBytes;
    }

    internal sealed class RedshiftUnusedScanResult
    {
        public readonly List<RedshiftUnusedCandidate> Candidates =
            new List<RedshiftUnusedCandidate>();

        public int RootCount;
        public int UsedAssetCount;
        public int ScannedAssetCount;
    }
}
