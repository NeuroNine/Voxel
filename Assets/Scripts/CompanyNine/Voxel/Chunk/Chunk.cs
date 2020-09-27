using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanyNine.Voxel.Chunk
{
    public class Chunk
    {
        private GameObject _chunkObject;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private World _world;
        private ChunkCoordinate _coordinate;
        private int _vertexIndex;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        private readonly ushort[][][] _blockIdArray =
            new ushort[VoxelData.ChunkWidth][][];

        private static readonly VoxelData.Face[] Faces =
            (VoxelData.Face[]) Enum.GetValues(typeof(VoxelData.Face));

        private bool _isActive;
        public bool Initialized { get; private set; }

        public Chunk(World world, ChunkCoordinate chunkCoordinate)
        {
            _world = world;
            _coordinate = chunkCoordinate;
            _isActive = true;
        }

        public void Init()
        {
            _chunkObject = new GameObject();
            _chunkObject.transform.SetParent(_world.transform);
            _chunkObject.transform.position = new Vector3(
                _coordinate.X * VoxelData.ChunkWidth, 0,
                _coordinate.Z * VoxelData.ChunkWidth);
            _chunkObject.name = $"Chunk[{_coordinate}]";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            _meshFilter = _chunkObject.AddComponent<MeshFilter>();
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = _world.Material;

            PopulateVoxelMap();
            CreateChunkMeshData();
            CreateMesh();

            Initialized = true;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (Initialized)
                {
                    _chunkObject.SetActive(value);
                }
            }
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
                
                if (IsVoxelSolid(blockPosition + VoxelData.ChecksByFace[face]))
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
                else
                {
                    Debug.Log($"Null Texture: {blockId}, {face}");
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
        /// <param name="blockPosition">The local coordinates</param>
        /// <returns></returns>
        private bool IsVoxelSolid(Vector3Int blockPosition)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!IsVoxelInChunk(blockPosition))
            {
                // if the voxel is not in this chunk, then retrieve its id from the world and check if its solid or not.
                return _world.IsVoxelSolid(Position + blockPosition);
            }

            // if the voxel is in the chunk then pull its coordinate and check if its solid or not.
            return _world
                .blockTypes[
                    _blockIdArray[blockPosition.x]
                        [blockPosition.y]
                        [blockPosition.z]]
                .IsSolid;
        }


        private static bool IsVoxelInChunk(Vector3Int position)
        {
            if (position.x < 0 || position.x >= VoxelData.ChunkWidth ||
                position.y < 0 || position.y >= VoxelData.ChunkHeight ||
                position.z < 0 || position.z >= VoxelData.ChunkWidth)
            {
                return false;
            }

            return true;
        }

        public ushort GetVoxelFromWorldPosition(float x, float y, float z)
        {
            if (!Initialized)
            {
                return 0;
            }

            var xCheck = Mathf.FloorToInt(x);
            var yCheck = Mathf.FloorToInt(y);
            var zCheck = Mathf.FloorToInt(z);

            xCheck -= Mathf.FloorToInt(Position.x);
            zCheck -= Mathf.FloorToInt(Position.z);

            return _blockIdArray[xCheck][yCheck][zCheck];
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