using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirFluid
{
    class AirComputer
    {
        struct FluidKernels
        {
            public FillKernel fill;
        }

        struct FillKernel
        {
            public int Id;
            public int FillValue;
        }

        private FluidKernels kernels;
        private ComputeShader fluidShader;

        public Vector3Int Blocks { get; }
        public Vector3 IdleVelocity { get; private set; }

        public RenderTexture MainTexture { get; }
        public RenderTexture TempTexture { get; }

        public AirComputer(ComputeShader fluidShader, Vector3Int blocks, Vector3 idleVelocity)
        {
            this.fluidShader = fluidShader;
            this.Blocks = blocks;
            this.IdleVelocity = idleVelocity;

            MainTexture = CreateTexture();
            TempTexture = CreateTexture();

            kernels.fill.Id = fluidShader.FindKernel("Fill");
            fluidShader.SetTexture(kernels.fill.Id, "FillResult", MainTexture);
            kernels.fill.FillValue = Shader.PropertyToID("FillValue");
        }

        public void Fill(Vector4 value)
        {
            fluidShader.SetVector(kernels.fill.FillValue, value);
            fluidShader.Dispatch(kernels.fill.Id, Blocks.x, Blocks.y, Blocks.z * AirConstants.blockSize / 2);
        }

        public void FillBlock(Vector4 value)
        {
            fluidShader.SetVector(kernels.fill.FillValue, value);
            fluidShader.Dispatch(kernels.fill.Id, 1, 1, AirConstants.blockSize / 2);
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
