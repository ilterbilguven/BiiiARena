using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Widgets
{
    internal class CreateSession : EnterSessionBase
    {
        TMP_InputField m_InputField;
        
        protected override void OnEnable()
        {
            m_InputField = GetComponentInChildren<TMP_InputField>();
            base.OnEnable();
        }

        public override void OnServicesInitialized()
        {
            m_InputField.onEndEdit.AddListener(value =>
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame && !string.IsNullOrEmpty(value))
                {
                    EnterSession();
                }
            });
            m_InputField.onValueChanged.AddListener(value =>
            {
                m_EnterSessionButton.interactable = !string.IsNullOrEmpty(value) && Session == null;
            });
        }

        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.Create,
                SessionName = m_InputField.text,
                WidgetConfiguration = WidgetConfiguration,
            };
        }
    }
}
