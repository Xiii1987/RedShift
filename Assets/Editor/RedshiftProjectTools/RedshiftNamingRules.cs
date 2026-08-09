using System;
using System.IO;

namespace Redshift.EditorTools
{
    internal static class RedshiftNamingRules
    {
        public static bool TryGetExpectedPrefix(
            string assetPath,
            out string expectedPrefix)
        {
            return RedshiftNamingPolicy.TryGetExpectedPrefix(
                assetPath,
                out expectedPrefix);
        }

        public static bool ObeysRule(string assetPath, out string expectedPrefix)
        {
            if (!TryGetExpectedPrefix(assetPath, out expectedPrefix))
            {
                return true;
            }

            // Naming exclusions are shared with the Asset Manager so the
            // Project Health audit and naming workflow never disagree about
            // deliberately exempt assets/folders.
            if (RedshiftNamingPolicy.IsExcluded(assetPath))
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
