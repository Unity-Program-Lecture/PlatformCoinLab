using UnityEngine;

public sealed class PlatformInfo : MonoBehaviour
{
    [SerializeField] private GameObject mobileControls;

    private void Start()
    {
        Debug.Log($"Platform={Application.platform}, Editor={Application.isEditor}");
        Debug.Log($"Version={Application.version}, Data={Application.dataPath}");
        Debug.Log($"Save={Application.persistentDataPath}");
        
        mobileControls.SetActive(Application.isMobilePlatform || Application.isEditor);
    }
}
