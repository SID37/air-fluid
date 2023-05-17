using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AirFluid
{
    public class AirFluid : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Each block is a 16 x 16 x 16 simulation field")]
        public Vector3Int blocks = new Vector3Int(8, 8, 8);

        [SerializeField]
        [Tooltip("Constant velocity at which the flow blows from the outer walls")]
        public Vector3 idleVelocity = new Vector3(0, 0, 0);

        [SerializeField, HideInInspector]
        ComputeShader m_Compute = null;

        class Collisions
        {
            public struct Wind<T> where T : AirCollider
            {
                public T collider;
                public Vector3 force;
            }

            public struct Obstacle<T> where T : AirCollider
            {
                public T collider;
                public Vector3 velocity;
                public Vector3 angularVelocity;
            }

            public List<Wind<AirSphereCollider>> SphereWinds = new();
            public List<Wind<AirCapsuleCollider>> CapsuleWinds = new();
            public List<Wind<AirBoxCollider>> BoxWinds = new();

            public List<Obstacle<AirSphereCollider>> SphereObstacles = new();
            public List<Obstacle<AirCapsuleCollider>> CapsuleObstacles = new();
            public List<Obstacle<AirBoxCollider>> BoxObstacles = new();

            public void Clear()
            {
                SphereWinds.Clear();
                CapsuleWinds.Clear();
                BoxWinds.Clear();
                SphereObstacles.Clear();
                CapsuleObstacles.Clear();
                BoxObstacles.Clear();
            }
        }

        internal AirComputer computer;
        private Collider[] colliders = new Collider[8];
        private Collisions collisions = new Collisions();

        public float Scale => Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        private void OnValidate()
        {
            if (blocks.x <= 0) blocks.x = 1;
            if (blocks.y <= 0) blocks.y = 1;
            if (blocks.z <= 0) blocks.z = 1;
        }

        private void Start()
        {
            computer = new AirComputer(m_Compute, blocks);
            computer.Fill(idleVelocity / Scale);
        }

        private void FixedUpdate()
        {
            var dt = Time.deltaTime;

            UpdateCollisions();
            computer.Advection(dt);
            ApplyWind(dt);
            computer.Projection();
        }

        private void UpdateCollisions()
        {
            LayerMask layerMask = new LayerMask();
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(i, gameObject.layer))
                    layerMask |= 1 << i;
            }

            int count;
            var halfExtensions = new Vector3(Scale * blocks.x, Scale * blocks.y, Scale * blocks.z) / 2;
            while (true)
            {
                count = Physics.OverlapBoxNonAlloc(
                    transform.position + transform.rotation * halfExtensions,
                    halfExtensions,
                    colliders,
                    transform.rotation,
                    layerMask
                );
                if (count < colliders.Length)
                    break;
                colliders = new Collider[colliders.Length * 2];
            }

            collisions.Clear();
            for (int i = 0; i < count; ++i)
            {
                var collider = colliders[i];
                AirWindSource windSource = null;
                Rigidbody rigidbody = null;
                if (collider.isTrigger)
                {
                    windSource = collider.GetComponent<AirWindSource>();
                    if (windSource == null) continue;
                }
                else
                    rigidbody = collider.GetComponentInParent<Rigidbody>(true);

                switch (collider)
                {
                    case SphereCollider sCollider: StoreCollision(new AirSphereCollider(sCollider, this), windSource, rigidbody, collisions.SphereWinds, collisions.SphereObstacles); break;
                    case CapsuleCollider cCollider: StoreCollision(new AirCapsuleCollider(cCollider, this), windSource, rigidbody, collisions.CapsuleWinds, collisions.CapsuleObstacles); break;
                    case BoxCollider bCollider: StoreCollision(new AirBoxCollider(bCollider, this), windSource, rigidbody, collisions.BoxWinds, collisions.BoxObstacles); break;
                    case MeshCollider mCollider: Debug.Log($"MeshCollider {mCollider.sharedMesh}"); break;
                    case TerrainCollider tCollider: Debug.Log($"TerrainCollider {tCollider.terrainData}"); break;
                    case WheelCollider wCollider: Debug.Log($"WheelCollider {wCollider.center}"); break;
                    default: Debug.LogWarning($"Unsupported collider: {collider.GetType()}"); break;
                }
            }
        }

        private void StoreCollision<T>(T collider, AirWindSource windSource, Rigidbody body,
            List<Collisions.Wind<T>> windList, List<Collisions.Obstacle<T>> obstacleList) where T : AirCollider
        {
            if (windSource != null)
            {
                windList.Add(new Collisions.Wind<T>()
                {
                    collider = collider,
                    force = windSource.WorldForce / Scale
                });
            }
            else
            {
                // TODO: store obstacle
            }
        }

        private void ApplyWind(float dt)
        {
            foreach (var sphere in collisions.SphereWinds)
                computer.SphereForce(sphere.collider.Center, sphere.collider.Radius, sphere.force * dt);
            foreach (var capsule in collisions.CapsuleWinds)
                computer.CapsuleForce(capsule.collider.Point1, capsule.collider.Point2, capsule.collider.Radius, capsule.force * dt);
            foreach (var box in collisions.BoxWinds)
                computer.BoxForce(box.collider.Center, box.collider.Size, box.collider.Rotation, box.force * dt);
        }

        internal Vector3 LocalToWorld(Vector3 point)
        {
            return transform.position + transform.rotation * (point * Scale);
        }

        internal Vector3 WorldToLocal(Vector3 point)
        {
            return Quaternion.Inverse(transform.rotation) * (point - transform.position) / Scale;
        }

        internal float WorldToLocal(float distance)
        {
            return distance / Scale;
        }

        void GizmoDrawContour(params Vector3[] p)
        {
            for (int i = 0; i < p.Length; ++i)
                p[i] = LocalToWorld(p[i]);
            for (int i = 0; i <= p.Length; ++i)
                Gizmos.DrawLine(p[i % p.Length], p[(i + 1) % p.Length]);
        }

        void GizmoDrawMeshCube(Vector3Int step)
        {
            for (int i = 0; i <= blocks.x; i += step.x)
                GizmoDrawContour(new Vector3(i, 0, 0), new Vector3(i, 0, blocks.z), new Vector3(i, blocks.y, blocks.z), new Vector3(i, blocks.y, 0));
            for (int i = 0; i <= blocks.y; i += step.y)
                GizmoDrawContour(new Vector3(0, i, 0), new Vector3(0, i, blocks.z), new Vector3(blocks.x, i, blocks.z), new Vector3(blocks.x, i, 0));
            for (int i = 0; i <= blocks.z; i += step.z)
                GizmoDrawContour(new Vector3(0, 0, i), new Vector3(blocks.x, 0, i), new Vector3(blocks.x, blocks.y, i), new Vector3(0, blocks.y, i));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            GizmoDrawMeshCube(new Vector3Int(1, 1, 1));
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            GizmoDrawMeshCube(blocks);
        }
    }
}
