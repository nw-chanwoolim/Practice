#if UNITY_EDITOR
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

// 대문자 GGUF와 소문자 gguf를 모두 강제 등록합니다.
[ScriptedImporter(1, new[] { "gguf", "GGUF" })]
public class GGUFImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        GGUFAsset asset = ScriptableObject.CreateInstance<GGUFAsset>();
        asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        asset.filePath = ctx.assetPath;

        ctx.AddObjectToAsset("main", asset);
        ctx.SetMainObject(asset);
    }
}

// 대문자 MMPROJ와 소문자 mmproj를 모두 강제 등록합니다.
[ScriptedImporter(1, new[] { "mmproj", "MMPROJ" })]
public class MMProjImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        MMProjAsset asset = ScriptableObject.CreateInstance<MMProjAsset>();
        asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        asset.filePath = ctx.assetPath;

        ctx.AddObjectToAsset("main", asset);
        ctx.SetMainObject(asset);
    }
}
#endif
