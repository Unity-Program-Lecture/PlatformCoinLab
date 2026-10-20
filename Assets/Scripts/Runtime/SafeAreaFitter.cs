using UnityEngine;

public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform target;
    private Rect previous;
    private Vector2Int previousSize;

    private void Awake()
    {
        target = GetComponent<RectTransform>();
    }

    private void Update()
    {
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (size.x <= 0 || size.y <= 0 || (safe == previous && size == previousSize))
        {
            return;
        }

        previous = safe;
        previousSize = size;
        
        target.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
        target.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
