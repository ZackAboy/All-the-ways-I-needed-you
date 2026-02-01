using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Provides a Yarn command to load a Unity scene and begin a dialogue node,
/// while keeping Yarn variables alive across scenes.
/// </summary>
public class YarnSceneLoader : MonoBehaviour
{
    private static YarnSceneLoader instance;
    private InMemoryVariableStorage sharedStorage;

    static YarnSceneLoader()
    {
        // Register the command globally so it doesn't rely on finding a scene object by name.
        Actions.AddRegistrationMethod(RegisterActions);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("YarnSceneLoader");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<YarnSceneLoader>();
    }

    private void Awake()
    {
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        sharedStorage = gameObject.AddComponent<InMemoryVariableStorage>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachSharedStorageToRunner();
    }

    private static void RegisterActions(IActionRegistration target, RegistrationType registrationType)
    {
        target.AddCommandHandler<string, string>("load_scene", LoadSceneCommandHandler);
    }

    private static IEnumerator LoadSceneCommandHandler(string sceneName, string startNode)
    {
        EnsureInstance();
        yield return instance.LoadSceneAndStartNode(sceneName, startNode);
    }

    private IEnumerator LoadSceneAndStartNode(string sceneName, string startNode)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        AttachSharedStorageToRunner();

        var runner = FindRunner();
        if (runner != null && string.IsNullOrEmpty(startNode) == false)
        {
            runner.StartDialogue(startNode);
        }
        else if (runner == null)
        {
            Debug.LogWarning($"YarnSceneLoader: No DialogueRunner found after loading scene '{sceneName}'.");
        }
    }

    private void AttachSharedStorageToRunner()
    {
        var runner = FindRunner();
        if (runner != null && runner.VariableStorage != sharedStorage)
        {
            runner.VariableStorage = sharedStorage;
        }
    }

    private DialogueRunner FindRunner()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<DialogueRunner>();
#else
        return Object.FindObjectOfType<DialogueRunner>();
#endif
    }
}
