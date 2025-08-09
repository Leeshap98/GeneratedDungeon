using UnityEngine;

public class RoomDecorator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject keyPrefab;
    public GameObject lockedDoorPrefab; // we won't use this in our current flow; door is placed in wall gap
    public GameObject startPropPrefab;
    public GameObject normalPropPrefab;

    [Header("Placement")]
    public float spawnYOffset = 0.6f;         // small lift so item sits above floor mesh
    public bool parentToRoom = true;          // parent under the room object for clarity
    public bool useRaycastToFloor = true;     // drop item onto the floor top

    public LayerMask floorMask = ~0;          // optional: layer mask for raycast

    public void Decorate(RoomData room)
    {
        // choose prefab by type
        GameObject prefab = null;
        switch (room.roomType)
        {
            case RoomType.Start:  prefab = startPropPrefab; break;
            case RoomType.Key:    prefab = keyPrefab;       break;
            case RoomType.Locked: prefab = lockedDoorPrefab;/* intentionally null in current pipeline */ break;
            case RoomType.Normal: prefab = normalPropPrefab; break;
        }

        if (prefab == null) return; // nothing to place (Locked handled by wall-door)

        // compute spawn point
        Vector3 pos = room.center;

        if (useRaycastToFloor)
        {
            // cast from above the room downwards to find top of floor/corridor
            Vector3 origin = room.center + Vector3.up * 10f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, floorMask, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + Vector3.up * spawnYOffset;
            }
            else
            {
                pos = room.center + Vector3.up * spawnYOffset;
            }
        }
        else
        {
            pos = room.center + Vector3.up * spawnYOffset;
        }

        // choose parent (room GameObject so it stays tidy)
        Transform parent = parentToRoom ? room.transform : null;

        Instantiate(prefab, pos, Quaternion.identity, parent);
    }
}
