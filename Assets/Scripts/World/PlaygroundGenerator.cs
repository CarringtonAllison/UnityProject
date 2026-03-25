using UnityEngine;

/// <summary>
/// Generates the entire playground from Unity primitives at runtime.
/// Also called by SceneBuilder in the Editor.
/// All materials use URP/Lit shader.
/// </summary>
public class PlaygroundGenerator : MonoBehaviour
{
    [Header("Ground Size")]
    [SerializeField] private float groundWidth  = 60f;
    [SerializeField] private float groundLength = 60f;

    // ── Palette ────────────────────────────────────────────────────────────
    private static readonly Color ColGrass    = new Color(0.55f, 0.76f, 0.40f);
    private static readonly Color ColSand     = new Color(0.93f, 0.84f, 0.63f);
    private static readonly Color ColMetal    = new Color(0.65f, 0.65f, 0.70f);
    private static readonly Color ColWood     = new Color(0.60f, 0.40f, 0.20f);
    private static readonly Color ColPlatform = new Color(0.85f, 0.50f, 0.20f);
    private static readonly Color ColChain    = new Color(0.55f, 0.55f, 0.60f);

    private void Awake() => Generate();

    // ── Public entry point ──────────────────────────────────────────────────
    public void Generate()
    {
        // Remove previously generated children (allows re-running in Editor)
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        CreateGround();
        CreateSlide(new Vector3(-12f, 0f, 8f));
        CreateSwingSet(new Vector3(10f, 0f, 10f));
        CreateSandbox(new Vector3(-10f, 0f, -10f));
        CreatePlatforms();
    }

