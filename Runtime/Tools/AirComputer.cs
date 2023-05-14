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
            public SphereForceKernel sphereForce;
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
            fluidShader.SetInts("GridSize", new int[] {gridSize.x, gridSize.y, gridSize.z});

            kernels.fill.id = fluidShader.FindKernel("Fill");
            kernels.fill.value = Shader.PropertyToID("FillValue");
            InitCommonParameters(kernels.fill.id);

            kernels.sphereForce.id = fluidShader.FindKernel("SphereForce");
            kernels.sphereForce.value = Shader.PropertyToID("SphereForceValue");
            kernels.sphereForce.center = Shader.PropertyToID("SphereForceCenter");
            kernels.sphereForce.radius = Shader.PropertyToID("SphereForceRadius");
            InitCommonParameters(kernels.sphereForce.id);
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

        public void SphereForce(Vector3 center, float radius, Vector3 force)
        {
            fluidShader.SetVector(kernels.sphereForce.center, LocalToGrid(center));
            fluidShader.SetVector(kernels.sphereForce.value, PackVelocity(force));
            fluidShader.SetFloat(kernels.sphereForce.radius, LocalToGrid(radius));
            DispatchForAllGrid(kernels.sphereForce.id);
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
