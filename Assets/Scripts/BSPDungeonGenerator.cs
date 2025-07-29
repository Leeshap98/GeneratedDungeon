using System.Collections.Generic;
using UnityEngine;

public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 50;
    public int dungeonHeight = 50;
    public int maxSplitDepth = 4;

    [Header("Prefabs")]
    public GameObject roomPrefab;

    private List<RectInt> roomList = new();

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        RectInt rootBounds = new RectInt(0, 0, dungeonWidth, dungeonHeight);
        SplitAndGenerate(rootBounds, 0);

        foreach (RectInt room in roomList)
        {
            Vector3 roomPos = new Vector3(room.x + room.width / 2f, 0f, room.y + room.height / 2f);
            Vector3 roomSize = new Vector3(room.width, 1, room.height);

            GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);
            roomObj.transform.localScale = roomSize;
        }
    }

    void SplitAndGenerate(RectInt area, int depth)
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
            SplitAndGenerate(top, depth + 1);
            SplitAndGenerate(bottom, depth + 1);
        }
        else
        {
            int split = Random.Range(10, area.width - 10);
            RectInt left = new RectInt(area.x, area.y, split, area.height);
            RectInt right = new RectInt(area.x + split, area.y, area.width - split, area.height);
            SplitAndGenerate(left, depth + 1);
            SplitAndGenerate(right, depth + 1);
        }
    }
}
