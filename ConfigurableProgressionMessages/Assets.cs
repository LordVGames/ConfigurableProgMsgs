using System.IO;
using UnityEngine;

namespace ConfigurableProgressionMessages
{
    public static class Assets
    {
        public static AssetBundle ModIconAssetBundle;
        public const string BundleName = "modicon";

        public static string AssetBundlePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(ConfigurableProgressionMessages.PInfo.Location), BundleName);
            }
        }

        public static void Init()
        {
            ModIconAssetBundle = AssetBundle.LoadFromFile(AssetBundlePath);
        }
    }
}
