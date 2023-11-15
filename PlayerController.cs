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

    [Header("Stamina Settings")]
    public float maxStamina = 100.0f;
    public float currentStamina;
    public float staminaDrownTime = 5.0f; // How many seconds it takes to drown the stamina
    public float staminaDrownPercentage = 50.0f; // How much percentage of stamina will drown
    public float staminaRestoreAmount = 100.0f; // How much stamina is restored by a collectible

    public Collider[] staminaCollectibleTriggers; // Stamina collectible trigger zones

    private Vector3 moveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private float currentSpeed;
    private float velocityY;
    private bool isRunning;
    private CharacterController controller;
    private float initialSpeed;
    private float lastGroundedTime;
    private float lastJumpButtonTime;
    private int remainingJumps;
    private bool isStaminaDecreasing = true; // New flag to control stamina decrease
    private bool isSpeedBoostActive; // Indicates if the speed boost is active

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        isRunning = false;
        lastGroundedTime = -0.1f;
        lastJumpButtonTime = -0.1f;
        initialSpeed = moveSpeed;
        remainingJumps = 1;
        isSpeedBoostActive = false; // Initially, the speed boost is not active
        currentStamina = maxStamina;
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            isRunning = false;
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

        // Update stamina
        if (isStaminaDecreasing)
        {
            currentStamina -= (Time.deltaTime / staminaDrownTime) * (maxStamina * staminaDrownPercentage / 100);
            currentStamina = Mathf.Clamp(currentStamina, 0.0f, maxStamina);

            // Check if stamina should stop decreasing
            if (currentStamina <= maxStamina * staminaDrownPercentage / 100)
            {
                isStaminaDecreasing = false;
            }
        }

        // Check for stamina collectibles
        foreach (Collider collectibleTrigger in staminaCollectibleTriggers)
        {
            if (collectibleTrigger != null && collectibleTrigger.bounds.Contains(transform.position))
            {
                RestoreStamina();
                break;
            }
        }

        // Modify move speed and jump forces based on stamina
        float staminaMultiplier = currentStamina / maxStamina;

        float effectiveMoveSpeed = moveSpeed * staminaMultiplier;
        float effectiveRunSpeedMultiplier = runSpeedMultiplier * staminaMultiplier;
        float effectiveForwardRunSpeed = forwardRunSpeed * staminaMultiplier;

        float effectiveJumpForce = jumpForce * staminaMultiplier;
        float effectiveDoubleJumpForce = doubleJumpForce * staminaMultiplier;
        float effectiveTripleJumpForce = tripleJumpForce * staminaMultiplier;

        Vector3 cameraRight = playerCamera.right;
        cameraRight.y = 0.0f;
        cameraRight.Normalize();

        float speedMultiplier = isRunning ? effectiveRunSpeedMultiplier : 1.0f;

        Vector3 move = cameraRight * horizontalInput + playerCamera.forward * verticalInput;
        moveDirection = Vector3.Lerp(moveDirection, move.normalized * effectiveMoveSpeed * speedMultiplier, isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

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
            if (Time.time - lastGroundedTime <= 0.1f)
            {
                Jump(effectiveJumpForce);
            }
            else if (remainingJumps > 0)
            {
                if (canDoubleJump && Time.time - lastJumpButtonTime <= 0.1f)
                {
                    Jump(effectiveDoubleJumpForce);
                }
                else if (canTripleJump && Time.time - lastJumpButtonTime <= 0.1f)
                {
                    Jump(effectiveTripleJumpForce);
                }
            }
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

            controller.Move(forwardMove * effectiveForwardRunSpeed * Time.deltaTime);
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

    private void Jump(float jumpForce)
    {
        velocityY = jumpForce;
        isGrounded = false;
        remainingJumps--;
    }

    private void RestoreStamina()
    {
        currentStamina = Mathf.Clamp(currentStamina + staminaRestoreAmount, 0.0f, maxStamina);
    }

    private void SpecialJump()
    {
        velocityY = specialJumpForce;
        isGrounded = false;
    }
}
