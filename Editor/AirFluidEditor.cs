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
            var bSize = AirConstants.blockSize;
            var consumption = (long)blocks.x * blocks.y * blocks.z
                * (2 * 4 + 2) * 2       // (velocity x, y, z, w * 2 (main and temp) + div, p) * sizeof(half)
                * bSize * bSize * bSize;

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