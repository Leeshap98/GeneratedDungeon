using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    public GameObject floorPrefab;     // e.g., Tile_A
    public GameObject wallPrefab;      // e.g., Wall_A
    public GameObject doorWallPrefab;  // e.g., Wall_B

    public Vector2Int roomSize = new Vector2Int(3, 3); // Width x Depth in tiles
    public float tileSize = 4f; // Size of one tile (assumes square tiles)

    void Start()
    {
        BuildRoom(Vector3.zero);
    }

    void BuildRoom(Vector3 position)
    {
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int z = 0; z < roomSize.y; z++)
            {
                // Place floor
                Vector3 floorPos = position + new Vector3(x * tileSize, 0, z * tileSize);
                Instantiate(floorPrefab, floorPos, Quaternion.identity, transform);

                // Wall logic (only place walls around edges)
                bool isEdgeX = (x == 0 || x == roomSize.x - 1);
                bool isEdgeZ = (z == 0 || z == roomSize.y - 1);

                if (isEdgeX || isEdgeZ)
                {
                    // Left wall
                    if (x == 0)
                        PlaceWall(floorPos + new Vector3(-tileSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0));
                    // Right wall
                    if (x == roomSize.x - 1)
                        PlaceWall(floorPos + new Vector3(tileSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0));
                    // Back wall
                    if (z == 0)
                        PlaceWall(floorPos + new Vector3(0, 0, -tileSize / 2f), Quaternion.identity);
                    // Front wall
                    if (z == roomSize.y - 1)
                        PlaceWall(floorPos + new Vector3(0, 0, tileSize / 2f), Quaternion.identity);
                }
            }
        }
    }

    void PlaceWall(Vector3 position, Quaternion rotation)
    {
        Instantiate(wallPrefab, position, rotation, transform);
    }
}
