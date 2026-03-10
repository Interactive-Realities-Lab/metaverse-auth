using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private HeadsetMotion headsetMotion;

    [Header("Split bar images")]
    [SerializeField] private RectTransform leftFill;
    [SerializeField] private RectTransform rightFill;

    [Header("Optional full red warning overlay")]
    [SerializeField] private GameObject redOverlay;

    [Header("Optional shaping")]
    [SerializeField] private AnimationCurve curvedProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Half widths at full progress")]
    [SerializeField] private float leftMaxWidth = 180f;
    [SerializeField] private float rightMaxWidth = 180f;

    [Header("CosSim UI")]
    [SerializeField] private TMP_Text cosSimText;

    private bool updateProgressBar = false;

    void Update()
    {
        if (!updateProgressBar) return;
        if (headsetMotion == null || leftFill == null || rightFill == null) return;

        float raw = headsetMotion.ParityProgress01();
        float t = curvedProgress != null ? curvedProgress.Evaluate(raw) : raw;
        t = Mathf.Clamp01(t);

        // Keep shrinking/growing normally
        UpdateSplitBar(t);

        // Show full red bar only when parity is lost
        bool parityLost = headsetMotion.CurrentState() == HeadsetMotion.MotionState.NotMatched;

        if (redOverlay != null)
            redOverlay.SetActive(parityLost);

        if (cosSimText != null)
        {
            cosSimText.text = $"CosSim: {headsetMotion.LastParityCos():F2}";
        }
    }

    void UpdateSplitBar(float t)
    {
        float leftWidth = leftMaxWidth * t;
        float rightWidth = rightMaxWidth * t;

        Vector2 leftSize = leftFill.sizeDelta;
        leftSize.x = leftWidth;
        leftFill.sizeDelta = leftSize;

        Vector2 rightSize = rightFill.sizeDelta;
        rightSize.x = rightWidth;
        rightFill.sizeDelta = rightSize;
    }

    public void StartUpdateProgressBar()
    {
        updateProgressBar = true;

        if (redOverlay != null)
            redOverlay.SetActive(false);
    }

    public void StopUpdateProgressBar()
    {
        updateProgressBar = false;

        if (redOverlay != null)
            redOverlay.SetActive(false);
    }

    public void ResetFillAmount()
    {
        UpdateSplitBar(0f);

        if (redOverlay != null)
            redOverlay.SetActive(false);
    }

    public void FillComplete()
    {
        UpdateSplitBar(1f);
    }
}