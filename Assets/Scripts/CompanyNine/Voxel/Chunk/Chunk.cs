using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanyNine.Voxel.Chunk
{
    public class Chunk
    {
        private readonly GameObject _chunkObject;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly World _world;
        private int _vertexIndex;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        private readonly ushort[][][] _blockIdArray =
            new ushort[VoxelData.ChunkWidth][][];

        private static readonly VoxelData.Face[] Faces =
            (VoxelData.Face[]) Enum.GetValues(typeof(VoxelData.Face));

        public Chunk(World world, ChunkCoordinate chunkCoordinate)
        {
            this._world = world;
            _chunkObject = new GameObject();
            _chunkObject.transform.SetParent(world.transform);
            _chunkObject.transform.position = new Vector3(
                chunkCoordinate.X * VoxelData.ChunkWidth, 0,
                chunkCoordinate.Z * VoxelData.ChunkWidth);
            _chunkObject.name = $"Chunk[{chunkCoordinate}]";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            _meshFilter = _chunkObject.AddComponent<MeshFilter>();
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
            
            _meshRenderer.material = world.Material;

            PopulateVoxelMap();
            CreateChunkMeshData();
            CreateMesh();
        }

        public bool IsActive
        {
            get => _chunkObject.activeSelf;
            set => _chunkObject.SetActive(value);
        }

        public Vector3 Position => _chunkObject.transform.position;

        /**
     * Populates the block id array of this chunk with the information from world generation.
     */
        private void PopulateVoxelMap()
        {
            for (var x = 0; x < _blockIdArray.Length; x++)
            {
                _blockIdArray[x] = new ushort[VoxelData.ChunkHeight][];
                for (var y = 0; y < _blockIdArray[x].Length; y++)
                {
                    _blockIdArray[x][y] = new ushort[VoxelData.ChunkWidth];
                    for (var z = 0; z < _blockIdArray[x][y].Length; z++)
                    {
                        _blockIdArray[x][y][z] =
                          _world.GetVoxel(new Vector3(x, y, z) + Position);
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
                        if (_world.blockTypes[_blockIdArray[x][y][z]].IsSolid)
                        {
                            AddVoxelDataToChunk(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }

        private void AddVoxelDataToChunk(Vector3Int blockPosition)
        {
            foreach (var face in Faces)
            {
                // Checks if this face should be drawn by looking at the voxel that would be next to that face. If the value
                // is within the chunk and solid, then we know we do not need to draw it as another voxel is blocking the 
                // face from view.
                if (IsFaceHidden(blockPosition, face))
                {
                    continue;
                }

                var blockId = _blockIdArray[blockPosition.x][blockPosition
                    .y][blockPosition.z];

                var blockTexture =
                    _world.blockTypes[blockId].GetBlockTexture(face);

                if (blockTexture != null)
                {
                    AddTexture(blockTexture.Id);
                }

                _vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 0]]);
                _vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 1]]);
                _vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 2]]);
                _vertices.Add(blockPosition + VoxelData.VoxelVertices[VoxelData
                    .VoxelTriangles[(int) face, 3]]);


                // These values correspond to the indices of the vertex array we have created above.
                // We are reusing the second and third vertex on each face as the first two vertices of the second triangle 
                // to save space.
                _triangles.Add(_vertexIndex);
                _triangles.Add(_vertexIndex + 1);
                _triangles.Add(_vertexIndex + 2);
                _triangles.Add(_vertexIndex + 2);
                _triangles.Add(_vertexIndex + 1);
                _triangles.Add(_vertexIndex + 3);
                _vertexIndex += 4;
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
                return _world
                    .blockTypes[
                        _world.GetVoxel(Position + neighborBlockPosition)]
                    .IsSolid;
            }

            // if the voxel is in the chunk then pull its coordinate and check if its solid or not.
            return _world.blockTypes[_blockIdArray[x][y][z]].IsSolid;
        }

        /**
     * Returns true if the voxel is in this chunk.
     */
        private static bool IsVoxelInChunk(int x, int y, int z)
        {
            if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 ||
                y >= VoxelData.ChunkHeight || z < 0 ||
                z >= VoxelData.ChunkWidth)
            {
                return false;
            }

            return true;
        }

        private void CreateMesh()
        {
            var mesh = new Mesh
            {
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray(),
                uv = _uvs.ToArray()
            };

            mesh.RecalculateNormals();
            _meshFilter.mesh = mesh;
        }

        private void AddTexture(int textureId)
        {
// ReSharper disable once PossibleLossOfFraction
            float y = textureId / VoxelData.TextureAtlasSizeInBlocks;
            var x = textureId - (y * VoxelData.TextureAtlasSizeInBlocks);
            x *= VoxelData.NormalizedBlockTextureSize;
            y *= VoxelData.NormalizedBlockTextureSize;
            y = 1f - y - VoxelData.NormalizedBlockTextureSize;
            _uvs.Add(new Vector2(x, y));
            _uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
            _uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
            _uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize,
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