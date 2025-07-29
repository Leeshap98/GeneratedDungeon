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
        if (depth >= maxSplitDepth || area.width < 10 || area.height < 10)
        {
            roomList.Add(area);
            return;
        }

        bool splitHorizontally = Random.value > 0.5f;
        if (area.width > area.height) splitHorizontally = false;
        if (area.height > area.width) splitHorizontally = true;

        if (splitHorizontally)
        {
            int split = Random.Range(10, area.height - 10);
            RectInt top = new RectInt(area.x, area.y + split, area.width, area.height - split);
            RectInt bottom = new RectInt(area.x, area.y, area.width, split);
            SplitAndGenerate(top, depth + 1, roomList);
            SplitAndGenerate(bottom, depth + 1, roomList);
        }
        else
        {
            int split = Random.Range(10, area.width - 10);
            RectInt left = new RectInt(area.x, area.y, split, area.height);
            RectInt right = new RectInt(area.x + split, area.y, area.width - split, area.height);
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
