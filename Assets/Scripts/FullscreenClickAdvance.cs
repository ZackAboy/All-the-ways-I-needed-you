using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

/// <summary>
/// Lets the player click anywhere on a fullscreen UI element to advance dialogue
/// (same behaviour as the continue arrow). Attach this to a transparent Image
/// that covers the screen and set the LineAdvancer reference.
/// </summary>
public class FullscreenClickAdvance : MonoBehaviour, IPointerClickHandler
{
    [Header("Yarn")]
    public LineAdvancer lineAdvancer;

    [Header("Input")]
    public bool ignoreUISelections = true; // avoid advancing when clicking UI controls (options, buttons)

    void Awake()
    {
        if (lineAdvancer == null)
        {
            lineAdvancer = FindObjectOfType<LineAdvancer>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (lineAdvancer == null)
            return;

        if (ignoreUISelections && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            return;

        // Hurry current line if mid-type, otherwise go to next line.
        lineAdvancer.RequestLineHurryUp();
        lineAdvancer.RequestNextLine();
    }
}
