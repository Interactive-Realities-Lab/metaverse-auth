using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ButtonClickEmulator : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
    public void ClickButton()
    {
        if (button == null) return;
        button.onClick?.Invoke();    
    }
}

//[CustomEditor(typeof(ButtonClickEmulator))]
//public class Asd : Editor
//{
    
//    public override void OnInspectorGUI()
//    {


//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("Send Click Event"))
//       {
//            ((ButtonClickEmulator)target).ClickButton();

//        }
//        GUILayout.EndHorizontal();
//    }
//}