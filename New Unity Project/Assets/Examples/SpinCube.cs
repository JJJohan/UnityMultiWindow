using JetBrains.Annotations;
using UnityEngine;

public class SpinCube : MonoBehaviour
{
    private Transform _transform;

    [UsedImplicitly]
    private void Awake()
    {
        _transform = transform;
    }

    [UsedImplicitly]
    private void Update()
    {
        float rotation = 10f * Time.deltaTime;
        _transform.Rotate(rotation, rotation, rotation);
    }
}
