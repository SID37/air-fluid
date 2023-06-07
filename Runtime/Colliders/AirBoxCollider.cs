using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirBoxCollider : AirCollider
    {
        public Vector3 Size { get; }
        public Vector3 Center { get; }
        public Matrix4x4 Rotation { get; }

        public AirBoxCollider(BoxCollider collider, AirFluid fluids)
        {
            var globalCenter = collider.transform.TransformPoint(collider.center);
            var scale = collider.transform.lossyScale;
            var size = collider.size;
            Size = new Vector3(scale.x * size.x, scale.y * size.y, scale.z * size.z) / fluids.Scale;
            Center = fluids.WorldToLocal(globalCenter);
            Rotation = Matrix4x4.Rotate(Quaternion.Inverse(collider.transform.rotation) * fluids.transform.rotation);
        }
    }
}
