using System.Threading.Tasks;
using Game.Utilities;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Game.Managers
{
    public class UiManager : SingletonBehaviour<UiManager>
    {
        [SerializeField] private TMP_Text _text;

        [SerializeField] private GameObject _platformPlacingContainer;

        [SerializeField] private GameObject _matchmakingContainer;

        [SerializeField] private GameObject _joystickContainer;
        
        public void SetText(string text) => _text.text = text;
        
        public void ClearText() => _text.text = string.Empty;
        
        public void EnablePlatformPlacing() => _platformPlacingContainer.SetActive(true);
        
        public void DisablePlatformPlacing() => _platformPlacingContainer.SetActive(false);
        
        public void EnableMatchmakingUI() => _matchmakingContainer.SetActive(true);
        
        public void DisableMatchmakingUI() => _matchmakingContainer.SetActive(false);
        
        public void EnableJoystickUI() => _joystickContainer.SetActive(true);
        
        public void DisableJoystickUI() => _joystickContainer.SetActive(false);
    }
}