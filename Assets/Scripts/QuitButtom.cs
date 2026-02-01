using UnityEngine;

public class QuitButtom : MonoBehaviour
{
    public void QuitGame()
    {
        // Works in builds (PC, Mac, WebGL-supported behavior)
        Application.Quit();

#if UNITY_EDITOR
        // So it also works when testing in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}