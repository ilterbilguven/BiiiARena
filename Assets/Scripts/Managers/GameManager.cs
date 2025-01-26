using System;
using System.Collections;
using System.Collections.Generic;
using Game.Behaviours;
using Game.Utilities;
using PrimeTween;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

namespace Game.Managers
{
    public class GameManager : NetworkSingletonBehaviour<GameManager>
    {
        public GameObject PreviewPlatformPrefab;
        public GameObject BasePlatformPrefab;
        public Transform BaseParent;
        public Transform BasePlatform;
    
        public Color[] PlayerColors;

        public bool Started;
    
        public event Action GameStarted;
        public event Action GameOver;

        public Dictionary<NetworkObject, string> RegisteredPlayers = new();
        public List<NetworkObject> ActivePlayers = new();

        public event Action<NetworkObject> PlayerRegistered;
        public event Action<NetworkObject> PlayerUnregistered;
        
        public float PlatformScaleDuration = 60f;

        public byte Countdown = 3;
        
        private Tween _scaleTween;
        
        [SerializeField] private GameObject _arSession;
        [SerializeField] private ARRaycastManager _raycastManager;
        private Vector2 _screenMiddlePoint;

        [SerializeField] private GameObject[] _arObjects;
        [SerializeField] private GameObject[] _nonArObjects;
        
        private bool _isARSupportChecked;
        private bool _isARSupported;
        
        private bool _placingPlatform;
        private GameObject _platformPreview;

        [SerializeField] private Camera _arCamera;
        [SerializeField] private Camera _nonArCamera;
        
        public Transform CameraPosition => !_isARSupported ? _nonArCamera.transform : _arCamera.transform;
        
        private void Awake()
        {
            _screenMiddlePoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Random.InitState(42);
            
            StartCoroutine(CheckARAvailability());
        }

        private IEnumerator CheckARAvailability()
        {
            if (Application.isEditor)
            {
                InitializeNonAR();
                yield break;
            }
            
            if (_arSession)
            {
                _arSession.SetActive(true);
                yield return new WaitForSecondsRealtime(1);
            }

            while (ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return null;
            }
            
            switch (ARSession.state)
            {
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    yield return ARSession.Install();
                    InitializeAR();
                    break;
                
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                case ARSessionState.SessionTracking:
                    _isARSupportChecked = true;
                    _isARSupported = true;
                    InitializeAR();
                    break;

                case ARSessionState.None:
                case ARSessionState.Unsupported:
                default:
                    _isARSupportChecked = true;
                    _isARSupported = false;
                    InitializeNonAR();
                    break;
            }
        }

        private void InitializeNonAR()
        {
            Destroy(_arSession);

            foreach (var arObject in _arObjects)
            {
                Destroy(arObject);
            }
            
            foreach (var nonArObject in _nonArObjects)
            {
                nonArObject.SetActive(true);
            }
            
            SpawnPlatform(Vector3.zero);

            UiManager.Instance.EnableMatchmakingUI();
        }
        private void InitializeAR()
        {
            foreach (var nonArObject in _nonArObjects)
            {
                Destroy(nonArObject);
            }
            
            foreach (var arObject in _arObjects)
            {
                arObject.SetActive(true);
            }
            
            UiManager.Instance.EnablePlatformPlacing();
            PreparePlatform();
        }

        private void PreparePlatform()
        {
            _platformPreview = Instantiate(PreviewPlatformPrefab, Vector3.zero, Quaternion.identity);
            _placingPlatform = true;
        }

        private void FixedUpdate()
        {
            if (!_placingPlatform)
            {
                return;
            }
            
            var list = new List<ARRaycastHit>();
            
            if (!_raycastManager.Raycast(_screenMiddlePoint, list, TrackableType.Planes)) return;
            if (list.Count <= 0) return;
            
            var hitPose = list[0].pose;
            var hitPosition = hitPose.position;
            
            _platformPreview.transform.position = hitPosition;
        }

