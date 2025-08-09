using UnityEngine;

public class KeyItem : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var inv = other.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.hasKey = true;
                Destroy(gameObject);
            }
        }
    }
}
