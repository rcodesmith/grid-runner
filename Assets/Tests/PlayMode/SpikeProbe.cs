using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Research spike (issue #8): counts the physics callbacks a GameObject receives
/// and records the order they arrive in, relative to FixedUpdate.
/// </summary>
public class SpikeProbe : MonoBehaviour
{
    public int CollisionEnter;
    public int CollisionStay;
    public int TriggerEnter;
    public int FixedUpdates;
    public bool Awoken;
    public readonly List<string> Order = new List<string>();

    void Awake() { Awoken = true; }

    void FixedUpdate()
    {
        FixedUpdates++;
        Record("FixedUpdate");
    }

    void OnCollisionEnter2D(Collision2D c) { CollisionEnter++; Record("CollisionEnter"); }
    void OnCollisionStay2D(Collision2D c) { CollisionStay++; }
    void OnTriggerEnter2D(Collider2D c) { TriggerEnter++; Record("TriggerEnter"); }

    void Record(string what)
    {
        if (Order.Count < 12)
        {
            Order.Add(what + "@frame" + Time.frameCount);
        }
    }
}
