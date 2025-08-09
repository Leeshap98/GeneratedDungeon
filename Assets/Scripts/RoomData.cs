using UnityEngine;

public class RoomData : MonoBehaviour
{
    public RoomType roomType;
    public Vector3 center;
    public Vector3 size;

    public void Init(RoomType type, Vector3 pos, Vector3 scale)
    {
        roomType = type;
        center = pos;
        size = scale;
    }

    void OnDrawGizmos()
    {
        Color c = Color.white;
        switch (roomType)
        {
            case RoomType.Start: c = Color.green; break;
            case RoomType.Key: c = Color.yellow; break;
            case RoomType.Locked: c = Color.red; break;
            case RoomType.Normal: c = Color.gray; break;
        }
        Gizmos.color = c;
        Gizmos.DrawWireCube(center, size);
    }
}