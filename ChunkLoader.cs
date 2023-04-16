using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    [SerializeField, Range(8, 64)]
    int chunkSize = 16;

    [SerializeField, Range(1, 64)]
    int loadRadius = 16;

    [SerializeField, Range(1, 16)]
    int worldHeight = 4;

	const byte VOXEL_Y_SHIFT = 4;
	const byte VOXEL_Z_SHIFT = 8;
    Dictionary<Vector2Int, ushort[]> world = new Dictionary<Vector2Int, ushort[]>();
    Vector2Int[] loadOrder;
    Vector2Int[] buildList;
    Vector2Int[] updateList;
    Vector2Int currentChunk;
    Vector2Int oldChunk;
    bool active = true;
    bool buildQueued = false;
    bool updateQueued = false;

    void Awake()
    {
       	var chunkOffsets = new List<Vector2Int>();
		for (int x = -loadRadius; x <= loadRadius; x++)
		{
			for (int z = -loadRadius; z <= loadRadius; z++)
			{
				chunkOffsets.Add(new Vector2Int(x, z));
			}
		}

		float chunkRadius = loadRadius * 1.55f;

		loadOrder = chunkOffsets
			.Where(pos => Mathf.Abs(pos.x) + Mathf.Abs(pos.y) < chunkRadius)
			.OrderBy(pos => Mathf.Abs(pos.x) + Mathf.Abs(pos.y))
			.ThenBy(pos => Mathf.Abs(pos.x))
			.ThenBy(pos => Mathf.Abs(pos.y))
			.ToArray();

        buildList = new Vector2Int[5];
        updateList = new Vector2Int[1];
    }

    void Update() 
    {
        if (active) 
        {
            if (!buildQueued && !updateQueued) 
            {
                GenerateBuildList();
            }

            if (!updateQueued) 
            {
                BuildChunks();
            }
            else 
            {
                UpdateChunks();
            }
        }
    }

    void GenerateBuildList() 
    {
		currentChunk = GetChunkPosition(transform.position);
			
        for (int i = 0; i < loadOrder.Count(); i++)
        {
            //translate the player position and array position into chunk position
            Vector2Int center = new Vector2Int(
                loadOrder[i].x + currentChunk.x, 
                loadOrder[i].y + currentChunk.y
            );
            
            //Get the chunk in the defined position
            if (world.ContainsKey(center)) {
                continue;
            }

            Vector2Int pos;
            pos = new Vector2Int(center.x, center.y - 1);
            if (!world.ContainsKey(pos)) {
                buildList[0] = pos;
            }
            pos = new Vector2Int(center.x + 1, center.y);
            if (!world.ContainsKey(pos)) {
                buildList[1] = pos;
            }
            pos = new Vector2Int(center.x, center.y + 1);
            if (!world.ContainsKey(pos)) {
                buildList[2] = pos;
            }
            pos = new Vector2Int(center.x - 1, center.y);
            if (!world.ContainsKey(pos)) {
                buildList[3] = pos;
            }

            buildList[4] = center;
            updateList[0] = center;
                
            oldChunk = currentChunk;
            buildQueued = true;
            break;
        }
    }

    void BuildChunks()
    {
        for (int i = 0; i < buildList.Count(); i++)
        {
            var chunk = new ushort[chunkSize * (chunkSize * worldHeight) * chunkSize];
            for (uint x = 0; x < chunkSize; x++) {
                for (uint y = 0; y < chunkSize * worldHeight; y++) {
                    for (uint z = 0; z < chunkSize; z++) {
                        if (y == 0) 
                        {
                            chunk[x | y << VOXEL_Y_SHIFT | z << VOXEL_Z_SHIFT] = 1;
                        }
                        else 
                        {
                            chunk[x | y << VOXEL_Y_SHIFT | z << VOXEL_Z_SHIFT] = 0;
                        }
                    }
                } 
            }
        }

        buildQueued = false;
        updateQueued = true;
    }

    void UpdateChunks()
    {

    }

     Vector2Int GetChunkPosition(Vector3 pos)
	{
		return new Vector2Int(
			Mathf.FloorToInt(pos.x / chunkSize),
			Mathf.FloorToInt(pos.z / chunkSize)
		);
	}
}
