using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirComputer
    {
        struct FluidKernels
        {
            public int mainTexture;
            public int tempTexture;
            public FillKernel fill;
            public ProjectionKernel projection;
            public SphereForceKernel sphereForce;
            public CapsuleForceKernel capsuleForce;
            public BoxForceKernel boxForce;
        }

        struct ProjectionKernel
        {
            public int initId;
            public int iterationId;
            public int bakeId;
        }

        struct FillKernel
        {
            public int id;
            public int value;
        }

        struct SphereForceKernel
        {
            public int id;
            public int value;
            public int center;
            public int radius;
        }

        struct CapsuleForceKernel
        {
            public int id;
            public int value;
            public int point;
            public int direction;
            public int dividedHeight2;
            public int radius;
        }

        struct BoxForceKernel
        {
            public int id;
            public int value;
            public int center;
            public int halfSize;
            public int rotation;
        }

        private FluidKernels kernels;
        private ComputeShader fluidShader;

        public Vector3Int Blocks { get; }

        public RenderTexture MainTexture { get; }
        public RenderTexture TempTexture { get; }

        public AirComputer(ComputeShader fluidShader, Vector3Int blocks)
        {
            this.fluidShader = fluidShader;
            this.Blocks = blocks;

            MainTexture = CreateTexture();
            TempTexture = CreateTexture();

            kernels.mainTexture = Shader.PropertyToID("MainTexture");
            kernels.tempTexture = Shader.PropertyToID("TempTexture");
            var gridSize = Blocks * AirConstants.blockSize;
            fluidShader.SetInts("GridSize", new int[] { gridSize.x, gridSize.y, gridSize.z });

            kernels.fill.id = fluidShader.FindKernel("Fill");
            kernels.fill.value = Shader.PropertyToID("FillValue");
            InitCommonParameters(kernels.fill.id);

            kernels.projection.initId = fluidShader.FindKernel("ProjectInit");
            kernels.projection.iterationId = fluidShader.FindKernel("ProjectIteration");
            kernels.projection.bakeId = fluidShader.FindKernel("ProjectBake");
            InitCommonParameters(kernels.projection.initId);
            InitCommonParameters(kernels.projection.iterationId);
            InitCommonParameters(kernels.projection.bakeId);

            kernels.sphereForce.id = fluidShader.FindKernel("SphereForce");
            kernels.sphereForce.value = Shader.PropertyToID("SphereForceValue");
            kernels.sphereForce.center = Shader.PropertyToID("SphereForceCenter");
            kernels.sphereForce.radius = Shader.PropertyToID("SphereForceRadius");
            InitCommonParameters(kernels.sphereForce.id);

            kernels.capsuleForce.id = fluidShader.FindKernel("CapsuleForce");
            kernels.capsuleForce.value = Shader.PropertyToID("CapsuleForceValue");
            kernels.capsuleForce.point = Shader.PropertyToID("CapsuleForcePoint");
            kernels.capsuleForce.direction = Shader.PropertyToID("CapsuleForceDirection");
            kernels.capsuleForce.dividedHeight2 = Shader.PropertyToID("CapsuleForceDividedHeight2");
            kernels.capsuleForce.radius = Shader.PropertyToID("CapsuleForceRadius");
            InitCommonParameters(kernels.capsuleForce.id);

            kernels.boxForce.id = fluidShader.FindKernel("BoxForce");
            kernels.boxForce.value = Shader.PropertyToID("BoxForceValue");
            kernels.boxForce.center = Shader.PropertyToID("BoxForceCenter");
            kernels.boxForce.halfSize = Shader.PropertyToID("BoxForceHalfSize");
            kernels.boxForce.rotation = Shader.PropertyToID("BoxForceRotation");
            InitCommonParameters(kernels.boxForce.id);
        }

        private void InitCommonParameters(int kernelId)
        {
            fluidShader.SetTexture(kernelId, kernels.mainTexture, MainTexture);
            fluidShader.SetTexture(kernelId, kernels.tempTexture, TempTexture);
        }

        public void Fill(Vector4 value)
        {
            fluidShader.SetVector(kernels.fill.value, PackVelocity(value));
            DispatchForAllGrid(kernels.fill.id);
        }

        public void Projection(int iterations = 20)
        {
            DispatchForAllGrid(kernels.projection.initId);
            for (int i = 0; i < iterations; ++i)
                DispatchForAllGrid(kernels.projection.iterationId);
            DispatchForAllGrid(kernels.projection.bakeId);
        }

        public void SphereForce(Vector3 center, float radius, Vector3 force)
        {
            fluidShader.SetVector(kernels.sphereForce.center, LocalToGrid(center));
            fluidShader.SetFloat(kernels.sphereForce.radius, LocalToGrid(radius));
            fluidShader.SetVector(kernels.sphereForce.value, PackVelocity(force));
            DispatchForAllGrid(kernels.sphereForce.id);
        }

        public void CapsuleForce(Vector3 Point1, Vector3 Point2, float radius, Vector3 force)
        {
            Debug.Log($"CapsuleForce({Point1}, {Point2}, {radius}, {force})");
            var point1 = LocalToGrid(Point1);
            var point2 = LocalToGrid(Point2);
            var d = point2 - point1;
            var height2 = d.x * d.x + d.y * d.y + d.z * d.z;
            fluidShader.SetVector(kernels.capsuleForce.point, point1);
            fluidShader.SetVector(kernels.capsuleForce.direction, d);
            fluidShader.SetFloat(kernels.capsuleForce.dividedHeight2, 1 / height2);
            fluidShader.SetFloat(kernels.capsuleForce.radius, LocalToGrid(radius));
            fluidShader.SetVector(kernels.capsuleForce.value, PackVelocity(force));
            DispatchForAllGrid(kernels.capsuleForce.id);
        }

        public void BoxForce(Vector3 center, Vector3 size, Matrix4x4 rotation, Vector3 force)
        {
            fluidShader.SetVector(kernels.boxForce.center, LocalToGrid(center));
            fluidShader.SetVector(kernels.boxForce.halfSize, LocalToGrid(size / 2));
            fluidShader.SetMatrix(kernels.boxForce.rotation, rotation);
            fluidShader.SetVector(kernels.boxForce.value, PackVelocity(force));
            DispatchForAllGrid(kernels.boxForce.id);
        }

        private Vector3 PackVelocity(Vector3 value)
        {
            return new Vector3(value.x / Blocks.x, value.y / Blocks.y, value.z / Blocks.z);
        }

        private Vector3 LocalToGrid(Vector3 position)
        {
            return position * AirConstants.blockSize;
        }

        private float LocalToGrid(float distance)
        {
            return distance * AirConstants.blockSize;
        }

        private void DispatchForAllGrid(int kernelId)
        {
            fluidShader.Dispatch(kernelId, Blocks.x, Blocks.y, Blocks.z * AirConstants.blockSize / 2);
        }

        private RenderTexture CreateTexture()
        {
            RenderTextureFormat format = RenderTextureFormat.ARGBFloat;

            const int bSize = AirConstants.blockSize;
            RenderTexture t = new RenderTexture(bSize * Blocks.x, bSize * Blocks.y, 0, format);
            t.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            t.volumeDepth = bSize * Blocks.z;
            t.enableRandomWrite = true;
            t.Create();
            return t;
        }

        public void Release()
        {
            MainTexture.Release();
            TempTexture.Release();
        }
    }
}
