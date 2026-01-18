using UnityEngine;

public interface ITarget
{
    Transform targetTransform { get; }
    Vector3 position { get; }
    Quaternion rotation { get; }
    bool IsAlive { get; }
}
