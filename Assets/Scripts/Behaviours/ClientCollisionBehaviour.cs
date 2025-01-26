using System;
using System.Collections;
using Game.Managers;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Behaviours
{
    public class ClientCollisionBehaviour : NetworkBehaviour
    {
        public static event Action<Vector3, ulong> CollisionReceived;

        [SerializeField] private NetworkRigidbody _rigidbody;

        public NetworkVariable<long> LastHitPlayerId = new();

        public bool Collided;

        private Coroutine _waitCoroutine;

        [SerializeField] private ClientPowerUpBehaviour _powerUpBehaviour;
        
        public ClientViewBehaviour ViewBehaviour;
        
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                LastHitPlayerId.Value = (long)OwnerClientId;
                CollisionReceived += OnCollisionReceived;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                LastHitPlayerId.Value = (long)OwnerClientId;
                CollisionReceived -= OnCollisionReceived;
            }
            
            base.OnNetworkDespawn();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!IsOwner)
            {
                if (other.collider.CompareTag("Player"))
                {
                    ViewBehaviour.HandleCollisionAnimation(other.contacts[0].normal.normalized);
                }
                return;
            }

            if (other.collider.CompareTag("Respawn"))
            {
                HandleDeath();
                return;
            }

            if (!other.collider.CompareTag("Player")) return;

            var networkObject = other.collider.GetComponent<NetworkObject>();
            if (networkObject == null) return;
            
            HandleCollisionWithPlayer(other, networkObject);

            SetLastHitPlayerId(networkObject.OwnerClientId);
        }

        private void HandleCollisionWithPlayer(Collision other, NetworkObject networkObject)
        {
            if (!IsOwner)
            {
                return;
            }

            Debug.Log($"Collision with {networkObject.OwnerClientId}");
            Collided = true;
            var force = other.contacts[0].normal.normalized * _powerUpBehaviour.HitPower.Value;
            _rigidbody.Rigidbody.AddForce(force);
            ViewBehaviour.HandleCollisionAnimation(force.normalized);

            SendCollisionRpc(-force, OwnerClientId, RpcTarget.Single(networkObject.OwnerClientId, RpcTargetUse.Temp));
            
            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            _waitCoroutine = StartCoroutine(WaitAndResetCollision());
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SendCollisionRpc(Vector3 force, ulong hitterId, RpcParams rpcParams)
        {
            ViewBehaviour.HandleCollisionAnimation(-force.normalized);

            CollisionReceived?.Invoke(force, hitterId);
        }
        
        private void OnCollisionReceived(Vector3 force, ulong hitterId)
        {
            Debug.Log($"Received collision from {hitterId}");
            
            SetLastHitPlayerId(hitterId);

            Collided = true;

            _rigidbody.Rigidbody.AddForce(force);

            ViewBehaviour.HandleCollisionAnimation(force.normalized);

            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            _waitCoroutine = StartCoroutine(WaitAndResetCollision());
        }
        

        private IEnumerator WaitAndResetCollision()
        {
            yield return new WaitForSeconds(_powerUpBehaviour.StunDuration.Value);
            Collided = false;
            _waitCoroutine = null;
        }

        private void SetLastHitPlayerId(ulong lastHitPlayerId)
        {
            LastHitPlayerId.Value = (long)lastHitPlayerId;
        }

        private void HandleDeath()
        {
            HandleDeathRpc(OwnerClientId, (ulong)LastHitPlayerId.Value);
        }
        
        [Rpc(SendTo.Everyone)]
        private void HandleDeathRpc(ulong victimId, ulong killerId)
        {
            GameManager.Instance.PlayerDied(victimId, killerId);
        }

        public void ResetLastHitPlayerId()
        {
            if (!IsOwner)
            {
                return;
            }
            LastHitPlayerId.Value = (long)OwnerClientId;
        }
    }
}