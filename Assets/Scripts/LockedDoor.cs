using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var inv = other.GetComponent<PlayerInventory>();
            if (inv != null && inv.hasKey)
            {
                Destroy(gameObject); // Door "opens"
            }
        }
    }
}
