using UnityEngine;

public sealed class ExitGame : MonoBehaviour
{
    [SerializeField] private GameSession session;

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        session.ReturnToMenu();
        session.SetStatus("Use the page navigation or close the tab.");
#else
        Application.Quit();
#endif
    }
}
