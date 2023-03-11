using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UITemplate
{

    [ExecuteInEditMode()]
    public class UITemplateBase : MonoBehaviour
    {

        protected virtual void OnSkinUI()
        {

        }

        public virtual void Awake()
        {
            OnSkinUI();
        }

        public virtual void Update()
        {
            if (Application.isEditor)
            {
                OnSkinUI();
            }
        }
    }
}
