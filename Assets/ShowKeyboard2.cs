using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

[RequireComponent(typeof(TMP_InputField))]
public class ShowKeyboard2 : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    private TMP_InputField inputField;
    public float distance = 0.5f;
    public float verticalOffset = -0.5f;
    public Transform positionSource;   // will default to Camera.main if null

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        // Keep caret at end
        inputField.onValueChanged.AddListener(_ => StartCoroutine(MoveCaretNextFrame()));
        inputField.onSelect.AddListener(_ => StartCoroutine(MoveCaretNextFrame()));
    }

    void Start()
    {
        if (positionSource == null && Camera.main != null)
            positionSource = Camera.main.transform;

#if UNITY_ANDROID && !UNITY_EDITOR
        inputField.shouldHideSoftKeyboard = true;   // we’re using a non-native keyboard
        inputField.shouldHideMobileInput  = true;
#endif
    }

    // Fired when you click/tap the field with an XR Ray Interactor
    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(OpenWhenFocused());
    }

    // Fired when the field becomes selected (sometimes not reliable in XR)
    public void OnSelect(BaseEventData eventData)
    {
        StartCoroutine(OpenWhenFocused());
    }

    private IEnumerator OpenWhenFocused()
    {
        // Ensure the field is actually focused before opening keyboard
        yield return null; // wait one frame
        if (!inputField.isFocused)
        {
            inputField.Select(); // force focus if needed
            yield return null;
        }

        // Ensure we have a keyboard instance
        var kb = NonNativeKeyboard.Instance ?? FindObjectOfType<NonNativeKeyboard>(true);
        if (kb == null)
        {
            Debug.LogError("[ShowKeyboard2] No NonNativeKeyboard in scene. Add the NonNativeKeyboard prefab.");
            yield break;
        }

        // Show and place the keyboard
        kb.gameObject.SetActive(true);
        kb.InputField = inputField;
        kb.PresentKeyboard(inputField.text);

        var src = positionSource != null ? positionSource : transform;
        Vector3 fwd = src.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 targetPos = src.position + fwd * distance + Vector3.up * verticalOffset;
        kb.RepositionKeyboard(targetPos);

        // Keep caret at end after TMP updates
        StartCoroutine(MoveCaretNextFrame());

        Debug.Log("[ShowKeyboard2] Keyboard opened.");
    }

    private IEnumerator MoveCaretNextFrame()
    {
        yield return null;
        int end = inputField.text.Length;
        inputField.caretPosition = end;
        inputField.selectionAnchorPosition = end;
        inputField.selectionFocusPosition = end;
        // inputField.MoveTextEnd(false); // alternative
    }
}
