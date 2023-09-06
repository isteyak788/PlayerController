using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float runSpeedMultiplier = 2.0f;
    public float accelerationTime = 1.0f;
    public float deaccelerationTime = 1.0f;
    public float jumpForce = 10.0f;
    public float gravity = 20.0f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public Transform playerCamera;
    public Transform body;

    [Header("Forward Dash Settings")]
    public float forwardDashSpeed = 10.0f;
    public bool canForwardDash = true;
    public float forwardRotationSmoothness = 10.0f;
    public float forwardGraceTime = 0.1f;
    public float forwardDashTime = 0.5f;
    public float forwardDashCooldown = 2.0f; // Cooldown time for forward dash

    [Header("Backward Dash Settings")]
    public float backwardDashSpeed = 10.0f;
    public bool canBackwardDash = true;
    public float backwardRotationSmoothness = 10.0f;
    public float backwardGraceTime = 0.1f;
    public float backwardDashTime = 0.5f;
    public float backwardDashCooldown = 2.0f; // Cooldown time for backward dash

    private Vector3 moveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private float currentSpeed;
    private float velocityY;
    private bool isRunning;
    private bool isDashing;
    private float initialYRotation;
    private float targetYRotation;
    private float lastGroundedTime;
    private float lastJumpButtonTime;
    private float lastForwardDashTime; // Added variable to track forward dash cooldown
    private float lastBackwardDashTime; // Added variable to track backward dash cooldown

    private CharacterController controller;
    private float initialSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        isRunning = false;
        isDashing = false;
        lastGroundedTime = -forwardGraceTime;
        lastJumpButtonTime = -forwardGraceTime;
        initialSpeed = moveSpeed;
        lastForwardDashTime = -forwardDashCooldown; // Initialize last dash times
        lastBackwardDashTime = -backwardDashCooldown;
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            isRunning = false;
            isDashing = false;

            if (velocityY < 0)
            {
                velocityY = -0.5f;
            }
        }
        else if (wasGrounded)
        {
            lastJumpButtonTime = Time.time;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 cameraRight = playerCamera.right;
        cameraRight.y = 0.0f;
        cameraRight.Normalize();

        float speedMultiplier = isRunning ? runSpeedMultiplier : 1.0f;

        Vector3 move = cameraRight * horizontalInput + playerCamera.forward * verticalInput;
        moveDirection = Vector3.Lerp(moveDirection, move.normalized * moveSpeed * speedMultiplier, isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

        if (!isGrounded)
        {
            velocityY -= gravity * Time.deltaTime;
        }
        else
        {
            velocityY = -gravity * Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            lastJumpButtonTime = Time.time;
            if (Time.time - lastGroundedTime <= forwardGraceTime)
            {
                Jump();
            }
        }

        // Forward Dash input and execution
        if (canForwardDash && Input.GetKeyDown(KeyCode.Q) && !isDashing && Time.time - lastForwardDashTime >= forwardDashCooldown)
        {
            StartCoroutine(ForwardDash());
        }

        // Backward Dash input and execution (using the "E" key)
        if (canBackwardDash && Input.GetKeyDown(KeyCode.E) && !isDashing && Time.time - lastBackwardDashTime >= backwardDashCooldown)
        {
            StartCoroutine(BackwardDash());
        }

        if (move.magnitude > 0)
        {
            RotateTowardsMovementDirection(move);
        }

        controller.Move((moveDirection + Vector3.up * velocityY) * Time.deltaTime);
    }

    private void Jump()
    {
        if (Time.time - lastGroundedTime <= forwardGraceTime)
        {
            velocityY = jumpForce;
            isGrounded = false;
        }
    }

    private void RotateTowardsMovementDirection(Vector3 direction)
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentSpeed, 0.1f);
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    private IEnumerator ForwardDash()
    {
        isDashing = true;
        lastForwardDashTime = Time.time; // Record the time of the forward dash

        Vector3 startPosition = transform.position;
        float startTime = Time.time;

        Vector3 dashDirection = playerCamera.forward;
        dashDirection.y = 0.0f; // Ignore changes in the Y-axis.

        initialYRotation = transform.rotation.eulerAngles.y;
        targetYRotation = Quaternion.LookRotation(dashDirection).eulerAngles.y;

        while (Time.time - startTime < forwardDashTime)
        {
            float currentYRotation = Mathf.LerpAngle(initialYRotation, targetYRotation, (Time.time - startTime) / forwardDashTime);
            transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
            controller.Move(dashDirection.normalized * forwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
    }


    private IEnumerator BackwardDash()
    {
        isDashing = true;
        lastBackwardDashTime = Time.time; // Record the time of the backward dash

        Vector3 startPosition = transform.position;
        float startTime = Time.time;

        Vector3 dashDirection = -playerCamera.forward;

        initialYRotation = transform.rotation.eulerAngles.y;
        targetYRotation = Quaternion.LookRotation(dashDirection).eulerAngles.y;

        while (Time.time - startTime < backwardDashTime)
        {
            float currentYRotation = Mathf.LerpAngle(initialYRotation, targetYRotation, (Time.time - startTime) / backwardDashTime);
            transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
            controller.Move(dashDirection * backwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
    }
}
