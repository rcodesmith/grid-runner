using UnityEngine;

/// <summary>
/// Smoothly follows a target (the player) on the XY plane, keeping the
/// camera's original Z. Needed because the world spans two rooms and no
/// longer fits a single fixed view.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    const float Smoothing = 8f;

    Transform _target;

    public void Init(Transform target)
    {
        _target = target;
        transform.position = Desired();
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        float k = 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, Desired(), k);
    }

    Vector3 Desired()
    {
        return new Vector3(_target.position.x, _target.position.y, transform.position.z);
    }
}
