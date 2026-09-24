using UnityEditor;
using UnityEngine;

/// <summary>
/// Research spike for issue #8 — entry points for -executeMethod.
///
///   Unity -batchmode -nographics -projectPath . -executeMethod HeadlessSpikeEditor.Noop -logFile x.log
///   Unity -batchmode -nographics -projectPath . -executeMethod HeadlessSpikeEditor.EditModePhysics -logFile x.log
/// </summary>
public static class HeadlessSpikeEditor
{
    /// <summary>Does nothing: measures pure editor start + exit cost.</summary>
    public static void Noop()
    {
        Debug.Log("[SPIKE] Noop reached at realtimeSinceStartup=" + Time.realtimeSinceStartup.ToString("0.00"));
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// Without entering Play mode: can Physics2D be stepped by hand, and do
    /// MonoBehaviour callbacks (Awake, OnCollisionEnter2D) fire?
    /// </summary>
    public static void EditModePhysics()
    {
        Physics2D.simulationMode = SimulationMode2D.Script;

        var mover = new GameObject("Mover");
        var body = mover.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        mover.AddComponent<BoxCollider2D>();
        var probe = mover.AddComponent<SpikeProbe>();
        body.linearVelocity = new Vector2(5f, 0f);

        var wall = new GameObject("Wall");
        wall.transform.position = new Vector3(3f, 0f, 0f);
        wall.AddComponent<BoxCollider2D>();

        for (int i = 0; i < 100; i++)
        {
            Physics2D.Simulate(0.02f);
        }

        Debug.Log($"[SPIKE] edit-mode Simulate: moverX={body.position.x:0.00} awake={probe.Awoken} " +
                  $"collisionEnter={probe.CollisionEnter} stay={probe.CollisionStay} contacts={body.GetContacts(new ContactPoint2D[4])}");

        // simulationMode is a project setting: left at Script it gets saved into
        // ProjectSettings/Physics2DSettings.asset on exit.
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        EditorApplication.Exit(0);
    }
}
