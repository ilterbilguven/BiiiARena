using System;
using Game.Managers;
using PrimeTween;
using TMPro;
using Unity.Collections;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using UnityEngine;

namespace Game.Behaviours
{
    public class ClientViewBehaviour : NetworkBehaviour
    {
        private static readonly int Direction = Shader.PropertyToID("_Direction");
        private static readonly int Panner = Shader.PropertyToID("_Panner");
        
        public NetworkVariable<FixedString4096Bytes> PlayerName;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Canvas _canvas;
        
        [SerializeField] private Renderer _self;
        [SerializeField] private Renderer _arrow;

        [SerializeField] private ClientPowerUpBehaviour _powerUpBehaviour;
        [SerializeField] private ParticleSystem _deathParticle;
        
        
        public override async void OnNetworkSpawn()
        {
            _canvas.worldCamera = Camera.main;
            
            NetworkObject.TrySetParent(GameManager.Instance.BaseParent, true);
            NetworkObject.transform.localPosition =
                GameManager.Instance.BaseParent.GetChild((int)OwnerClientId).localPosition;
            NetworkObject.transform.forward = GameManager.Instance.BaseParent.position - NetworkObject.transform.position;
            
            if (IsOwner)
            {
                PlayerName.Value = await WidgetDependencies.Instance.AuthenticationService.GetPlayerNameAsync();
                GameManager.Instance.RegisterPlayer(NetworkObject, PlayerName.Value.Value);
                _nameText.text = PlayerName.Value.Value;
                SetNameTextRpc(PlayerName.Value.Value);
            }
            else if (!string.IsNullOrEmpty(PlayerName.Value.Value))
            {
                GameManager.Instance.RegisterPlayer(NetworkObject, PlayerName.Value.Value);
                _nameText.text = PlayerName.Value.Value;
            }
            
            base.OnNetworkSpawn();
            SetColorBasedOnOwner();
        }

        public override void OnNetworkDespawn()
        {
            GameManager.Instance.UnregisterPlayer(NetworkObject);
            base.OnNetworkDespawn();
        }
        
        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            SetColorBasedOnOwner();
        }

        private void SetColorBasedOnOwner()
        {
            var color = GameManager.Instance.PlayerColors.Length < (int)OwnerClientId
                ? UnityEngine.Random.ColorHSV()
                : GameManager.Instance.PlayerColors[(int)OwnerClientId - 1];

            _self.material.color = color;
            _arrow.material.color = color;
            var mainModule = _deathParticle.main;
            mainModule.startColor = color;
        }

        [Rpc(SendTo.NotOwner)]
        private void SetNameTextRpc(string playerName)
        {
            GameManager.Instance.RegisterPlayer(NetworkObject, playerName);
            _nameText.text = playerName;
        }

        private void Update()
        {
            _canvas.transform.LookAt(GameManager.Instance.CameraPosition);
            _canvas.transform.eulerAngles = new Vector3(-_canvas.transform.eulerAngles.x, 0, 0);
        }
        
        public void HandleCollisionAnimation(Vector3 impulse)
        {
            _self.material.SetVector(Direction, transform.InverseTransformDirection(-impulse));
            Tween.MaterialProperty(_self.material, Panner, 1f, -1f,
                _powerUpBehaviour.StunDuration.Value, Ease.Linear);
        }

        public void HandleDeathAnimation()
        {
            _deathParticle.Play();
            _self.enabled = false;
            _arrow.enabled = false;
            _canvas.enabled = false;
        }
    }
}