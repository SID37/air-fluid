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
            public AdvectionKernel advection;
            public CollidersKernel colliders;
            public ForcesKernel forces;
        }

        struct ProjectionKernel
        {
            public int initId;
            public int iterationId;
            public int bakeId;
        }

        struct AdvectionKernel
        {
            public int id;
            public int deltaTime;
            public int tempToMainId;
        }

        struct FillKernel
        {
            public int id;
            public int value;
        }

        struct CollidersKernel
        {
            public int sphereCenter;
            public int sphereRadius;
            public int radius;
            public int capsulePoint;
            public int capsuleDirection;
            public int capsuleDividedHeight2;
            public int capsuleRadius;
            public int boxCenter;
            public int boxHalfSize;
            public int boxRotation;
        }

        struct ForcesKernel
        {
            public int sphereId;
            public int capsuleId;
            public int boxId;
            public int forceValue;
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

            kernels.advection.id = fluidShader.FindKernel("Advection");
            kernels.advection.deltaTime = Shader.PropertyToID("AdvectionDeltaTime");
            kernels.advection.tempToMainId = fluidShader.FindKernel("TempToMain");
            InitCommonParameters(kernels.advection.id);
            InitCommonParameters(kernels.advection.tempToMainId);

            kernels.colliders.sphereCenter = Shader.PropertyToID("SphereCenter");
            kernels.colliders.sphereRadius = Shader.PropertyToID("SphereRadius");
            kernels.colliders.capsulePoint = Shader.PropertyToID("CapsulePoint");
            kernels.colliders.capsuleDirection = Shader.PropertyToID("CapsuleDirection");
            kernels.colliders.capsuleDividedHeight2 = Shader.PropertyToID("CapsuleDividedHeight2");
            kernels.colliders.capsuleRadius = Shader.PropertyToID("CapsuleRadius");
            kernels.colliders.boxCenter = Shader.PropertyToID("BoxCenter");
            kernels.colliders.boxHalfSize = Shader.PropertyToID("BoxHalfSize");
            kernels.colliders.boxRotation = Shader.PropertyToID("BoxRotation");

            kernels.forces.sphereId = fluidShader.FindKernel("SphereForce");
            kernels.forces.capsuleId = fluidShader.FindKernel("CapsuleForce");
            kernels.forces.boxId = fluidShader.FindKernel("BoxForce");
            kernels.forces.forceValue = Shader.PropertyToID("ForceValue");
            InitCommonParameters(kernels.forces.sphereId);
            InitCommonParameters(kernels.forces.capsuleId);
            InitCommonParameters(kernels.forces.boxId);
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

        public void Advection(float dt)
        {
            fluidShader.SetFloat(kernels.advection.deltaTime, dt);
            DispatchForAllGrid(kernels.advection.id);
            DispatchForAllGrid(kernels.advection.tempToMainId);
        }

        public void SphereForce(Vector3 center, float radius, Vector3 force)
        {
            Debug.Log($"SphereForce({center}, {radius}, {force})");
            fluidShader.SetVector(kernels.colliders.sphereCenter, LocalToGrid(center));
            fluidShader.SetFloat(kernels.colliders.sphereRadius, LocalToGrid(radius));
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.sphereId);
        }

        public void CapsuleForce(Vector3 Point1, Vector3 Point2, float radius, Vector3 force)
        {
            var point1 = LocalToGrid(Point1);
            var point2 = LocalToGrid(Point2);
            var d = point2 - point1;
            var height2 = d.x * d.x + d.y * d.y + d.z * d.z;
            fluidShader.SetVector(kernels.colliders.capsulePoint, point1);
            fluidShader.SetVector(kernels.colliders.capsuleDirection, d);
            fluidShader.SetFloat(kernels.colliders.capsuleDividedHeight2, 1 / height2);
            fluidShader.SetFloat(kernels.colliders.capsuleRadius, LocalToGrid(radius));
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.capsuleId);
        }

        public void BoxForce(Vector3 center, Vector3 size, Matrix4x4 rotation, Vector3 force)
        {
            fluidShader.SetVector(kernels.colliders.boxCenter, LocalToGrid(center));
            fluidShader.SetVector(kernels.colliders.boxHalfSize, LocalToGrid(size / 2));
            fluidShader.SetMatrix(kernels.colliders.boxRotation, rotation);
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.boxId);
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