        public void PlacePlatform()
        {
            if (!_placingPlatform)
            {
                return;
            }
            
            _placingPlatform = false;
            SpawnPlatform(_platformPreview.transform.position);
            Destroy(_platformPreview);
            UiManager.Instance.DisablePlatformPlacing();
            UiManager.Instance.EnableMatchmakingUI();
        }

        public void StartGame()
        {
            ReceiveStartGameSignalRpc();
        }
    
        [Rpc(SendTo.Everyone)]
        private void ReceiveStartGameSignalRpc()
        {
            ActivePlayers.AddRange(RegisteredPlayers.Keys);
            UiManager.Instance.DisableMatchmakingUI();
            UiManager.Instance.EnableJoystickUI();            
            StartGameSignalReceived();
            return;

            async void StartGameSignalReceived()
            {
                var seconds = Countdown;

                UiManager.Instance.SetText(seconds.ToString());
                 
                while (seconds-- > 0)
                {
                    await Awaitable.WaitForSecondsAsync(1);
                    UiManager.Instance.SetText(seconds.ToString());
                }

                UiManager.Instance.ClearText();

                StartScalingPlatformDown();

                Started = true;
                GameStarted?.Invoke();
            }
        }

        public void RegisterPlayer(NetworkObject player, string playerName)
        {
            RegisteredPlayers.TryAdd(player, playerName);
            PlayerRegistered?.Invoke(player);
        }

        public void UnregisterPlayer(NetworkObject player)
        {
            RegisteredPlayers.Remove(player);
            PlayerUnregistered?.Invoke(player);
        }

        public void PlayerDied(ulong victimId, ulong killerId)
        {
            var victim = ActivePlayers.Find(p => p.OwnerClientId == victimId);
            
            ActivePlayers.Remove(victim);
            
            var killer = ActivePlayers.Find(p => p.OwnerClientId == killerId);
            
            if (victimId == killerId)
            {
                if (victim.IsOwner)
                {
                    if (victim.TryGetComponent(out ClientViewBehaviour viewBehaviour))
                    {
                        UiManager.Instance.EnableJoystickUI();
                        viewBehaviour.HandleDeathAnimation();
                    }

                    UiManager.Instance.SetText("lol u killed urself");
                }
                
                Debug.Log("He's dead, Jim!");
                
                if (ActivePlayers.Count == 1)
                {
                    OnLastManStanding(ActivePlayers[0]);
                }
                
                return;
            }

            if (victim.IsOwner)
            {
                if (victim.TryGetComponent(out ClientViewBehaviour viewBehaviour))
                {
                    UiManager.Instance.EnableJoystickUI();
                    viewBehaviour.HandleDeathAnimation();
                }
                UiManager.Instance.SetText("u ded");
            }

            Debug.Log($"{victimId}'s dead, Jim! Killed by {killerId}");
            
            if (killer.TryGetComponent(out ClientPowerUpBehaviour powerUpBehaviour))
            {
                powerUpBehaviour.OnKillRpc(RpcTarget.Single(killerId, RpcTargetUse.Temp));
            }

            if (killer.TryGetComponent(out ClientCollisionBehaviour collisionBehaviour))
            {
                collisionBehaviour.ResetLastHitPlayerId();
            }
            
            if (ActivePlayers.Count == 1)
            {
                OnLastManStanding(ActivePlayers[0]);
            }
        }

        private void OnLastManStanding(NetworkObject player)
        {
            if (player.IsOwner) UiManager.Instance.SetText("omg you won!");
            UiManager.Instance.SetText($"{RegisteredPlayers[player]} has bubblerizz"); 
            Debug.Log($"{player.OwnerClientId} won!");
            GameOver?.Invoke();
            Started = false;
            _scaleTween.Stop();
        }
        
        public void SpawnPlatform(Vector3 position)
        {
            BaseParent = Instantiate(BasePlatformPrefab, position, Quaternion.identity).transform;
            BasePlatform = BaseParent.GetChild(0);
        }

        private void StartScalingPlatformDown()
        {
             _scaleTween = Tween.Scale(BasePlatform, Vector3.zero, PlatformScaleDuration, Ease.Linear);
        }
    }
}