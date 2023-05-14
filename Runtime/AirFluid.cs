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

        internal AirComputer computer;

        public float Scale => Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        private void Start()
        {
            computer = new AirComputer(m_Compute, blocks, idleVelocity);
        }

        private void FixedUpdate()
        {
            var value = new Vector3(1, 0, 0);
            computer.Fill(new Vector3(value.x / blocks.x, value.y / blocks.y, value.z / blocks.z));
        }

        private void OnValidate()
        {
            if (blocks.x <= 0) blocks.x = 1;
            if (blocks.y <= 0) blocks.y = 1;
            if (blocks.z <= 0) blocks.z = 1;
        }

        Vector3 LocalToWorld(Vector3 point)
        {
            return transform.position + transform.rotation * (point * Scale);
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
