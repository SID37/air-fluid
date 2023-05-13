using UnityEditor;
using UnityEngine;

namespace AirFluid
{
    [CustomEditor(typeof(AirFluid))]
    [CanEditMultipleObjects]
    public class AirFluidEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var blocks = (target as AirFluid).blocks;
            var consumption = (long)blocks.x * blocks.y * blocks.z
                * 4 * sizeof(float) // velocity x, y, z + pressure
                * 2 // main + temporary matrix
                * (16 * 16 * 16); // block size 16 x 16 x 16

            if (consumption >= 1024 * 1024 * 1024)
            {
                var gib = consumption / 1024 / 1024 / 1024.0;
                EditorGUILayout.HelpBox($"Video memory consumption {gib} GiB", MessageType.Warning);
            }
            else
            {
                var mib = consumption / 1024 / 1024.0;
                EditorGUILayout.HelpBox($"Video memory consumption {mib} MiB", MessageType.None);
            }
        }
    }
}