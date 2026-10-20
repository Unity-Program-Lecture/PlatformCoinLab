using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlatformMover : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    [SerializeField] private GameSession session;
    [SerializeField] private float speed = 5f;

    private void OnEnable()
    {
        move.action.Enable();
    }

    private void OnDisable()
    {
        move.action.Disable();
    }

    private void Update()
    {
        if (!session.CanMove)
        {
            return;
        }

        Vector2 input = Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f);
        Vector3 next = transform.position + new Vector3(input.x, 0f, input.y) * speed * Time.deltaTime;
        next.x = Mathf.Clamp(next.x, -9f, 9f);
        next.z = Mathf.Clamp(next.z, -9f, 9f);
        transform.position = next;
    }
}
