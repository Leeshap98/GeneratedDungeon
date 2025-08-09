using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LockedDoor : MonoBehaviour
{
    [Header("Colliders")]
    public Collider blockingCollider;   // solid collider that blocks when locked
    public Collider sensorTrigger;      // trigger that detects the player

    [Header("Visuals")]
    public Renderer[] visuals;          // optional: assign renderers to hide on open
    public bool destroyOnOpen = false;  // if true, Destroy(gameObject) when unlocked

    bool isOpen = false;

    void Awake()
    {
        // Fallbacks if not wired in Inspector
        if (blockingCollider == null) blockingCollider = GetComponent<Collider>();

        // If no separate trigger given, try to find another collider marked trigger
        if (sensorTrigger == null)
        {
            var cols = GetComponents<Collider>();
            foreach (var c in cols) if (c != blockingCollider && c.isTrigger) { sensorTrigger = c; break; }
        }

        // If still no trigger, we can temporarily create a trigger on this object
        if (sensorTrigger == null)
        {
            var trig = gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            // make it slightly thinner so it sits in the doorway
            (trig as BoxCollider).size = Vector3.one;
            sensorTrigger = trig;
        }

        // Ensure blocking collider is solid while locked
        if (blockingCollider != null) blockingCollider.isTrigger = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // Only the sensorTrigger should call this
        if (sensorTrigger != null && other == sensorTrigger) return; // safety (not needed in most setups)

        TryOpenFromCollider(other);
    }

    void OnTriggerStay(Collider other)
    {
        // In case the player was already inside the trigger before picking the key
        TryOpenFromCollider(other);
    }

    void TryOpenFromCollider(Collider other)
    {
        if (isOpen) return;
        if (!other.CompareTag("Player")) return;

        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null) return;

        if (inv.hasKey)
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        isOpen = true;

        // 1) make it non-blocking
        if (blockingCollider != null) blockingCollider.enabled = false;

        // 2) optional: hide meshes
        if (visuals != null && visuals.Length > 0)
        {
            foreach (var r in visuals) if (r != null) r.enabled = false;
        }

        // 3) optional: destroy
        if (destroyOnOpen) Destroy(gameObject);
    }
}
