using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15.0f;
    public float accelerationTime = 0.0f;
    public float deaccelerationTime = 0.0f;
    public float gravity = 30.0f;
    public float gravityMultiplier = 1.0f; // Multiplier for gravity when left shift is pressed
    private bool isGravityIncreased = false;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public Transform playerCamera;
    public Transform body;

    [Header("Jump Settings")]
    public float jumpForce = 20.0f;
    public float doubleJumpForce = 20.0f;
    public float tripleJumpForce = 25.0f;
    public bool canDoubleJump = true;
    public bool canTripleJump = true;

    [Header("Crouch Settings")]
    public float crouchHeight = 0.5f;
    public float standingHeight = 2.0f;
    public float crouchSpeed = 5.0f;
    [SerializeField] private float crouchTransitionTime = 0.2f; // Adjust the crouch transition time
    private bool isCrouching = false;
    private float crouchTransitionVelocity;


    [Header("Crouch Forward Settings")]
    public float crouchForwardSpeed = 5.0f; // Adjust the crouch forward speed
    public Collider[] crouchForwardTriggers; // Triggers for crouch forward movement


    [Header("Forward Dash Settings")]
    public float forwardDashSpeed = 10.0f;
    public bool canForwardDash = true;
    public float forwardDashTime = 0.5f;
    public float forwardDashCooldown = 2.0f; // Cooldown time for forward dash

    [Header("Backward Dash Settings")]
    public float backwardDashSpeed = 10.0f;
    public bool canBackwardDash = true;
    public float backwardDashTime = 0.5f;
    public float backwardDashCooldown = 2.0f; // Cooldown time for backward dash

    [Header("Forward Run Settings")]
    public float forwardRunSpeed = 40.0f;
    public bool canForwardRun = true;
    public float forwardRunAccelerationTime = 1.0f;
    public float forwardRunDecelerationTime = 1.0f;
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
    private float targetForwardRunSpeed; // Declare targetForwardRunSpeed at the class level

    private float lastForwardDashTime; // Added variable to track forward dash cooldown
    private bool isDashing;
    private float lastBackwardDashTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        isRunning = false;
        lastGroundedTime = -0.1f;
        lastJumpButtonTime = -0.1f;
        initialSpeed = moveSpeed;
        remainingJumps = 1;
        isSpeedBoostActive = false; // Initially, the speed boost is not active
        lastBackwardDashTime = -forwardDashCooldown; // Initialize to ensure immediate backward dash is possible
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
        // Crouch input handling
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isCrouching)
        {
            StartCrouch();
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl) && isCrouching)
        {
            StopCrouch();
        }

        // Crouch forward movement
        if (isCrouching && Input.GetKey(KeyCode.LeftControl))
        {
            foreach (Collider trigger in crouchForwardTriggers)
            {
                if (trigger != null && trigger.bounds.Contains(transform.position))
                {
                    CrouchForward();
                    break;
                }
            }
        }
        else
        {
            // Reset crouch forward movement when not crouching or control key is released
            ResetCrouchForward();
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
        float effectiveForwardRunSpeed = forwardRunSpeed * staminaMultiplier;

        float effectiveJumpForce = jumpForce * staminaMultiplier;
        float effectiveDoubleJumpForce = doubleJumpForce * staminaMultiplier;
        float effectiveTripleJumpForce = tripleJumpForce * staminaMultiplier;

        Vector3 cameraRight = playerCamera.right;
        cameraRight.y = 0.0f;
        cameraRight.Normalize();

        float speedMultiplier = isRunning ? 1.0f : 1.0f;

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

        if (!isGrounded)
        {
            velocityY -= gravity * gravityMultiplier * Time.deltaTime; // Use modified gravity here
        }
        else
        {
            velocityY = -gravity * gravityMultiplier * Time.deltaTime; // Use modified gravity here
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Check if left shift is pressed to increase gravity
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isGravityIncreased = true;
            gravityMultiplier = 2.0f; // Adjust the multiplier as needed
        }

        // Check if left shift is released to reset gravity
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isGravityIncreased = false;
            gravityMultiplier = 1.0f; // Reset to default multiplier
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



        // Forward Dash input and execution
        if (canForwardDash && Input.GetKeyDown(KeyCode.Q) && !isDashing && Time.time - lastForwardDashTime >= forwardDashCooldown)
        {
            StartCoroutine(ForwardDash());
        }

        // Backward Dash input and execution
        if (canBackwardDash && Input.GetKeyDown(KeyCode.R) && !isDashing && Time.time - lastBackwardDashTime >= backwardDashCooldown)
        {
            StartCoroutine(BackwardDash());
        }



        // Handle forward running
        if (Input.GetKey(KeyCode.E) && canForwardRun)
        {
            isForwardRunning = true;

            // Set the target speed during acceleration
            targetForwardRunSpeed = effectiveForwardRunSpeed;
        }
        else if (!Input.GetKey(KeyCode.E) && isForwardRunning)
        {
            // Set the target speed during deceleration
            targetForwardRunSpeed = 0f;
        }

        // Smoothly interpolate current speed towards the target speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetForwardRunSpeed, Time.deltaTime / (isForwardRunning ? forwardRunAccelerationTime : forwardRunDecelerationTime));

        // Forward run
        if (isForwardRunning)
        {
            Vector3 forwardMove = playerCamera.forward;
            forwardMove.y = 0.0f;
            forwardMove.Normalize();

            controller.Move(forwardMove * currentSpeed * Time.deltaTime);
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

    private void StartCrouch()
    {
        isCrouching = true;
        CrouchTransition(crouchHeight);

        // You may want to adjust other components such as camera position when crouching

        // You can also reduce movement speed or do other adjustments if needed
        moveSpeed /= crouchSpeed;
    }

    private void StopCrouch()
    {
        isCrouching = false;
        CrouchTransition(standingHeight);

        // You may want to adjust other components such as camera position when standing up

        // Restore movement speed to its original value
        moveSpeed *= crouchSpeed;
    }

    private void CrouchTransition(float targetHeight)
    {
        float currentHeight = controller.height;

        // Smoothly adjust the controller's height using Mathf.SmoothDamp
        controller.height = Mathf.SmoothDamp(currentHeight, targetHeight, ref crouchTransitionVelocity, crouchTransitionTime);

        // Make sure the height never exceeds the target height
        controller.height = Mathf.Min(controller.height, targetHeight);
    }


    private void CrouchForward()
    {
        // Move forward while crouching
        Vector3 forwardMove = playerCamera.forward;
        forwardMove.y = 0.0f;
        forwardMove.Normalize();

        controller.Move(forwardMove * crouchForwardSpeed * Time.deltaTime);
    }

    private void ResetCrouchForward()
    {
        // Reset any adjustments made for crouch forward movement
        // For example, you might stop the forward movement if needed.
    }

    private IEnumerator ForwardDash()
    {
        isDashing = true;
        lastForwardDashTime = Time.time; // Record the time of the forward dash

        Vector3 dashDirection = playerCamera.forward;
        //dashDirection.y = 0.0f; // Ignore changes in the Y-axis.

        float startTime = Time.time;

        while (Time.time - startTime < forwardDashTime)
        {
            controller.Move(dashDirection.normalized * forwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
    }

    private IEnumerator BackwardDash()
    {
        isDashing = true;
        lastBackwardDashTime = Time.time; // Record the time of the backward dash

        // Calculate the backward dash direction based on the camera's orientation
        Vector3 dashDirection = -playerCamera.transform.forward;
        //dashDirection.y = 0.0f; // Ignore changes in the Y-axis.//

        float startTime = Time.time;

        while (Time.time - startTime < backwardDashTime)
        {
            // Move the player in the calculated backward dash direction
            controller.Move(dashDirection.normalized * backwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
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
