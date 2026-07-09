using UnityEditor;

public class AssetBundler : Editor
{
    [MenuItem("Assets/Build to Level Asset Bundle")]
    static void BuildLevelAssetBundle()
    {
        BuildPipeline.BuildAssetBundles("Assets/Bundles", BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }
}
