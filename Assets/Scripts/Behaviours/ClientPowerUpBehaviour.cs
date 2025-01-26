using PrimeTween;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Behaviours
{
    public class ClientPowerUpBehaviour : NetworkBehaviour
    {
        public NetworkVariable<float> HitPower;
        public NetworkVariable<uint> KillCount;
        public NetworkVariable<float> HitPowerModifier;
        public NetworkVariable<float> ScaleAfterKillModifier;
        
        public NetworkVariable<float> StunDuration;

        public NetworkTransform NetworkTransform;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void OnKillRpc(RpcParams rpcParams)
        {
            KillCount.Value++;
            HitPower.Value += HitPower.Value * HitPowerModifier.Value;
            
            Tween.Scale(NetworkTransform.transform, Vector3.one,
                NetworkTransform.transform.localScale + Vector3.one * ScaleAfterKillModifier.Value, 
                0.5f, Ease.OutBack);
            Debug.Log("Power up!");
        }
    }
}