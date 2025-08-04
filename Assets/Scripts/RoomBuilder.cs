using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    public GameObject floorPrefab;     // e.g., Tile_A (4x4 units)
    public GameObject wallPrefab;      // e.g., Wall_A (4 units long)

    public Vector2Int roomSize = new Vector2Int(4, 4); // Width x Depth in tiles
    public float tileSize = 4f; // Size of one tile and wall segment

    void Start()
    {
        BuildRoom(Vector3.zero);
    }

    void BuildRoom(Vector3 origin)
    {
        // 1. Build floor grid
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int z = 0; z < roomSize.y; z++)
            {
                Vector3 floorPos = origin + new Vector3(x * tileSize, 0, z * tileSize);
                Instantiate(floorPrefab, floorPos, Quaternion.identity, transform);
            }
        }

        int width = roomSize.x;
        int depth = roomSize.y;

        // 2. Build walls along perimeter

        // South wall (Z min)
        for (int x = 0; x < width; x++)
        {
            Vector3 pos = origin + new Vector3(x * tileSize, 0, -tileSize);
            Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        }

        // North wall (Z max)
        for (int x = 0; x < width; x++)
        {
            Vector3 pos = origin + new Vector3(x * tileSize, 0, depth * tileSize);
            Instantiate(wallPrefab, pos, Quaternion.Euler(0, 180, 0), transform);
        }

        // West wall (X min)
        for (int z = 0; z < depth; z++)
        {
            Vector3 pos = origin + new Vector3(-tileSize, 0, z * tileSize);
            Instantiate(wallPrefab, pos, Quaternion.Euler(0, -90, 0), transform);
        }

        // East wall (X max)
        for (int z = 0; z < depth; z++)
        {
            Vector3 pos = origin + new Vector3(width * tileSize, 0, z * tileSize);
            Instantiate(wallPrefab, pos, Quaternion.Euler(0, 90, 0), transform);
        }
    }
}