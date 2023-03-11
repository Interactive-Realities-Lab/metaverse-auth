using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [CreateAssetMenu(fileName = "New UI Canvas Instructions Data", menuName = "AlertEvaluation/UI/UICanvasInstructionsData")]
    public class UICanvasInstructionsData : ScriptableObject
    {
        public Vector3 distanceFromWorldOrigin;
        public Vector2 canvasSize;
        public Vector3 scale;

    }
}