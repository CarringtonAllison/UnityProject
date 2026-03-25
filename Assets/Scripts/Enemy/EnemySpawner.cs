using UnityEngine;

/// <summary>
/// Keeps exactly one enemy alive at a time.
/// When an enemy dies it waits spawnDelay seconds then spawns a new one at a random position.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnDelay = 1.5f;

    private void Start() => SpawnEnemy();

    public void OnEnemyDied() => Invoke(nameof(SpawnEnemy), spawnDelay);

    private void SpawnEnemy()
    {
        float x = Random.Range(-8f, 8f);
        float z = Random.Range(-8f, 8f);

        var go = new GameObject("Enemy");
        go.transform.position = new Vector3(x, 0f, z);
        go.AddComponent<Enemy>().Init(this);
    }
}
