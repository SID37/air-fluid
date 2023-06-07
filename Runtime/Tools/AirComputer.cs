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
            public int projTexture;
            public FillKernel fill;
            public ProjectionKernel projection;
            public AdvectionKernel advection;
            public CollidersKernel colliders;
            public ForcesKernel forces;
            public ObstaclesKernel obstacles;
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
            public int meshVertexes;
            public int meshTriangles;
            public int meshTriangleCount;
        }

        struct ForcesKernel
        {
            public int sphereId;
            public int capsuleId;
            public int boxId;
            public int forceValue;
        }

        struct ObstaclesKernel
        {
            public int sphereId;
            public int capsuleId;
            public int boxId;
            public int meshId;
            public int velocity;
            public int angularVelocity;
            public int rotationCenter;
        }

        private FluidKernels kernels;
        private ComputeShader fluidShader;

        public Vector3Int Blocks { get; }

        public RenderTexture MainTexture { get; }
        public RenderTexture TempTexture { get; }
        public RenderTexture ProjTexture { get; }

        ComputeBuffer vertexBuffer;
        ComputeBuffer trianglesBuffer;

        public AirComputer(ComputeShader fluidShader, Vector3Int blocks)
        {
            this.fluidShader = fluidShader;
            this.Blocks = blocks;

            MainTexture = CreateTexture(RenderTextureFormat.ARGBHalf);
            TempTexture = CreateTexture(RenderTextureFormat.ARGBHalf);
            ProjTexture = CreateTexture(RenderTextureFormat.RGHalf);

            kernels.mainTexture = Shader.PropertyToID("MainTexture");
            kernels.tempTexture = Shader.PropertyToID("TempTexture");
            kernels.projTexture = Shader.PropertyToID("ProjTexture");
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
            InitCommonParameters(kernels.advection.id);

            kernels.colliders.sphereCenter = Shader.PropertyToID("SphereCenter");
            kernels.colliders.sphereRadius = Shader.PropertyToID("SphereRadius");
            kernels.colliders.capsulePoint = Shader.PropertyToID("CapsulePoint");
            kernels.colliders.capsuleDirection = Shader.PropertyToID("CapsuleDirection");
            kernels.colliders.capsuleDividedHeight2 = Shader.PropertyToID("CapsuleDividedHeight2");
            kernels.colliders.capsuleRadius = Shader.PropertyToID("CapsuleRadius");
            kernels.colliders.boxCenter = Shader.PropertyToID("BoxCenter");
            kernels.colliders.boxHalfSize = Shader.PropertyToID("BoxHalfSize");
            kernels.colliders.boxRotation = Shader.PropertyToID("BoxRotation");
            kernels.colliders.meshVertexes = Shader.PropertyToID("MeshVertexes");
            kernels.colliders.meshTriangles = Shader.PropertyToID("MeshTriangles");
            kernels.colliders.meshTriangleCount = Shader.PropertyToID("MeshTriangleCount");

            kernels.forces.sphereId = fluidShader.FindKernel("SphereForce");
            kernels.forces.capsuleId = fluidShader.FindKernel("CapsuleForce");
            kernels.forces.boxId = fluidShader.FindKernel("BoxForce");
            kernels.forces.forceValue = Shader.PropertyToID("ForceValue");
            InitCommonParameters(kernels.forces.sphereId);
            InitCommonParameters(kernels.forces.capsuleId);
            InitCommonParameters(kernels.forces.boxId);

            kernels.obstacles.sphereId = fluidShader.FindKernel("SphereObstacle");
            kernels.obstacles.capsuleId = fluidShader.FindKernel("CapsuleObstacle");
            kernels.obstacles.boxId = fluidShader.FindKernel("BoxObstacle");
            kernels.obstacles.meshId = fluidShader.FindKernel("ConvexMeshObstacle");
            kernels.obstacles.velocity = Shader.PropertyToID("ObstacleVelocity");
            kernels.obstacles.angularVelocity = Shader.PropertyToID("ObstacleAngularVelocity");
            kernels.obstacles.rotationCenter = Shader.PropertyToID("ObstacleRotationCenter");
            InitCommonParameters(kernels.obstacles.sphereId);
            InitCommonParameters(kernels.obstacles.capsuleId);
            InitCommonParameters(kernels.obstacles.boxId);
            InitCommonParameters(kernels.obstacles.meshId);
        }

        private void InitCommonParameters(int kernelId)
        {
            fluidShader.SetTexture(kernelId, kernels.mainTexture, MainTexture);
            fluidShader.SetTexture(kernelId, kernels.tempTexture, TempTexture);
            fluidShader.SetTexture(kernelId, kernels.projTexture, ProjTexture);
        }

        public void Fill(Vector4 value)
        {
            fluidShader.SetVector(kernels.fill.value, PackVelocity(value));
            DispatchForAllGrid(kernels.fill.id);
        }

        public void Projection(int iterations = 10)
        {
            DispatchForAllGrid(kernels.projection.initId);
            for (int i = 0; i < iterations; ++i)
                fluidShader.Dispatch(kernels.projection.iterationId,
                    Blocks.x, Blocks.y,
                    Blocks.z * AirConstants.blockSize / 4); // Z_ITERATIONS
            DispatchForAllGrid(kernels.projection.bakeId);
        }

        public void Advection(float dt)
        {
            fluidShader.SetFloat(kernels.advection.deltaTime, dt);
            DispatchForAllGrid(kernels.advection.id);
        }

        public void SphereForce(Vector3 center, float radius, Vector3 force)
        {
            ConfigureSphere(center, radius);
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.sphereId);
        }

        public void CapsuleForce(Vector3 point1, Vector3 point2, float radius, Vector3 force)
        {
            ConfigureCapsule(point1, point2, radius);
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.capsuleId);
        }

        public void BoxForce(Vector3 center, Vector3 size, Matrix4x4 rotation, Vector3 force)
        {
            ConfigureBox(center, size, rotation);
            fluidShader.SetVector(kernels.forces.forceValue, PackVelocity(force));
            DispatchForAllGrid(kernels.forces.boxId);
        }

        public void SphereObstacle(Vector3 center, float radius, Vector3 velocity, Vector3 angularVelocity)
        {
            ConfigureSphere(center, radius);
            ConfigureObstacle(velocity, angularVelocity, center);
            DispatchForAllGrid(kernels.obstacles.sphereId);
        }

        public void CapsuleObstacle(Vector3 point1, Vector3 point2, float radius, Vector3 velocity, Vector3 angularVelocity)
        {
            ConfigureCapsule(point1, point2, radius);
            ConfigureObstacle(velocity, angularVelocity, (point1 + point2) / 2);
            DispatchForAllGrid(kernels.obstacles.capsuleId);
        }

        public void BoxObstacle(Vector3 center, Vector3 size, Matrix4x4 rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            ConfigureBox(center, size, rotation);
            ConfigureObstacle(velocity, angularVelocity, center);
            DispatchForAllGrid(kernels.obstacles.boxId);
        }

        public void ConvexMeshObstacle(Matrix4x4 matrix, Mesh mesh, Vector3 center, Vector3 velocity, Vector3 angularVelocity)
        {
            ConfigureMesh(kernels.obstacles.meshId, matrix, mesh);
            ConfigureObstacle(velocity, angularVelocity, center);
            DispatchForAllGrid(kernels.obstacles.meshId);
        }

        private void ConfigureObstacle(Vector3 velocity, Vector3 angularVelocity, Vector3 center)
        {
            fluidShader.SetVector(kernels.obstacles.velocity, PackVelocity(velocity));
            fluidShader.SetVector(kernels.obstacles.angularVelocity, angularVelocity);
            fluidShader.SetVector(kernels.obstacles.rotationCenter, LocalToGrid(center));
        }

        private void ConfigureSphere(Vector3 center, float radius)
        {
            fluidShader.SetVector(kernels.colliders.sphereCenter, LocalToGrid(center));
            fluidShader.SetFloat(kernels.colliders.sphereRadius, LocalToGrid(radius));
        }

        private void ConfigureCapsule(Vector3 point1, Vector3 point2, float radius)
        {
            var p1 = LocalToGrid(point1);
            var p2 = LocalToGrid(point2);
            var d = p2 - p1;
            var height2 = d.x * d.x + d.y * d.y + d.z * d.z;
            fluidShader.SetVector(kernels.colliders.capsulePoint, p1);
            fluidShader.SetVector(kernels.colliders.capsuleDirection, d);
            fluidShader.SetFloat(kernels.colliders.capsuleDividedHeight2, 1 / height2);
            fluidShader.SetFloat(kernels.colliders.capsuleRadius, LocalToGrid(radius));
        }

        private void ConfigureBox(Vector3 center, Vector3 size, Matrix4x4 rotation)
        {
            fluidShader.SetVector(kernels.colliders.boxCenter, LocalToGrid(center));
            fluidShader.SetVector(kernels.colliders.boxHalfSize, LocalToGrid(size / 2));
            fluidShader.SetMatrix(kernels.colliders.boxRotation, rotation);
        }

        private void ConfigureMesh(int kernel, Matrix4x4 matrix, Mesh mesh)
        {
            Vector3[] vertexes = new Vector3[mesh.vertices.Length];
            for (int i = 0; i < vertexes.Length; ++i)
            {
                var v = mesh.vertices[i];
                vertexes[i] = LocalToGrid(matrix * new Vector4(v.x, v.y, v.z, 1));
            }

            if (vertexBuffer == null || vertexBuffer.count < vertexes.Length)
            {
                vertexBuffer?.Release();
                vertexBuffer = new ComputeBuffer(vertexes.Length, sizeof(float) * 3);
            }

            if (trianglesBuffer == null || trianglesBuffer.count < mesh.triangles.Length)
            {
                trianglesBuffer?.Release();
                trianglesBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
            }

            vertexBuffer.SetData(vertexes);
            trianglesBuffer.SetData(mesh.triangles);

            fluidShader.SetBuffer(kernel, kernels.colliders.meshVertexes, vertexBuffer);
            fluidShader.SetBuffer(kernel, kernels.colliders.meshTriangles, trianglesBuffer);
            fluidShader.SetInt(kernels.colliders.meshTriangleCount, mesh.triangles.Length / 3);
        }

        private Vector3 PackVelocity(Vector3 value)
        {
            return new Vector3(value.x / Blocks.x, value.y / Blocks.y, value.z / Blocks.z);
        }

        private Matrix4x4 LocalToGrid(Matrix4x4 transform)
        {
            var b = AirConstants.blockSize;
            return Matrix4x4.Scale(new Vector3(b, b, b)) * transform;
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

        private RenderTexture CreateTexture(RenderTextureFormat format)
        {
            const int bSize = AirConstants.blockSize;
            RenderTexture t = new RenderTexture(bSize * Blocks.x, bSize * Blocks.y, 0, format);
            t.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            t.volumeDepth = bSize * Blocks.z;
            t.enableRandomWrite = true;
            t.Create();
            return t;
        }

        // private ComputeBuffer CreateBuffer(int size, int stride)
        // {
        //     ComputeBuffer buffer = new ComputeBuffer(size, stride);

        //     Texture2D texture = new Texture2D(size, 0, format, -1, false);
        //     texture.SetPixels();
        //     // const int bSize = AirConstants.blockSize;
        //     // RenderTexture t = new RenderTexture(bSize * Blocks.x, bSize * Blocks.y, 0, format);
        //     // t.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        //     // t.volumeDepth = bSize * Blocks.z;
        //     // t.enableRandomWrite = true;
        //     // t.Create();
        //     // return t;

        //         ShaderCube[] cube_arr = cube_data.ToArray();
        //         ShaderCube[] output_arr = cube_data.ToArray();
        //         ComputeBuffer cubeBuffer = new ComputeBuffer(x_cubes*y_cubes*z_cubes,sizeof(float)*36);
        //         int kernel = shader.FindKernel("CSMain");

        //         cubeBuffer.SetData(cube_arr);
        //         shader.SetFloat("isoLevel",isoLevel);
        //         shader.SetBuffer(kernel, "_Cubes", cubeBuffer);

        //         shader.Dispatch(kernel, 16, 1, 1);
        //         cubeBuffer.GetData(output_arr);
        //         cubeBuffer.Release();
        // }

        public void Release()
        {
            MainTexture.Release();
            TempTexture.Release();
            vertexBuffer?.Release();
            trianglesBuffer?.Release();
        }
    }
}
