using UnityEngine;

/// <summary>
/// Enemy that patrols left/right and dies when the player jumps on its head.
/// On death all body parts scatter with physics, then the spawner queues the next enemy.
/// </summary>
public class Enemy : MonoBehaviour
{
    // ── Palette — inverted Megaman colours ────────────────────────────────────
    private static readonly Color ColRed  = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color ColDark = new Color(0.10f, 0.10f, 0.12f);
    private static readonly Color ColEye  = new Color(0.90f, 0.55f, 0.10f);

    [Header("Patrol")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float moveSpeed      = 2.5f;

    private EnemySpawner _spawner;
    private Vector3      _startPos;
    private float        _patrolDir = 1f;
    private bool         _dead;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Init(EnemySpawner spawner) => _spawner = spawner;

    private void Start()
    {
        _startPos = transform.position;
        BuildBody();
    }

    // ── Patrol ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_dead) return;

        transform.Translate(Vector3.right * _patrolDir * moveSpeed * Time.deltaTime, Space.World);

        if (Mathf.Abs(transform.position.x - _startPos.x) >= patrolDistance)
        {
            _patrolDir *= -1f;
            transform.rotation = Quaternion.Euler(0f, _patrolDir > 0f ? 0f : 180f, 0f);
        }
    }

    // ── Stomp ─────────────────────────────────────────────────────────────────

    public void OnStomp(PlayerController player)
    {
        if (_dead) return;
        _dead = true;

        player.Bounce(3f);
        BreakApart();
        _spawner?.OnEnemyDied();
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    private void BreakApart()
    {
        // Collect direct children before detaching
        var children = new Transform[transform.childCount];
        for (int i = 0; i < children.Length; i++)
            children[i] = transform.GetChild(i);

        foreach (var child in children)
        {
            child.SetParent(null);

            // Every piece needs a collider for Rigidbody to work properly
            if (child.GetComponent<Collider>() == null)
                child.gameObject.AddComponent<BoxCollider>();

            var rb = child.gameObject.AddComponent<Rigidbody>();
            rb.linearVelocity  = new Vector3(Random.Range(-5f, 5f), Random.Range(4f, 9f), Random.Range(-5f, 5f));
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            Destroy(child.gameObject, 2.5f);
        }

        Destroy(gameObject);
    }

    // ── Body construction ────────────────────────────────────────────────────

    private void BuildBody()
    {
        // Body — keep CapsuleCollider so the player's CharacterController detects it
        MakePart(PrimitiveType.Capsule, "Body", transform,
                 new Vector3(0f, 0.9f, 0f), Vector3.zero,
                 new Vector3(0.7f, 1.0f, 0.7f), ColRed, keepCollider: true);

        // Head — keep SphereCollider; player lands here triggering the stomp
        var head = MakePart(PrimitiveType.Sphere, "Head", transform,
                            new Vector3(0f, 1.85f, 0f), Vector3.zero,
                            Vector3.one * 0.55f, ColRed, keepCollider: true);

        // Visor (child of Head) — decorative only
        MakePart(PrimitiveType.Cube, "Visor", head.transform,
                 new Vector3(0f, 0.05f, 0.28f), Vector3.zero,
                 new Vector3(0.45f, 0.20f, 0.10f), ColEye, keepCollider: false);

        // Arms — decorative
        MakePart(PrimitiveType.Capsule, "LeftArm", transform,
                 new Vector3(-0.5f, 1.3f, 0f), new Vector3(0f, 0f, 15f),
                 new Vector3(0.22f, 0.5f, 0.22f), ColRed, keepCollider: false);
        MakePart(PrimitiveType.Capsule, "RightArm", transform,
                 new Vector3(0.5f, 1.3f, 0f), new Vector3(0f, 0f, -15f),
                 new Vector3(0.22f, 0.5f, 0.22f), ColRed, keepCollider: false);

        // Feet
        MakePart(PrimitiveType.Cube, "LeftFoot", transform,
                 new Vector3(-0.18f, 0.1f, 0f), Vector3.zero,
                 new Vector3(0.35f, 0.20f, 0.50f), ColDark, keepCollider: false);
        MakePart(PrimitiveType.Cube, "RightFoot", transform,
                 new Vector3(0.18f, 0.1f, 0f), Vector3.zero,
                 new Vector3(0.35f, 0.20f, 0.50f), ColDark, keepCollider: false);
    }

    private static GameObject MakePart(PrimitiveType type, string name, Transform parent,
                                        Vector3 localPos, Vector3 localEuler, Vector3 localScale,
                                        Color color, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale       = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = PlaygroundGenerator.MakeMat(color);

        if (!keepCollider)
            Object.Destroy(go.GetComponent<Collider>());

        return go;
    }
}
