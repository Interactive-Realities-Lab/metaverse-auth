using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MorseSegment : MonoBehaviour
{
    public GameObject digitPrefab;
    public Sprite dotSprite;
    public Sprite dashSprite;
    [Space()]
    public RectTransform panel;
    public List<Image> digits;

    public void AddNewDigit()
    {
        if (digits == null || panel == null || digitPrefab == null)
        {
            Debug.LogError("MorseSegment->InitializeSegment(): references missing");
            return;
        }

        GameObject newDigit = Instantiate(digitPrefab);
        newDigit.transform.parent = gameObject.transform;
        digits.Add(newDigit.GetComponent<Image>());

        // Set new digit position
        Vector3 newPos = digits[digits.Count - 1].rectTransform.position;
        newPos.x = digits[digits.Count - 2].rectTransform.position.x + 90;
        digits[digits.Count - 1].rectTransform.position = newPos;

        // Resize panel
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, digits.Count * 80 + (digits.Count - 1) * 10 + 20);
    }

    private void Start()
    {
        AddNewDigit();
        AddNewDigit();
        AddNewDigit();
        AddNewDigit();
        AddNewDigit();
    }

    public void SetDigit(int index, bool dot)
    {
        digits[index].sprite = dot ? dotSprite : dashSprite;
    }
}
