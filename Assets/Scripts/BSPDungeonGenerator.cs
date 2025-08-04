using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 50;
    public int dungeonHeight = 50;
    public int maxSplitDepth = 4;
    public int numberOfFloors = 3;
    public float floorHeightSpacing = 10f;

    [Header("Room Size Settings")]
    public int minRoomWidth = 6;
    public int maxRoomWidth = 15;
    public int minRoomHeight = 6;
    public int maxRoomHeight = 15;

    [Header("Corridor Settings")]
    public int corridorWidth = 2;

    [Header("Prefabs")]
    public GameObject roomPrefab;
    public GameObject stairPrefab;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    private List<List<RectInt>> allFloorsRooms = new();

    void Start()
    {
        GenerateMultiFloorDungeon();
        ConnectFloorsWithStairs();

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning("NavMeshSurface reference not assigned!");
        }
    }

    void GenerateMultiFloorDungeon()
    {
        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            List<RectInt> currentFloorRooms = new();
            RectInt rootBounds = new RectInt(0, 0, dungeonWidth, dungeonHeight);
            SplitAndGenerate(rootBounds, 0, currentFloorRooms);
            allFloorsRooms.Add(currentFloorRooms);

            foreach (RectInt room in currentFloorRooms)
            {
                Vector3 roomPos = new Vector3(room.x + room.width / 2f, floor * floorHeightSpacing, room.y + room.height / 2f);
                Vector3 roomSize = new Vector3(room.width, 1, room.height);

                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);
                roomObj.transform.localScale = roomSize;
                roomObj.name = $"Floor {floor} Room";
            }
        }
    }

    void SplitAndGenerate(RectInt area, int depth, List<RectInt> roomList)
    {
        if (depth >= maxSplitDepth || area.width < maxRoomWidth * 2 || area.height < maxRoomHeight * 2)
        {
            int roomWidth = Mathf.Clamp(Random.Range(minRoomWidth, maxRoomWidth + 1), 1, area.width);
            int roomHeight = Mathf.Clamp(Random.Range(minRoomHeight, maxRoomHeight + 1), 1, area.height);

            int roomX = area.x + Random.Range(0, area.width - roomWidth);
            int roomY = area.y + Random.Range(0, area.height - roomHeight);

            RectInt room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            roomList.Add(room);
            return;
        }

        bool splitHorizontally = Random.value > 0.5f;
        if (area.width > area.height) splitHorizontally = false;
        if (area.height > area.width) splitHorizontally = true;

        if (splitHorizontally)
        {
            int split = Random.Range(maxRoomHeight, area.height - maxRoomHeight);
            RectInt top = new RectInt(area.x, area.y + split + corridorWidth, area.width, area.height - split - corridorWidth);
            RectInt bottom = new RectInt(area.x, area.y, area.width, split);
            SplitAndGenerate(top, depth + 1, roomList);
            SplitAndGenerate(bottom, depth + 1, roomList);
        }
        else
        {
            int split = Random.Range(maxRoomWidth, area.width - maxRoomWidth);
            RectInt left = new RectInt(area.x, area.y, split, area.height);
            RectInt right = new RectInt(area.x + split + corridorWidth, area.y, area.width - split - corridorWidth, area.height);
            SplitAndGenerate(left, depth + 1, roomList);
            SplitAndGenerate(right, depth + 1, roomList);
        }
    }

    void ConnectFloorsWithStairs()
    {
        for (int floor = 0; floor < numberOfFloors - 1; floor++)
        {
            List<RectInt> current = allFloorsRooms[floor];
            List<RectInt> above = allFloorsRooms[floor + 1];

            if (current.Count == 0 || above.Count == 0) continue;

            RectInt fromRoom = current[Random.Range(0, current.Count)];
            RectInt toRoom = above[Random.Range(0, above.Count)];

            Vector3 stairPos = new Vector3(
                fromRoom.x + fromRoom.width / 2f,
                floor * floorHeightSpacing + 1.5f,
                fromRoom.y + fromRoom.height / 2f
            );

            Instantiate(stairPrefab, stairPos, Quaternion.identity, transform);
        }
    }
}
