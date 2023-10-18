using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15.0f;
    public float runSpeedMultiplier = 2.0f;
    public float accelerationTime = 0.0f;
    public float deaccelerationTime = 0.0f;
    public float gravity = 30.0f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public Transform playerCamera;
    public Transform body;

    [Header("Forward Dash Settings")]
    public float forwardDashSpeed = 400.0f;
    public bool canForwardDash = true;
    public float forwardRotationSmoothness = 10.0f;
    public float forwardGraceTime = 0.1f;
    public float forwardDashTime = 0.1f;
    public float forwardDashCooldown = 0.5f;

    [Header("Jump Settings")]
    public float jumpForce = 20.0f;
    public float doubleJumpForce = 20.0f;
    public float tripleJumpForce = 25.0f;

    [Header("Jump Options")]
    public bool canDoubleJump = true;
    public bool canTripleJump = true;

    [Header("Forward Run Settings")]
    public float forwardRunSpeed = 40.0f;
    public bool canForwardRun = true;
    private bool isForwardRunning;

    [Header("Special Jump Settings")]
    public float specialJumpForce = 30.0f;
    public Collider[] specialJumpTriggers;

    [Header("Speed Boost Settings")]
    public float speedBoostFactor = 2.0f;
    public Collider[] speedBoostTriggers;

    private Vector3 moveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private float currentSpeed;
    private float velocityY;
    private bool isRunning;
    private bool isDashing;
    private CharacterController controller;
    private float initialSpeed;
    private float initialYRotation;
    private float targetYRotation;
    private float lastGroundedTime;
    private float lastJumpButtonTime;
    private float lastForwardDashTime;
    private Vector3 dashDirection;
    private int remainingJumps;

    private bool isSpeedBoostActive; // Indicates if the speed boost is active

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        isRunning = false;
        isDashing = false;
        lastGroundedTime = -forwardGraceTime;
        lastJumpButtonTime = -forwardGraceTime;
        initialSpeed = moveSpeed;
        lastForwardDashTime = -forwardDashCooldown;
        remainingJumps = 1;
        isSpeedBoostActive = false; // Initially, the speed boost is not active
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
            remainingJumps = canTripleJump ? 3 : (canDoubleJump ? 2 : 1);

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

        if (isSpeedBoostActive)
        {
            moveSpeed = initialSpeed * speedBoostFactor;
        }
        else
        {
            moveSpeed = initialSpeed;
        }

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
            else if (remainingJumps > 0)
            {
                if (canDoubleJump && Time.time - lastJumpButtonTime <= forwardGraceTime)
                {
                    DoubleJump();
                }
                else if (canTripleJump && Time.time - lastJumpButtonTime <= forwardGraceTime)
                {
                    TripleJump();
                }
            }
        }

        if (canForwardDash && Input.GetKeyDown(KeyCode.Q) && !isDashing && Time.time - lastForwardDashTime >= forwardDashCooldown)
        {
            dashDirection = playerCamera.forward;
            StartCoroutine(ForwardDash());
        }

        // Handle forward running
        if (Input.GetKey(KeyCode.E) && canForwardRun)
        {
            isForwardRunning = true;
            isRunning = false;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            isForwardRunning = false;
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        // Forward run
        if (isForwardRunning)
        {
            Vector3 forwardMove = playerCamera.forward;
            forwardMove.y = 0.0f;
            forwardMove.Normalize();

            controller.Move(forwardMove * forwardRunSpeed * Time.deltaTime);
        }

        // Check if the player is inside any of the trigger zones and trigger the special jump
        foreach (Collider trigger in specialJumpTriggers)
        {
            if (trigger != null && trigger.bounds.Contains(transform.position))
            {
                SpecialJump();
                break; // Exit the loop after the first trigger found
            }
        }

        // Check if the player is inside any of the trigger zones and activate the speed boost
        foreach (Collider trigger in speedBoostTriggers)
        {
            if (trigger != null && trigger.bounds.Contains(transform.position))
            {
                isSpeedBoostActive = true;
                break; // Exit the loop after the first trigger found
            }
            else
            {
                isSpeedBoostActive = false;
            }
        }

        controller.Move((moveDirection + Vector3.up * velocityY) * Time.deltaTime);
    }

    private void Jump()
    {
        velocityY = jumpForce;
        isGrounded = false;
        remainingJumps--;
    }

    private void DoubleJump()
    {
        velocityY = doubleJumpForce;
        isGrounded = false;
        remainingJumps--;
    }

    private void TripleJump()
    {
        velocityY = tripleJumpForce;
        isGrounded = false;
        remainingJumps--;
    }

    private void SpecialJump()
    {
        velocityY = specialJumpForce;
        isGrounded = false;
    }

    private IEnumerator ForwardDash()
    {
        isDashing = true;
        lastForwardDashTime = Time.time;

        initialYRotation = transform.rotation.eulerAngles.y;
        targetYRotation = Quaternion.LookRotation(dashDirection).eulerAngles.y;

        while (Time.time - lastForwardDashTime < forwardDashTime)
        {
            float currentYRotation = Mathf.LerpAngle(initialYRotation, targetYRotation, (Time.time - lastForwardDashTime) / forwardDashTime);
            transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
            controller.Move(dashDirection.normalized * forwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
    }
}
