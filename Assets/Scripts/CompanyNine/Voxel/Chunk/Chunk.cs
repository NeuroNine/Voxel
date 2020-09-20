using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanyNine.Voxel.Chunk
{
    public class Chunk
    {
        private readonly GameObject chunkObject;
        private readonly MeshRenderer meshRenderer;
        private readonly MeshFilter meshFilter;
        private readonly World world;
        private int vertexIndex;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uvs = new List<Vector2>();

        private readonly ushort[,,] blockIdArray =
            new ushort[VoxelData.ChunkWidth, VoxelData.ChunkHeight,
                VoxelData.ChunkWidth];

        private readonly ushort[][][] blockIdArray2 =
            new ushort[VoxelData.ChunkWidth][][];

        public Chunk(World world, ChunkCoordinate chunkCoordinate)
        {
            this.world = world;
            chunkObject = new GameObject();
            chunkObject.transform.SetParent(world.transform);
            chunkObject.transform.position = new Vector3(
                chunkCoordinate.X * VoxelData.ChunkWidth, 0,
                chunkCoordinate.Z * VoxelData.ChunkWidth);
            chunkObject.name = $"Chunk[{chunkCoordinate}]";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            meshFilter = chunkObject.AddComponent<MeshFilter>();
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.material = world.Material;

            PopulateVoxelMap();
            CreateChunkMeshData();
            CreateMesh();
        }

        public bool IsActive
        {
            get => chunkObject.activeSelf;
            set => chunkObject.SetActive(value);
        }

        public Vector3 Position => chunkObject.transform.position;

        /**
     * Populates the block id array of this chunk with the information from world generation.
     */
        private void PopulateVoxelMap()
        {
            for (var x = 0; x < blockIdArray2.Length; x++)
            {
                blockIdArray2[x] = new ushort[VoxelData.ChunkHeight][];
                for (var y = 0; y < blockIdArray2[x].Length; y++)
                {
                    blockIdArray2[x][y] = new ushort[VoxelData.ChunkWidth];
                    for (var z = 0; z < blockIdArray2[x][y].Length; z++)
                    {
                        blockIdArray2[x][y][z] =
                            World.GetVoxel(new Vector3(x, y, z) + Position);
                    }
                }
            }
        }

        private void CreateChunkMeshData()
        {
            for (var y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (var x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    for (var z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        if (world.blockTypes[blockIdArray2[x][y][z]].IsSolid)
                        {
                            AddVoxelDataToChunk(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }

        private void AddVoxelDataToChunk(Vector3Int blockPosition)
        {
            var faces =
                (VoxelData.Face[]) Enum.GetValues(typeof(VoxelData.Face));
            foreach (var face in faces)
            {
                // Checks if this face should be drawn by looking at the voxel that would be next to that face. If the value
                // is within the chunk and solid, then we know we do not need to draw it as another voxel is blocking the 
                // face from view.
                if (IsFaceHidden(blockPosition, face))
                {
                    continue;
                }

                // var blockId = blockIdArray[blockPosition.x, blockPosition.y,
                //     blockPosition.z];

                var blockId = blockIdArray2[blockPosition.x][blockPosition
                    .y][blockPosition.z];

                var blockTexture =
                    world.blockTypes[blockId].GetBlockTexture(face);

                if (blockTexture != null)
                {
                    AddTexture(blockTexture.Id);
                }

                vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 0]]);
                vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 1]]);
                vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 2]]);
                vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 3]]);


                // These values correspond to the indices of the vertex array we have created above.
                // We are reusing the second and third vertex on each face as the first two vertices of the second triangle 
                // to save space.
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
                vertexIndex += 4;
            }
        }

        /// <summary>
        /// Returns true if face should be hidden, otherwise false. 
        /// </summary>
        /// <param name="blockPosition">The local coor</param>
        /// <param name="face"></param>
        /// <returns></returns>
        private bool IsFaceHidden(Vector3Int blockPosition, VoxelData.Face face)
        {
            var neighborBlockPosition =
                blockPosition + VoxelData.ChecksByFace[face];
            var x = neighborBlockPosition.x;
            var y = neighborBlockPosition.y;
            var z = neighborBlockPosition.z;

            if (!IsVoxelInChunk(x, y, z))
            {
                // if the voxel is not in this chunk, then retrieve its id from the world and check if its solid or not.
                return world
                    .blockTypes[
                        World.GetVoxel(Position + neighborBlockPosition)]
                    .IsSolid;
            } 

            // if the voxel is in the chunk then pull its coordinate and check if its solid or not.
            return world.blockTypes[blockIdArray2[x][y][z]].IsSolid;
        }

        /**
     * Returns true if the voxel is in this chunk.
     */
        private static bool IsVoxelInChunk(int x, int y, int z)
        {
            if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 ||
                y >= VoxelData.ChunkHeight || z < 0 ||
                z >= VoxelData.ChunkWidth )
            {
                return false;
            }

            return true;
        }

        private void CreateMesh()
        {
            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };

            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        }

        private void AddTexture(int textureId)
        {
// ReSharper disable once PossibleLossOfFraction
            float y = textureId / VoxelData.TextureAtlasSizeInBlocks;
            var x = textureId - (y * VoxelData.TextureAtlasSizeInBlocks);
            x *= VoxelData.NormalizedBlockTextureSize;
            y *= VoxelData.NormalizedBlockTextureSize;
            y = 1f - y - VoxelData.NormalizedBlockTextureSize;
            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize,
                y + VoxelData.NormalizedBlockTextureSize));
        }

        private bool Equals(Chunk other)
        {
            return Equals(Position, other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Chunk) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}