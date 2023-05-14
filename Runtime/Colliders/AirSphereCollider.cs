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
            Center = fluids.WorldToLocal(globalCenter);
            var scale = collider.transform.lossyScale;
            Radius = collider.radius * Mathf.Max(scale.x, scale.y, scale.z) / fluids.Scale;
        }
    }
}
