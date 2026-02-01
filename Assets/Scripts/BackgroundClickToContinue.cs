using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

/// <summary>
/// Allows clicking the background to continue Yarn dialogue.
/// </summary>
public class BackgroundClickToContinue : MonoBehaviour, IPointerClickHandler
{
    public DialogueRunner dialogueRunner;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Background clicked!");

        if (dialogueRunner == null)
            return;

        dialogueRunner.RequestNextLine();
    }
}