    // ── Ground ──────────────────────────────────────────────────────────────
    private void CreateGround()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Ground";
        g.transform.parent        = transform;
        g.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        g.transform.localScale    = new Vector3(groundWidth, 0.5f, groundLength);
        g.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(ColGrass);
    }

    // ── Slide ────────────────────────────────────────────────────────────────
    private void CreateSlide(Vector3 pos)
    {
        var root = EmptyChild("Slide", pos);

        // Ladder posts
        Prim(PrimitiveType.Cube,  "Ladder_L",   root, new Vector3(-0.55f, 1.25f, -2.2f), Vector3.zero, new Vector3(0.12f, 2.5f, 0.12f), ColMetal);
        Prim(PrimitiveType.Cube,  "Ladder_R",   root, new Vector3( 0.55f, 1.25f, -2.2f), Vector3.zero, new Vector3(0.12f, 2.5f, 0.12f), ColMetal);

        // Ladder rungs
        int rungs = 7;
        for (int i = 0; i < rungs; i++)
        {
            float y = 0.35f + i * 0.35f;
            Prim(PrimitiveType.Cube, $"Rung_{i}", root,
                 new Vector3(0f, y, -2.2f), Vector3.zero,
                 new Vector3(0.9f, 0.07f, 0.07f), ColMetal);
        }

        // Platform at top of ladder
        Prim(PrimitiveType.Cube, "Platform", root,
             new Vector3(0f, 2.6f, -2.2f), Vector3.zero,
             new Vector3(1.4f, 0.1f, 1.0f), ColWood);

        // Ramp (tilted -35° on X so bottom end is near ground)
        // Ramp bottom sits at z≈0, top connects to platform z≈-2.2
        Prim(PrimitiveType.Cube, "Ramp", root,
             new Vector3(0f, 1.35f, -0.7f), new Vector3(-35f, 0f, 0f),
             new Vector3(1.4f, 0.12f, 5.0f), ColWood);

        // Side rails on ramp
        Prim(PrimitiveType.Cube, "Rail_L", root,
             new Vector3(-0.65f, 1.7f, -0.7f), new Vector3(-35f, 0f, 0f),
             new Vector3(0.08f, 0.35f, 5.0f), ColMetal);
        Prim(PrimitiveType.Cube, "Rail_R", root,
             new Vector3( 0.65f, 1.7f, -0.7f), new Vector3(-35f, 0f, 0f),
             new Vector3(0.08f, 0.35f, 5.0f), ColMetal);
    }

    // ── Swing Set ────────────────────────────────────────────────────────────
    private void CreateSwingSet(Vector3 pos)
    {
        var root = EmptyChild("SwingSet", pos);

        float poleH     = 3.2f;
        float halfSpan  = 2.2f;
        float barY      = poleH + 0.1f;

        // Vertical posts
        Prim(PrimitiveType.Cube, "Post_L", root, new Vector3(-halfSpan, poleH * 0.5f, 0f), Vector3.zero, new Vector3(0.15f, poleH, 0.15f), ColMetal);
        Prim(PrimitiveType.Cube, "Post_R", root, new Vector3( halfSpan, poleH * 0.5f, 0f), Vector3.zero, new Vector3(0.15f, poleH, 0.15f), ColMetal);

        // Diagonal braces
        Prim(PrimitiveType.Cube, "Brace_L", root, new Vector3(-halfSpan, poleH * 0.5f, -0.7f), new Vector3(15f, 0f, 0f), new Vector3(0.12f, poleH + 0.3f, 0.12f), ColMetal);
        Prim(PrimitiveType.Cube, "Brace_R", root, new Vector3( halfSpan, poleH * 0.5f, -0.7f), new Vector3(15f, 0f, 0f), new Vector3(0.12f, poleH + 0.3f, 0.12f), ColMetal);

        // Top bar
        Prim(PrimitiveType.Cube, "TopBar", root, new Vector3(0f, barY, 0f), Vector3.zero, new Vector3(halfSpan * 2f + 0.15f, 0.15f, 0.15f), ColMetal);

        // Two swings
        CreateSwing(root, new Vector3(-1.0f, barY, 0f));
        CreateSwing(root, new Vector3( 1.0f, barY, 0f));
    }

    private void CreateSwing(Transform parent, Vector3 attachPos)
    {
        var swingRoot = EmptyChild("Swing", attachPos, parent);

        // Chain links (visual only — colliders stripped)
        int   links      = 8;
        float linkSpacing = 0.28f;
        for (int i = 0; i < links; i++)
        {
            var link = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            link.name = $"Link_{i}";
            link.transform.parent        = swingRoot;
            link.transform.localPosition = new Vector3(0f, -i * linkSpacing, 0f);
            link.transform.localScale    = Vector3.one * 0.07f;
            link.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(ColChain);
            Object.DestroyImmediate(link.GetComponent<SphereCollider>());
        }

        // Seat
        float seatY = -(links * linkSpacing) - 0.1f;
        Prim(PrimitiveType.Cube, "Seat", swingRoot, new Vector3(0f, seatY, 0f), Vector3.zero, new Vector3(0.6f, 0.07f, 0.3f), ColWood, parent2: swingRoot);
    }

    // ── Sandbox ──────────────────────────────────────────────────────────────
    private void CreateSandbox(Vector3 pos)
    {
        var root = EmptyChild("Sandbox", pos);

        float hw = 3.0f; // half width
        float bh = 0.35f;
        float bt = 0.2f;

        // Four border planks
        Prim(PrimitiveType.Cube, "Border_Front", root, new Vector3(0f,        bh * 0.5f,  hw + bt * 0.5f), Vector3.zero, new Vector3(hw * 2f + bt * 2f, bh, bt), ColWood);
        Prim(PrimitiveType.Cube, "Border_Back",  root, new Vector3(0f,        bh * 0.5f, -hw - bt * 0.5f), Vector3.zero, new Vector3(hw * 2f + bt * 2f, bh, bt), ColWood);
        Prim(PrimitiveType.Cube, "Border_Left",  root, new Vector3(-hw - bt * 0.5f, bh * 0.5f, 0f), Vector3.zero, new Vector3(bt, bh, hw * 2f), ColWood);
        Prim(PrimitiveType.Cube, "Border_Right", root, new Vector3( hw + bt * 0.5f, bh * 0.5f, 0f), Vector3.zero, new Vector3(bt, bh, hw * 2f), ColWood);

        // Sand fill
        Prim(PrimitiveType.Cube, "Sand", root, new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(hw * 2f, 0.2f, hw * 2f), ColSand);
    }

    // ── Jump Platforms ────────────────────────────────────────────────────────
    private void CreatePlatforms()
    {
        // Platform A — reachable from ground with a single jump
        var pA = Prim(PrimitiveType.Cube, "Platform_A", transform,
                      new Vector3(6f, 1.5f, -8f), Vector3.zero,
                      new Vector3(4f, 0.5f, 4f), ColPlatform);
        // Support post
        Prim(PrimitiveType.Cube, "Post_A", transform,
             new Vector3(6f, 0.65f, -8f), Vector3.zero,
             new Vector3(0.3f, 1.3f, 0.3f), ColMetal);

        // Platform B — higher, requires jump from A
        Prim(PrimitiveType.Cube, "Platform_B", transform,
             new Vector3(10f, 3.0f, -8f), Vector3.zero,
             new Vector3(3f, 0.5f, 3f), ColPlatform);
        Prim(PrimitiveType.Cube, "Post_B", transform,
             new Vector3(10f, 1.4f, -8f), Vector3.zero,
             new Vector3(0.3f, 2.8f, 0.3f), ColMetal);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Transform EmptyChild(string name, Vector3 localPos, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.parent        = parent != null ? parent : transform;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    /// <summary>Creates a primitive, parents it, and returns its Transform.</summary>
    private Transform Prim(PrimitiveType type, string name, Transform parent,
                           Vector3 localPos, Vector3 localEuler, Vector3 localScale,
                           Color color, Transform parent2 = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.parent        = parent;
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale    = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(color);
        return go.transform;
    }

    // Overload for convenience (parent is this.transform)
    private Transform Prim(PrimitiveType type, string name, Transform parent,
                           Vector3 localPos, Vector3 localEuler, Vector3 localScale, Color color)
        => Prim(type, name, parent, localPos, localEuler, localScale, color, null);

    public static Material MakeMat(Color color, float metallic = 0f, float smoothness = 0.35f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("[PlaygroundGenerator] URP/Lit shader not found — falling back to Standard.");
            shader = Shader.Find("Standard");
        }
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }
}
