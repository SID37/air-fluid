using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirCapsuleCollider : AirCollider
    {
        public Vector3 Point1 { get; }
        public Vector3 Point2 { get; }
        public float Radius { get; }

        public AirCapsuleCollider(CapsuleCollider collider, AirFluid fluids)
        {
            var scale = collider.transform.lossyScale;
            var radius = collider.radius;
            var height = collider.height;
            if (collider.direction == 0) radius *= Mathf.Max(scale.y, scale.z);
            if (collider.direction == 1) radius *= Mathf.Max(scale.x, scale.z);
            if (collider.direction == 2) radius *= Mathf.Max(scale.x, scale.y);
            if (collider.direction == 0) height *= scale.x;
            if (collider.direction == 1) height *= scale.y;
            if (collider.direction == 2) height *= scale.z;
            height = height - 2 * radius;
            height = height < 0 ? 0 : height;
            var direction = collider.transform.rotation * new Vector3(
                collider.direction == 0 ? height / 2 : 0,
                collider.direction == 1 ? height / 2 : 0,
                collider.direction == 2 ? height / 2 : 0
            );

            var center = collider.transform.TransformPoint(collider.center);
            Point1 = fluids.WorldToLocal(center - direction);
            Point2 = fluids.WorldToLocal(center + direction);
            Radius = fluids.WorldToLocal(radius);
        }
    }
}
