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

    [Header("Corridors (Floor Connectors)")]
    public GameObject corridorPrefab;
    public float corridorWorldWidth = 2f; // visual corridor width

    [Header("Walls")]
    public GameObject wallPrefab;         // 1x1x1 cube-like, with BoxCollider
    public float wallHeight = 3f;
    public float wallThickness = 0.2f;
    public float doorwayWidth = 2.0f;

    [Header("Locked Door")]
    public GameObject lockedDoorPrefab;   // door model with collider + LockedDoor script
    public float doorThickness = 0.18f;
    public float doorHeight = 2.3f;

    [Header("Corridor Settings")]
    public int corridorWidth = 2;         // BSP split gutter (keep)

    [Header("Prefabs")]
    public GameObject roomPrefab;
    public GameObject stairPrefab;

    [Header("Player Spawn")]
    public Transform player;
    public float playerSpawnYOffset = 1.0f;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    // ------- Internal types / data -------
    private enum WallSide { North, South, East, West }

    private struct RoomRuntime
    {
        public RectInt rect;
        public Vector3 center;
        public Vector3 size;
        public GameObject go;
        public RoomType type;
    }

    private List<List<RectInt>> allFloorsRooms = new();

    // ================= LIFECYCLE =================
    void Start()
    {
        GenerateMultiFloorDungeon();
        ConnectFloorsWithStairs();

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
        else
            Debug.LogWarning("NavMeshSurface reference not assigned!");
    }

    // ================= GENERATION =================
    void GenerateMultiFloorDungeon()
    {
        var allFloorsRuntime = new List<List<RoomRuntime>>();

        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            // BSP rectangles for this floor
            List<RectInt> currentFloorRooms = new();
            RectInt rootBounds = new RectInt(0, 0, dungeonWidth, dungeonHeight);
            SplitAndGenerate(rootBounds, 0, currentFloorRooms);
            allFloorsRooms.Add(currentFloorRooms);

            var runtimeRooms = new List<RoomRuntime>();

            // Spawn rooms (temp type Normal; we’ll assign after MST)
            for (int i = 0; i < currentFloorRooms.Count; i++)
            {
                RectInt room = currentFloorRooms[i];
                Vector3 roomPos = new Vector3(
                    room.x + room.width / 2f,
                    floor * floorHeightSpacing,
                    room.y + room.height / 2f
                );
                Vector3 roomSize = new Vector3(room.width, 1, room.height);

                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);
                roomObj.transform.localScale = roomSize;
                roomObj.name = $"Floor {floor} Room {i}";

                var rd = roomObj.GetComponent<RoomData>() ?? roomObj.AddComponent<RoomData>();
                rd.Init(RoomType.Normal, roomPos, roomSize);

                runtimeRooms.Add(new RoomRuntime
                {
                    rect = room,
                    center = roomPos,
                    size = roomSize,
                    go = roomObj,
                    type = RoomType.Normal
                });
            }

            // Build MST + record door openings + parents
            var openings = new Dictionary<int, HashSet<WallSide>>();
            ConnectRoomsOnFloor(runtimeRooms, openings, out int[] parent, out WallSide[] sideToParent);

            // Choose Locked = farthest from Start (index 0) by depth; Key = first step on path to Locked
            int lockedIndex = 0, maxDepth = -1;
            for (int i = 0; i < runtimeRooms.Count; i++)
            {
                int depth = 0, p = parent[i];
                while (p != -1) { depth++; p = parent[p]; }
                if (depth > maxDepth) { maxDepth = depth; lockedIndex = i; }
            }
            var pathToLocked = GetPathFromStart(lockedIndex, parent); // 0..locked
            int keyIndex = (pathToLocked.Count >= 2) ? pathToLocked[1] : lockedIndex;

            // Assign types (Start only for floor 0 / room 0)
            for (int i = 0; i < runtimeRooms.Count; i++)
            {
                RoomType t = RoomType.Normal;
                if (floor == 0 && i == 0) t = RoomType.Start;
                if (i == keyIndex) t = RoomType.Key;
                if (i == lockedIndex) t = RoomType.Locked;

                var tmp = runtimeRooms[i];
                tmp.type = t;
                runtimeRooms[i] = tmp;
                var rd = runtimeRooms[i].go.GetComponent<RoomData>();
                rd.roomType = t;
            }

            // Decorate (props). Avoid double door: door is placed in the wall opening by us.
            var template = GetComponent<RoomDecorator>();
            if (template != null)
            {
                for (int i = 0; i < runtimeRooms.Count; i++)
                {
                    var dec = runtimeRooms[i].go.GetComponent<RoomDecorator>() ?? runtimeRooms[i].go.AddComponent<RoomDecorator>();
                    dec.keyPrefab = template.keyPrefab;
                    dec.lockedDoorPrefab = null; // door handled by PlaceLockedDoor
                    dec.startPropPrefab = template.startPropPrefab;
                    dec.normalPropPrefab = template.normalPropPrefab;

                    dec.Decorate(runtimeRooms[i].go.GetComponent<RoomData>());
                }
            }

            // Build walls with doorway gaps
            BuildWallsForFloor(runtimeRooms, openings);

            // Place the locked door on the side facing its parent (entry direction)
            if (runtimeRooms.Count > 0 && lockedDoorPrefab != null && lockedIndex >= 0 && lockedIndex < runtimeRooms.Count)
            {
                WallSide doorSide = (parent[lockedIndex] == -1) ? WallSide.South : sideToParent[lockedIndex];
                PlaceLockedDoor(runtimeRooms[lockedIndex].center, runtimeRooms[lockedIndex].size, doorSide);
            }

            allFloorsRuntime.Add(runtimeRooms);
        }

        // Spawn player in Start room (floor 0, room 0)
        if (player != null && allFloorsRuntime.Count > 0 && allFloorsRuntime[0].Count > 0)
        {
            var start = allFloorsRuntime[0][0];
            player.position = start.center + Vector3.up * playerSpawnYOffset;
        }
    }

    // ================= BSP SPLITTER (unchanged) =================
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

    // ================= STAIRS (visual only) =================
    void ConnectFloorsWithStairs()
    {
        for (int floor = 0; floor < numberOfFloors - 1; floor++)
        {
            List<RectInt> current = allFloorsRooms[floor];
            List<RectInt> above = allFloorsRooms[floor + 1];
            if (current.Count == 0 || above.Count == 0) continue;

            RectInt fromRoom = current[Random.Range(0, current.Count)];
            Vector3 stairPos = new Vector3(
                fromRoom.x + fromRoom.width / 2f,
                floor * floorHeightSpacing + 1.5f,
                fromRoom.y + fromRoom.height / 2f
            );

            if (stairPrefab != null)
                Instantiate(stairPrefab, stairPos, Quaternion.identity, transform);
        }
    }

    // ================= CORRIDORS & OPENINGS =================
    void AddOpening(Dictionary<int, HashSet<WallSide>> openings, int roomIdx, WallSide side)
    {
        if (!openings.TryGetValue(roomIdx, out var set))
        {
            set = new HashSet<WallSide>();
            openings[roomIdx] = set;
        }
        set.Add(side);
    }

    // MST + doorway sides + parent maps; also spawns corridors aligned to doorway midpoints
    void ConnectRoomsOnFloor(
        List<RoomRuntime> roomsOnFloor,
        Dictionary<int, HashSet<WallSide>> openings,
        out int[] parent,
        out WallSide[] sideToParent)
    {
        parent = new int[roomsOnFloor.Count];
        sideToParent = new WallSide[roomsOnFloor.Count];
        for (int i = 0; i < parent.Length; i++) parent[i] = -1; // root (room 0) has no parent

        if (roomsOnFloor == null || roomsOnFloor.Count <= 1 || corridorPrefab == null) return;

        float corridorW = Mathf.Min(corridorWorldWidth, doorwayWidth * 0.95f);

        var connected = new List<int>() { 0 };
        var remaining = new HashSet<int>();
        for (int i = 1; i < roomsOnFloor.Count; i++) remaining.Add(i);

        while (remaining.Count > 0)
        {
            float bestDist = float.MaxValue;
            int bestA = -1, bestB = -1;

            foreach (var a in connected)
            {
                foreach (var b in remaining)
                {
                    float d = Vector2.Distance(
                        new Vector2(roomsOnFloor[a].center.x, roomsOnFloor[a].center.z),
                        new Vector2(roomsOnFloor[b].center.x, roomsOnFloor[b].center.z)
                    );
                    if (d < bestDist) { bestDist = d; bestA = a; bestB = b; }
                }
            }
            if (bestA == -1) break;

            Vector3 ca = roomsOnFloor[bestA].center;
            Vector3 cb = roomsOnFloor[bestB].center;
            Vector3 dlt = cb - ca;

            WallSide sideA, sideB;
            if (Mathf.Abs(dlt.x) >= Mathf.Abs(dlt.z))
            {
                sideA = (dlt.x >= 0) ? WallSide.East : WallSide.West;
                sideB = (dlt.x >= 0) ? WallSide.West : WallSide.East;
            }
            else
            {
                sideA = (dlt.z >= 0) ? WallSide.North : WallSide.South;
                sideB = (dlt.z >= 0) ? WallSide.South : WallSide.North;
            }

            AddOpening(openings, bestA, sideA);
            AddOpening(openings, bestB, sideB);

            parent[bestB] = bestA;          // MST parent
            sideToParent[bestB] = sideB;    // on B, which side faces A

            // Corridors between doorway centers
            Vector3 aWall = GetWallMidpoint(roomsOnFloor[bestA].center, roomsOnFloor[bestA].size, sideA);
            Vector3 bWall = GetWallMidpoint(roomsOnFloor[bestB].center, roomsOnFloor[bestB].size, sideB);
            SpawnLCorridor(aWall, bWall, corridorW);

            connected.Add(bestB);
            remaining.Remove(bestB);
        }
    }

    Vector3 GetWallMidpoint(Vector3 c, Vector3 s, WallSide side)
    {
        switch (side)
        {
            case WallSide.North: return new Vector3(c.x, c.y, c.z + s.z * 0.5f);
            case WallSide.South: return new Vector3(c.x, c.y, c.z - s.z * 0.5f);
            case WallSide.East:  return new Vector3(c.x + s.x * 0.5f, c.y, c.z);
            case WallSide.West:  return new Vector3(c.x - s.x * 0.5f, c.y, c.z);
            default: return c;
        }
    }

    List<int> GetPathFromStart(int targetIndex, int[] parent)
    {
        var path = new List<int>();
        int cur = targetIndex;
        while (cur != -1)
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Reverse(); // 0..target
        return path;
    }

    void SpawnLCorridor(Vector3 a, Vector3 b, float width)
    {
        Vector3 mid = new Vector3(b.x, a.y, a.z);
        SpawnRectCorridor(a, mid, width);
        SpawnRectCorridor(mid, b, width);
    }

    void SpawnRectCorridor(Vector3 p1, Vector3 p2, float width)
    {
        if (corridorPrefab == null) return;

        Vector3 delta = p2 - p1;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
        {
            float length = Mathf.Abs(delta.x);
            if (length < 0.01f) return;

            Vector3 center = new Vector3((p1.x + p2.x) * 0.5f, p1.y, p1.z);
            var go = Instantiate(corridorPrefab, center, Quaternion.identity, transform);
            go.name = "Corridor_X";
            go.transform.localScale = new Vector3(length, go.transform.localScale.y, width);
        }
        else
        {
            float length = Mathf.Abs(delta.z);
            if (length < 0.01f) return;

            Vector3 center = new Vector3(p1.x, p1.y, (p1.z + p2.z) * 0.5f);
            var go = Instantiate(corridorPrefab, center, Quaternion.identity, transform);
            go.name = "Corridor_Z";
            go.transform.localScale = new Vector3(width, go.transform.localScale.y, length);
        }
    }

    // ================= WALLS & DOOR =================
    void BuildWallsForFloor(List<RoomRuntime> roomsOnFloor, Dictionary<int, HashSet<WallSide>> openings)
    {
        for (int i = 0; i < roomsOnFloor.Count; i++)
        {
            var rr = roomsOnFloor[i];
            openings.TryGetValue(i, out var gaps);

            BuildWallWithGap(rr.center, rr.size, WallSide.North, gaps?.Contains(WallSide.North) == true);
            BuildWallWithGap(rr.center, rr.size, WallSide.South, gaps?.Contains(WallSide.South) == true);
            BuildWallWithGap(rr.center, rr.size, WallSide.East,  gaps?.Contains(WallSide.East)  == true);
            BuildWallWithGap(rr.center, rr.size, WallSide.West,  gaps?.Contains(WallSide.West)  == true);
        }
    }

    void BuildWallWithGap(Vector3 c, Vector3 s, WallSide side, bool hasGap)
    {
        if (wallPrefab == null) return;

        if (side == WallSide.North || side == WallSide.South)
        {
            float z = c.z + (side == WallSide.North ? +s.z / 2f : -s.z / 2f);
            float y = c.y + wallHeight / 2f;
            float totalLen = s.x;

            if (!hasGap || doorwayWidth >= totalLen)
            {
                BuildWallSegment(new Vector3(c.x, y, z), new Vector3(totalLen, wallHeight, wallThickness));
            }
            else
            {
                float halfRemain = (totalLen - doorwayWidth) * 0.5f;
                // left
                BuildWallSegment(new Vector3(c.x - (doorwayWidth / 2f + halfRemain / 2f), y, z),
                                 new Vector3(halfRemain, wallHeight, wallThickness));
                // right
                BuildWallSegment(new Vector3(c.x + (doorwayWidth / 2f + halfRemain / 2f), y, z),
                                 new Vector3(halfRemain, wallHeight, wallThickness));
            }
        }
        else // East/West
        {
            float x = c.x + (side == WallSide.East ? +s.x / 2f : -s.x / 2f);
            float y = c.y + wallHeight / 2f;
            float totalLen = s.z;

            if (!hasGap || doorwayWidth >= totalLen)
            {
                BuildWallSegment(new Vector3(x, y, c.z), new Vector3(wallThickness, wallHeight, totalLen));
            }
            else
            {
                float halfRemain = (totalLen - doorwayWidth) * 0.5f;
                // bottom
                BuildWallSegment(new Vector3(x, y, c.z - (doorwayWidth / 2f + halfRemain / 2f)),
                                 new Vector3(wallThickness, wallHeight, halfRemain));
                // top
                BuildWallSegment(new Vector3(x, y, c.z + (doorwayWidth / 2f + halfRemain / 2f)),
                                 new Vector3(wallThickness, wallHeight, halfRemain));
            }
        }
    }

    void BuildWallSegment(Vector3 pos, Vector3 scale)
    {
        var w = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        w.transform.localScale = scale;
    }

    void PlaceLockedDoor(Vector3 c, Vector3 s, WallSide side)
    {
        if (lockedDoorPrefab == null) return;

        Vector3 pos = c;
        Vector3 scl = new Vector3(doorwayWidth, doorHeight, doorThickness);

        switch (side)
        {
            case WallSide.North: pos = new Vector3(c.x, c.y + doorHeight / 2f, c.z + s.z / 2f); break;
            case WallSide.South: pos = new Vector3(c.x, c.y + doorHeight / 2f, c.z - s.z / 2f); break;
            case WallSide.East:  pos = new Vector3(c.x + s.x / 2f, c.y + doorHeight / 2f, c.z); scl = new Vector3(doorThickness, doorHeight, doorwayWidth); break;
            case WallSide.West:  pos = new Vector3(c.x - s.x / 2f, c.y + doorHeight / 2f, c.z); scl = new Vector3(doorThickness, doorHeight, doorwayWidth); break;
        }

        var door = Instantiate(lockedDoorPrefab, pos, Quaternion.identity, transform);
        door.transform.localScale = scl;
    }
}
