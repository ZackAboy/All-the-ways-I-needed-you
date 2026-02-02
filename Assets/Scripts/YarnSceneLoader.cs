using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Provides a Yarn command to load a Unity scene and begin a dialogue node,
/// while keeping Yarn variables alive across scenes.
/// </summary>
[DefaultExecutionOrder(-500)]
public class YarnSceneLoader : MonoBehaviour
{
    private static YarnSceneLoader instance;
    private InMemoryVariableStorage sharedStorage;
    [SerializeField] private bool verboseVariableLogging = false;

    // Ensure the load_scene command is registered when assemblies load.
    static YarnSceneLoader()
    {
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
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (sharedStorage == null)
        {
            sharedStorage = GetComponent<InMemoryVariableStorage>();
            if (sharedStorage == null)
            {
                sharedStorage = gameObject.AddComponent<InMemoryVariableStorage>();
            }
        }

        AttachSharedStorageToAllRunners();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (verboseVariableLogging)
        {
            Debug.Log("YarnSceneLoader: Awake completed; shared storage attached.");
            LogStorageSnapshot("Awake");
        }
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
        AttachSharedStorageToAllRunners();

        if (verboseVariableLogging)
        {
            Debug.Log($"YarnSceneLoader: Scene loaded '{scene.name}'.");
            LogStorageSnapshot($"SceneLoaded:{scene.name}");
        }
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
        if (op == null)
        {
            Debug.LogError($"YarnSceneLoader: Scene '{sceneName}' can't be loaded. Is it added to Build Settings or Addressables?");
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        AttachSharedStorageToAllRunners();

        if (verboseVariableLogging)
        {
            Debug.Log($"YarnSceneLoader: Scene load completed '{sceneName}'.");
            LogStorageSnapshot($"LoadScene:{sceneName}");
        }

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

    private DialogueRunner FindRunner()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<DialogueRunner>();
#else
        return Object.FindObjectOfType<DialogueRunner>();
#endif
    }

    private void AttachSharedStorageToAllRunners()
    {
        foreach (var runner in FindRunners())
        {
            if (runner == null)
                continue;

            if (runner.VariableStorage != sharedStorage)
            {
                // If the runner already has values (eg. it started before we attached),
                // merge them into the shared storage before switching.
                if (runner.VariableStorage != null && HasAnyVariables(runner.VariableStorage))
                {
                    var (floats, strings, bools) = runner.VariableStorage.GetAllVariables();
                    sharedStorage.SetAllVariables(floats, strings, bools, clear: false);
                    if (verboseVariableLogging)
                    {
                        Debug.Log($"YarnSceneLoader: Merged variables from runner '{runner.name}'.");
                    }
                }

                runner.VariableStorage = sharedStorage;

                if (verboseVariableLogging)
                {
                    Debug.Log($"YarnSceneLoader: Attached shared storage to runner '{runner.name}'.");
                }

                // Force the runner to rebuild its internal Dialogue with the new storage.
                ResetDialogueInstance(runner);
            }

            // Ensure dialogue starts only after shared storage is attached.
            if (runner.autoStart)
            {
                runner.autoStart = false;
                if (runner.IsDialogueRunning == false && string.IsNullOrEmpty(runner.startNode) == false)
                {
                    runner.StartDialogue(runner.startNode);
                    if (verboseVariableLogging)
                    {
                        Debug.Log($"YarnSceneLoader: Manually started dialogue on '{runner.name}' node '{runner.startNode}'.");
                    }
                }
            }
        }
    }

    private IEnumerable<DialogueRunner> FindRunners()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<DialogueRunner>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<DialogueRunner>();
#endif
    }

    private static bool HasAnyVariables(VariableStorageBehaviour storage)
    {
        var (floats, strings, bools) = storage.GetAllVariables();
        if (floats != null && floats.Count > 0) return true;
        if (strings != null && strings.Count > 0) return true;
        if (bools != null && bools.Count > 0) return true;
        return false;
    }

    private static void ResetDialogueInstance(DialogueRunner runner)
    {
        if (runner == null) return;
        var field = typeof(DialogueRunner).GetField("dialogue", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(runner, null);
        }
    }

    private void LogStorageSnapshot(string context)
    {
        if (sharedStorage == null)
        {
            Debug.LogWarning($"YarnSceneLoader: [{context}] sharedStorage is null.");
            return;
        }

        float simp = 0f;
        float arbiter = 0f;
        float martyr = 0f;

        sharedStorage.TryGetValue("$simp", out simp);
        sharedStorage.TryGetValue("$arbiter", out arbiter);
        sharedStorage.TryGetValue("$martyr", out martyr);

        Debug.Log($"YarnSceneLoader: [{context}] simp={simp} arbiter={arbiter} martyr={martyr}");
    }
}
