using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirSphereCollider : AirCollider
    {
        public Vector3 Center { get; }
        public float Radius { get; }

        public AirSphereCollider(SphereCollider collider, AirFluid fluids)
        {
            var globalCenter = collider.transform.TransformPoint(collider.center);
            var scale = collider.transform.lossyScale;
            var radius = collider.radius * Mathf.Max(scale.x, scale.y, scale.z);
            Center = fluids.WorldToLocal(globalCenter);
            Radius = fluids.WorldToLocal(radius);
        }
    }
}
