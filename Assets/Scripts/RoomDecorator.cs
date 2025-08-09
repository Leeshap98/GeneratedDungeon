using UnityEngine;

public class RoomDecorator : MonoBehaviour
{
    public GameObject keyPrefab;
    public GameObject lockedDoorPrefab;
    public GameObject startPropPrefab;
    public GameObject normalPropPrefab;

    public void Decorate(RoomData room)
    {
        switch (room.roomType)
        {
            case RoomType.Start:
                if (startPropPrefab) Instantiate(startPropPrefab, room.center, Quaternion.identity);
                break;
            case RoomType.Key:
                if (keyPrefab) Instantiate(keyPrefab, room.center, Quaternion.identity);
                break;
            case RoomType.Locked:
                if (lockedDoorPrefab) Instantiate(lockedDoorPrefab, room.center, Quaternion.identity);
                break;
            case RoomType.Normal:
                if (normalPropPrefab) Instantiate(normalPropPrefab, room.center, Quaternion.identity);
                break;
        }
    }
}
