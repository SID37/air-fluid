#if UNITY_VISUAL_EFFECT_GRAPH
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine;

namespace AirFluid
{
    [AddComponentMenu("VFX/Property Binders/Air Fluid Binder")]
    [VFXBinder("AirFluid/AirFluid")]
    class AirFluidBinder : VFXBinderBase
    {
        public string TransformProperty {
            get {
                return (string)transformProperty;
            }
            set {
                transformProperty = value;
                UpdateSubProperties();
            }
        }

        [VFXPropertyBinding("UnityEditor.VFX.Transform"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("transformProperty")]
        protected ExposedProperty transformProperty = "Transform";
        [VFXPropertyBinding("UnityEngine.Texture3D"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("fieldProperty")]
        protected ExposedProperty fieldProperty = "Texture3D";

        public AirFluid fluids;

        private ExposedProperty Position;
        private ExposedProperty Angles;
        private ExposedProperty Scale;


        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        // /// ???
        // void OnValidate()
        // {
        //     UpdateSubProperties();
        // }

        void UpdateSubProperties()
        {
            Position = transformProperty + "_position";
            Angles = transformProperty + "_angles";
            Scale = transformProperty + "_scale";
        }

        public override bool IsValid(VisualEffect component)
        {
            return fluids != null
                && component.HasVector3((int)Position)
                && component.HasVector3((int)Angles)
                && component.HasVector3((int)Scale);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            var b = fluids.blocks;
            var scale = new Vector3(fluids.Scale * b.x, fluids.Scale * b.y, fluids.Scale * b.z);
            component.SetVector3((int)Position, fluids.transform.position 
                + fluids.transform.rotation * (scale / 2));
            component.SetVector3((int)Angles, fluids.transform.eulerAngles);
            component.SetVector3((int)Scale, scale);
            if (fluids.computer != null)
                component.SetTexture((int)fieldProperty, fluids.computer.MainTexture);
        }

        public override string ToString()
        {
            return string.Format($"Fluid : ('{fieldProperty}', {transformProperty}) -> '{(fluids == null ? "(null)" : fluids.name)}'");
        }
    }
}
#endif
