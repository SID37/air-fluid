using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AirFluid
{
    public class AirWindSource : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Acceleration transmitted to the stream in meters per second")]
        public Vector3 force;

        public Vector3 WorldForce => transform.rotation * force;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + WorldForce / 10);
            Gizmos.DrawSphere(transform.position + WorldForce / 10, force.magnitude / 100);
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "Light Gizmo.tiff", true);
        }
    }
}
