using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class LoadStarter : MonoBehaviour
{
    [SerializeField] private RawImage _rawImage;
    
    private void Start()
    {
        LoadImage();
    }

    private void LoadImage()
    {
        // UniTask入れてawaitできるようにしような
        var asset = Addressables.LoadAssetAsync<Texture2D>("Assets/ScreenShot.png");
        asset.Completed += op =>
        {
            _rawImage.texture = op.Result;
        };
    }
}
