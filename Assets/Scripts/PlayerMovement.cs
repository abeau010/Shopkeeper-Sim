using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Player's movement speed
    [SerializeField] private float moveSpeed = 5f;

    // Variable for the rigidbody component
    private Rigidbody2D rb;
    // Variable for the movement direction
    private Vector2 movementInput;

    // When the player is initiated: find rigidbody component
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Gets the direction of movement
    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    // Moves the player character in the correct direction
    private void FixedUpdate()
    {
        rb.linearVelocity = movementInput * moveSpeed;
    }
}
