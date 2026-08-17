namespace JazzCreate.MultiPurposeUIPixel
{
    using UnityEngine;

public class SpawnEnemies : MonoBehaviour
{
    [Header("Spawn Timing")]
    [Tooltip("How long to wait before the first spawn happens.")]
    [SerializeField] private float initialDelay = 3f;

    [Tooltip("Time between spawns.")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("Spawn Area (World Units)")]
    [SerializeField] private float minX = -6f;
    [SerializeField] private float maxX = 6f;
    [SerializeField] private float minY = 4f;
    [SerializeField] private float maxY = 5f;

    private void OnEnable()
    {
        // Spawn repeatedly using your original style
        InvokeRepeating(nameof(LaunchEnemy), initialDelay, spawnInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(LaunchEnemy));
    }

    private void LaunchEnemy()
    {
        if (EnemyPoolScript.Instance == null)
        {
            Debug.LogError("SpawnEnemies: EnemyPoolScript.Instance is NULL. Add EnemyPoolScript to the scene.", this);
            return;
        }

        GameObject obj = EnemyPoolScript.Instance.GetEnemyObject();
        if (obj == null) return; // pool empty + willGrow false

        Vector2 pos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

        obj.transform.position = pos;
        obj.transform.rotation = transform.rotation; // keep your original rotation behaviour
        obj.SetActive(true);
    }
}
}
