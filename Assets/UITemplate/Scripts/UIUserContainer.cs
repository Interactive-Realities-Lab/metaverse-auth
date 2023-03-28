using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [CreateAssetMenu(fileName = "New User Data", menuName = "AlertEvaluation/UI/UI User Data")]
    public class UIUserContainer : ScriptableObject
    {

        public Sprite userImage;
        public string userName;
        public bool hasConnected;

    }
}