#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

/// <summary>
/// Editor-only utility that constructs the entire playground scene
/// from scratch via a single menu click.
///
/// Menu: Tools > Playground > Build Entire Scene
///
/// This file MUST remain inside a folder named "Editor/" so Unity
/// excludes it from runtime builds automatically.
/// </summary>
public static class SceneBuilder
{
    // ── Megaman palette ───────────────────────────────────────────────────────
    private static readonly Color ColBlue   = new Color(0.15f, 0.45f, 0.85f);
    private static readonly Color ColWhite  = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color ColVisor  = new Color(0.20f, 0.60f, 0.90f);
    private static readonly Color ColSkin   = new Color(0.98f, 0.82f, 0.70f);

    [MenuItem("Tools/Playground/Build Entire Scene")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Playground Scene",
                "This will delete all existing scene objects and rebuild the scene. Continue?",
                "Build", "Cancel"))
            return;

        // ── 0. Clear scene ────────────────────────────────────────────────────
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
            Object.DestroyImmediate(go);

        // ── 1. Lighting ───────────────────────────────────────────────────────
        SetupLighting();

        // ── 2. Playground ─────────────────────────────────────────────────────
        var playgroundRoot = new GameObject("Playground");
        var gen = playgroundRoot.AddComponent<PlaygroundGenerator>();
        gen.Generate();

        // ── 3. Character ──────────────────────────────────────────────────────
        GameObject player = BuildCharacter();

        // ── 4. Camera Rig ─────────────────────────────────────────────────────
        GameObject cameraPivot = BuildCameraRig(player.transform);

        // ── 5. Wire PlayerController ──────────────────────────────────────────
        var pc = player.GetComponent<PlayerController>();
        var soPc = new SerializedObject(pc);
        soPc.FindProperty("cameraTarget").objectReferenceValue = cameraPivot.transform;
        soPc.ApplyModifiedProperties();

        // ── 6. Wire PlayerAnimator ────────────────────────────────────────────
        WireAnimator(player);

        // ── 7. Mark dirty ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SceneBuilder] ✓ Scene built. Press Play to run.");
    }

    // ── Lighting ──────────────────────────────────────────────────────────────
    private static void SetupLighting()
    {
        // Directional sun light
        var sunGO = new GameObject("Sun");
        var sun   = sunGO.AddComponent<Light>();
        sun.type      = LightType.Directional;
        sun.color     = new Color(1.0f, 0.95f, 0.84f);
        sun.intensity = 1.2f;
        sun.shadows   = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ambient trilight — daytime sky tones
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.50f, 0.70f, 1.00f);
        RenderSettings.ambientEquatorColor = new Color(0.40f, 0.50f, 0.40f);
        RenderSettings.ambientGroundColor  = new Color(0.20f, 0.20f, 0.15f);

        // Default skybox already set in new URP scenes — no change needed
    }

    // ── Character ─────────────────────────────────────────────────────────────
    private static GameObject BuildCharacter()
    {
        // Root
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, 0f);

        // CharacterController
        var cc          = player.AddComponent<CharacterController>();
        cc.height       = 1.8f;
        cc.radius       = 0.35f;
        cc.center       = new Vector3(0f, 0.9f, 0f);
        cc.skinWidth    = 0.02f;
        cc.stepOffset   = 0.4f;
        cc.slopeLimit   = 45f;

        // Scripts
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerAnimator>();

        // ── Body (Capsule) ────────────────────────────────────────────────────
        var body = CreatePart(PrimitiveType.Capsule, "Body", player.transform,
                              new Vector3(0f, 0.9f, 0f), Vector3.zero,
                              new Vector3(0.7f, 1.0f, 0.7f), ColBlue);

        // ── Head (Sphere) ─────────────────────────────────────────────────────
        var head = CreatePart(PrimitiveType.Sphere, "Head", player.transform,
                              new Vector3(0f, 1.85f, 0f), Vector3.zero,
                              Vector3.one * 0.55f, ColBlue);

        // Visor (Cube, child of Head) — glassy
        var visor = CreatePart(PrimitiveType.Cube, "Visor", head.transform,
                               new Vector3(0f, 0.05f, 0.28f), Vector3.zero,
                               new Vector3(0.45f, 0.20f, 0.10f),
                               ColVisor, metallic: 0.3f, smoothness: 0.85f);
        // Remove visor collider so it doesn't interfere with CharacterController
        Object.DestroyImmediate(visor.GetComponent<BoxCollider>());

        // ── Arms (Capsule) ────────────────────────────────────────────────────
        var leftArm = CreatePart(PrimitiveType.Capsule, "LeftArm", player.transform,
                                 new Vector3(-0.5f, 1.3f, 0f), new Vector3(0f, 0f, 15f),
                                 new Vector3(0.22f, 0.5f, 0.22f), ColBlue);

        var rightArm = CreatePart(PrimitiveType.Capsule, "RightArm", player.transform,
                                  new Vector3(0.5f, 1.3f, 0f), new Vector3(0f, 0f, -15f),
                                  new Vector3(0.22f, 0.5f, 0.22f), ColBlue);

        // Remove arm capsule colliders — CC on root handles collision
        Object.DestroyImmediate(leftArm.GetComponent<CapsuleCollider>());
        Object.DestroyImmediate(rightArm.GetComponent<CapsuleCollider>());

        // ── Feet (Cube) ───────────────────────────────────────────────────────
        var leftFoot = CreatePart(PrimitiveType.Cube, "LeftFoot", player.transform,
                                  new Vector3(-0.18f, 0.1f, 0f), Vector3.zero,
                                  new Vector3(0.35f, 0.20f, 0.50f), ColWhite);

        var rightFoot = CreatePart(PrimitiveType.Cube, "RightFoot", player.transform,
                                   new Vector3(0.18f, 0.1f, 0f), Vector3.zero,
                                   new Vector3(0.35f, 0.20f, 0.50f), ColWhite);

        // Remove foot colliders
        Object.DestroyImmediate(leftFoot.GetComponent<BoxCollider>());
        Object.DestroyImmediate(rightFoot.GetComponent<BoxCollider>());

        // Remove body capsule collider (CC handles it)
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        return player;
    }

    // ── Camera Rig ────────────────────────────────────────────────────────────
    private static GameObject BuildCameraRig(Transform playerTransform)
    {
        // Pivot (empty, holds CameraController)
        var pivot = new GameObject("CameraPivot");
        var camCtrl = pivot.AddComponent<CameraController>();

        // Wire target via SerializedObject
        var soCam = new SerializedObject(camCtrl);
        soCam.FindProperty("target").objectReferenceValue = playerTransform;
        soCam.ApplyModifiedProperties();

        // Camera child
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.parent        = pivot.transform;
        camGO.transform.localPosition = new Vector3(0f, 0f, -6f);
        camGO.transform.localRotation = Quaternion.identity;

        var cam         = camGO.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane  = 300f;

        camGO.AddComponent<AudioListener>();

        return pivot;
    }

    // ── Wire PlayerAnimator references ────────────────────────────────────────
    private static void WireAnimator(GameObject player)
    {
        var anim = player.GetComponent<PlayerAnimator>();
        if (anim == null) return;

        var so = new SerializedObject(anim);

        so.FindProperty("controller").objectReferenceValue = player.GetComponent<PlayerController>();
        so.FindProperty("leftArm").objectReferenceValue    = FindChild(player, "LeftArm");
        so.FindProperty("rightArm").objectReferenceValue   = FindChild(player, "RightArm");
        so.FindProperty("leftFoot").objectReferenceValue   = FindChild(player, "LeftFoot");
        so.FindProperty("rightFoot").objectReferenceValue  = FindChild(player, "RightFoot");
        so.FindProperty("head").objectReferenceValue       = FindChild(player, "Head");

        so.ApplyModifiedProperties();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreatePart(PrimitiveType type, string name, Transform parent,
                                         Vector3 localPos, Vector3 localEuler, Vector3 localScale,
                                         Color color, float metallic = 0f, float smoothness = 0.45f)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.parent        = parent;
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale    = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial =
            PlaygroundGenerator.MakeMat(color, metallic, smoothness);
        return go;
    }

    private static Transform FindChild(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        if (t == null) Debug.LogWarning($"[SceneBuilder] Child '{childName}' not found on {root.name}");
        return t;
    }
}
#endif
