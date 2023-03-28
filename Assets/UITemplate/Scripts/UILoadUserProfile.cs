using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UITemplate;
using UnityEngine.UI;
using TMPro;

public class UILoadUserProfile : MonoBehaviour
{
    [SerializeField] private UIUserContainer userContainer;

    [SerializeField] private Image imageProfile;
    [SerializeField] private TMP_Text name;


    private void Awake()
    {
        imageProfile.sprite = userContainer.userImage;
        name.text = userContainer.name;

        userContainer.hasConnected = false;
    }

}
