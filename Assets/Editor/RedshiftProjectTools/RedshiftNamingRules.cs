using System;
using System.Collections.Generic;
using System.IO;

namespace Redshift.EditorTools
{
    internal static class RedshiftNamingRules
    {
        private static readonly Dictionary<string, string> PrefixByExtension =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".mat", "MAT_" },

                { ".png", "T_" },
                { ".jpg", "T_" },
                { ".jpeg", "T_" },
                { ".tga", "T_" },
                { ".psd", "T_" },
                { ".exr", "T_" },
                { ".hdr", "T_" },
                { ".tif", "T_" },
                { ".tiff", "T_" },

                { ".fbx", "M_" },
                { ".obj", "M_" },
                { ".blend", "M_" },

                { ".prefab", "PF_" },
                { ".anim", "ANIM_" },
                { ".controller", "AC_" },
                { ".overrideController", "AOC_" },
                { ".mask", "MASK_" },

                { ".wav", "AUD_" },
                { ".mp3", "AUD_" },
                { ".ogg", "AUD_" },
                { ".aiff", "AUD_" },
                { ".aif", "AUD_" },

                { ".unity", "SCN_" },
                { ".shader", "SH_" },
                { ".shadergraph", "SH_" },
                { ".compute", "CSH_" }
            };

        public static bool TryGetExpectedPrefix(
            string assetPath,
            out string expectedPrefix)
        {
            expectedPrefix = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string extension = Path.GetExtension(assetPath);
            return PrefixByExtension.TryGetValue(extension, out expectedPrefix);
        }

        public static bool ObeysRule(string assetPath, out string expectedPrefix)
        {
            if (!TryGetExpectedPrefix(assetPath, out expectedPrefix))
            {
                return true;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            return fileName.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
