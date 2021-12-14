using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class CustomAssetBundleResource : IAssetBundleResource
{
   private AssetBundle assetBundle;
   private DownloadHandlerAssetBundle downloadHandler;
   private AsyncOperation requestOperation;
   private ProvideHandle provideHandle;
   private AssetBundleRequestOptions options;
   private FileStream fileStream;
   const string password = "password";

   /// <summary>
   /// 初期化する
   /// </summary>
   public void Setup(ProvideHandle handle)
   {
       assetBundle = null;
       downloadHandler = null;
       provideHandle = handle;
       options = provideHandle.Location.Data as AssetBundleRequestOptions;
       requestOperation = null;
       provideHandle.SetProgressCallback(GetProgress);
   }

   /// <summary>
   /// ロード・ダウンロードする
   /// </summary>
   public void Fetch()
   {
       var path = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
       if (File.Exists(path) || (Application.platform == RuntimePlatform.Android && path.StartsWith("jar:")))
       {
           // 暗号化したAssetBundleを取得
           fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
           var bundleName = Path.GetFileNameWithoutExtension(path);
           var uniqueSalt = Encoding.UTF8.GetBytes(bundleName); // AssetBundle名でsaltを生成
           // Streamで暗号化を解除しつつAssetBundleをロードする
           var decryptStream = new SeekableAesStream(fileStream, password, uniqueSalt);
           requestOperation = AssetBundle.LoadFromStreamAsync(decryptStream);
           requestOperation.completed += op =>
           {
               assetBundle = (op as AssetBundleCreateRequest).assetBundle;
               provideHandle.Complete(this, true, null);
           };
           return;
       }
       
       // ローカルに無い場合にサーバから取ってくる処理をここに書くといいよ
   }

   /// <summary>
   /// アンロードする
   /// </summary>
   public void Unload()
   {
       if (assetBundle != null)
       {
           assetBundle.Unload(true);
           assetBundle = null;
       }
       if (downloadHandler != null)
       {
           downloadHandler.Dispose();
           downloadHandler = null;
       }
       requestOperation = null;
   }

   /// <summary>
   /// ロード・ダウンロードされたAssetBundleを取得する
   /// </summary>
   public AssetBundle GetAssetBundle()
   {
       if (assetBundle == null && downloadHandler != null)
       {
           assetBundle = downloadHandler.assetBundle;
           downloadHandler.Dispose();
           downloadHandler = null;
       }
       return assetBundle;
   }

   /// <summary>
   /// ロード・ダウンロード進捗を取得する
   /// </summary>
   private float GetProgress() => requestOperation?.progress ?? 0.0f;
}

[System.ComponentModel.DisplayName("Custom AssetBundle Provider")]
public class CustomAssetBundleProvider : ResourceProviderBase
{
   /// <summary>
   /// ProvideHandleに入っている情報が示すAssetBundleを読み込む処理
   /// </summary>
   public override void Provide(ProvideHandle providerInterface)
   {
       var res = new CustomAssetBundleResource();
       res.Setup(providerInterface);
       res.Fetch();
   }

   /// <summary>
   /// 読み込み結果としてAssetロード用のProviderに渡すための型を返却
   /// Assets from Bundles Providerを使う場合にはIAssetBundleResourceを指定
   /// </summary>
   public override Type GetDefaultType(IResourceLocation location) => typeof(IAssetBundleResource);

   /// <summary>
   /// 解放処理
   /// </summary>
   public override void Release(IResourceLocation location, object asset)
   {
       if (location == null) { throw new ArgumentNullException(nameof(location)); }

       if (asset == null)
       {
           Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
           return;
       }
       if (asset is CustomAssetBundleResource bundle) { bundle.Unload(); }
   }
}
