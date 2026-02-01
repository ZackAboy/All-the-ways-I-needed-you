using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtom : MonoBehaviour
{
    public void LoadTestScene()
    {
        SceneManager.LoadScene("LevelOne");
    }
}