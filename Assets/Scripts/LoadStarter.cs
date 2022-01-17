using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class LoadStarter : MonoBehaviour
{
    [SerializeField] private RawImage _rawImage;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadImage();
            _rawImage.color = Color.cyan;
        }
    }

    private void LoadImage()
    {
        // UniTask入れてawaitできるようにしような
        var asset = Addressables.LoadAssetAsync<Texture2D>("Assets/EncryptedTexts/1 1.jpg");
        asset.Completed += op =>
        {
            _rawImage.texture = op.Result;
        };
    }
}
