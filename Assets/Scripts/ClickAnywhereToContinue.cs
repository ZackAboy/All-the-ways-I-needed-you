using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

public class ClickAnywhereToContinue : MonoBehaviour, IPointerClickHandler
{
    public DialogueRunner dialogueRunner;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dialogueRunner == null)
            return;

        dialogueRunner.RequestNextLine();
    }
}
