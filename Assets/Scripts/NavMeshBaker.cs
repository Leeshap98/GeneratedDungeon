using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;

    void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    public void Bake()
    {
        if (surface != null)
        {
            surface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("NavMeshSurface not found on this GameObject!");
        }
    }
}
