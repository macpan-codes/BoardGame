using UnityEngine;

public class CommunityChestBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (FindFirstObjectByType<CommunityChestCardManager>(
                FindObjectsInactive.Include) == null)
        {
            gameObject.AddComponent<CommunityChestCardManager>();
        }
    }
}
