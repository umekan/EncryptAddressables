using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

public static class AddressableAssetBuilder
{
   /// <summary>
   /// ビルド
   /// 最後の暗号化以外は
   /// AddressableAssetSettings.BuildPlayerContent()
   /// の中身をコピペしたものです。
   /// </summary>
   [MenuItem("Assets/Build/AddressableBuild")]
   public static void Build()
   {
       var settings = AddressableAssetSettingsDefaultObject.Settings;
       if (settings == null)
       {
           if (EditorApplication.isUpdating)
               Debug.LogError("Addressable Asset Settings does not exist.  EditorApplication.isUpdating was true.");
           else if (EditorApplication.isCompiling)
               Debug.LogError("Addressable Asset Settings does not exist.  EditorApplication.isCompiling was true.");
           else
               Debug.LogError("Addressable Asset Settings does not exist.  Failed to create.");
           return;
       }

       foreach (AddressableAssetGroup group in settings.groups)
       {
           if (group == null)
               continue;
           foreach (AddressableAssetEntry entry in group.entries)
               entry.BundleFileId = null;
       }
       if (Directory.Exists(Addressables.BuildPath))
       {
           try
           {
               Directory.Delete(Addressables.BuildPath, true);
           }
           catch (Exception e)
           {
               Debug.LogException(e);
           }
       }

       var buildContext = new AddressablesDataBuilderInput(settings);
       var result = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(buildContext);
       if (!string.IsNullOrEmpty(result.Error))
           Debug.LogError(result.Error);
       if (BuildScript.buildCompleted != null)
           BuildScript.buildCompleted(result);

       foreach (var bundlePath in result.FileRegistry.GetFilePaths())
       {
           
           // カタログとか暗号化されると、多分JsonAssetProviderも復号対応しなきゃいけないので除く
           // もっとまともな方法あるだろうけど許して
           if (bundlePath.Contains("settings.json") ||
               bundlePath.Contains("catalog.json") ||
               bundlePath.Contains(".bin"))
           {
               continue;
           }

           var bundleName = Path.GetFileNameWithoutExtension(bundlePath);
           // uniqueSaltはStream毎にユニークにする必要がある
           // 今回はAssetBundle名を設定
           var uniqueSalt = Encoding.UTF8.GetBytes(bundleName);

           // 暗号化してファイルに書き込む
           var data = File.ReadAllBytes(bundlePath);
           using (var baseStream = new FileStream(bundlePath, FileMode.OpenOrCreate))
           {
               var cryptor = new SeekableAesStream(baseStream, "password", uniqueSalt);
               cryptor.Write(data, 0, data.Length);
           }
       }

       AssetDatabase.Refresh();
   }
}
