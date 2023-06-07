using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirMeshCollider : AirCollider
    {
        public Matrix4x4 Matrix { get; }
        public Mesh MeshData { get; }
        public Vector3 Center { get; }
        public bool Convex { get; }

        public AirMeshCollider(MeshCollider collider, AirFluid fluids)
        {
            var unScale = 1 / fluids.Scale;
            Matrix =
                Matrix4x4.Rotate(Quaternion.Inverse(fluids.transform.rotation)) *
                Matrix4x4.Scale(new Vector3(unScale, unScale, unScale)) *
                Matrix4x4.Translate(-fluids.transform.position) *
                collider.transform.localToWorldMatrix;

            Center = fluids.WorldToLocal(collider.transform.position);
            MeshData = collider.sharedMesh;
            Convex = collider.convex;
        }
    }
}