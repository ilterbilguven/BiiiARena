using System;
using Game.Managers;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Behaviours
{
    public class ClientMovementBehaviour : NetworkBehaviour
    {
        /// <summary>
        /// Movement Speed
        /// </summary>
        public float Speed = 5;

        public NetworkRigidbody rb;

        private bool _falling;
        
        private Ray X1Ray => new(transform.position + Vector3.right * _mesh.transform.localScale.x / 2f, Vector3.down);
        private Ray X2Ray => new(transform.position - Vector3.right * _mesh.transform.localScale.x / 2f, Vector3.down);
        private Ray Z1Ray => new(transform.position + Vector3.forward * _mesh.transform.localScale.z / 2f, Vector3.down);
        private Ray Z2Ray => new(transform.position - Vector3.forward* _mesh.transform.localScale.z / 2f, Vector3.down);
        
        private bool _x1Hit;
        private bool _x2Hit;
        private bool _z1Hit;
        private bool _z2Hit;

        [SerializeField] private ClientCollisionBehaviour _collisionBehaviour;

        [SerializeField] private Transform _mesh;
        
        public override void OnNetworkSpawn()
        {
#if UNITY_EDITOR
            if (OwnerClientId != 1)
            {
                Speed = 1;
            }
#endif
            base.OnNetworkSpawn();
        }

        private void FixedUpdate()
        {
            if (!IsOwner || !IsSpawned || _falling || _collisionBehaviour.Collided || !GameManager.Instance.Started) return;

            if (Physics.Raycast(X1Ray, out var hit, 100))
            {
                _x1Hit = hit.collider.CompareTag("Respawn");
            }

            if (Physics.Raycast(X2Ray, out hit, 100))
            {
                _x2Hit = hit.collider.CompareTag("Respawn");
            }

            if (Physics.Raycast(Z1Ray, out hit, 100))
            {
                _z1Hit = hit.collider.CompareTag("Respawn");
            }

            if (Physics.Raycast(Z2Ray, out hit, 100))
            {
                _z2Hit = hit.collider.CompareTag("Respawn");
            }

            if (_x1Hit && _x2Hit && _z1Hit && _z2Hit)
            {
                _falling = true;
                return;
            }

            var multiplier = Speed * Time.fixedDeltaTime;
            
            var gamepadValue = Vector3.right * Gamepad.current.leftStick.ReadValue().x + Vector3.forward * Gamepad.current.leftStick.ReadValue().y;
            // var gamepadValue = Vector3.right * Gamepad.all[(int)(OwnerClientId - 1)].leftStick.ReadValue().x + Vector3.forward * Gamepad.all[(int)(OwnerClientId - 1)].leftStick.ReadValue().y;

            if (gamepadValue != Vector3.zero)
            {
                rb.transform.forward = gamepadValue;
            }

            rb.SetLinearVelocity(rb.transform.forward * multiplier);
        }

        private void OnDrawGizmos()
        {
            if (!IsOwner || !IsSpawned || _falling) return;

            Gizmos.color = _x1Hit ? Color.red : Color.green;
            Gizmos.DrawRay(X1Ray);
            Gizmos.color = _x2Hit ? Color.red : Color.green;
            Gizmos.DrawRay(X2Ray);
            Gizmos.color = _z1Hit ? Color.red : Color.green;
            Gizmos.DrawRay(Z1Ray);
            Gizmos.color = _z2Hit ? Color.red : Color.green;
            Gizmos.DrawRay(Z2Ray);
        }
    }
}
