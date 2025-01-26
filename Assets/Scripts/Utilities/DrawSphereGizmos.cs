using System;
using UnityEngine;

namespace Game.Utilities
{
    public class DrawSphereGizmos : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.02f);
        }
    }
}
