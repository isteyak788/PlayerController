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

    [Header("Forward Dash Settings")]
    public float forwardDashSpeed = 10.0f;
    public float forwardDashTime = 0.5f;
    public float forwardDashCooldown = 2.0f; // Cooldown time for forward dash

    [Header("Backward Dash Settings")]
    public float backwardDashSpeed = 10.0f;
    public float backwardDashTime = 0.5f;
    public float backwardDashCooldown = 2.0f;
    public GameObject backwardDashWeapon; // Reference to the weapon object

    [Header("Dash Downward Settings")]
    public float downwardDashSpeed = 10.0f;
    public bool canDownwardDash = true;
    public float downwardDashTime = 0.5f;
    public float downwardDashCooldown = 2.0f;

    private bool isDownwardDashing;
    private float lastDownwardDashTime;

    [Header("Forward Run Settings")]
    public float forwardRunSpeed = 40.0f;
    public float forwardRunAccelerationTime = 1.0f;
    public float forwardRunDecelerationTime = 1.0f;
    private bool isForwardRunning;

    [Header("Crouch Run Settings")]
    public float crouchRunSpeed = 40.0f;
    public float crouchRunAccelerationTime = 1.0f;
    public float crouchRunDecelerationTime = 1.0f;
    private bool isCrouchRunning;
    private float targetCrouchRunSpeed;

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

    [Header("Toggle Options")]
    public bool canDoubleJump = true;
    public bool canTripleJump = true;
    public bool canForwardDash = true;
    public bool canBackwardDash = true;
    public bool canForwardRun = true;
    public bool canCrouchRun = true;
    public bool enableSpecialJump = true;
    public bool enableSpeedBoost = true;

    [Header("Trigger Zones")]
    public Collider doubleJumpTrigger;
    public Collider tripleJumpTrigger;
    public Collider forwardDashTrigger;
    public Collider backwardDashTrigger;
    public Collider forwardRunTrigger;
    public Collider crouchRunTrigger;
    public Collider specialJumpTrigger;
    public Collider speedBoostTrigger;

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
    private bool isBackwardDashing;
    private Vector3 velocity; // Variable to store the velocity

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        if (other == doubleJumpTrigger)
        {
            canDoubleJump = true;
            Destroy(other.gameObject);
        }
        else if (other == tripleJumpTrigger)
        {
            canTripleJump = true;
            Destroy(other.gameObject);
        }
        else if (other == forwardDashTrigger)
        {
            canForwardDash = true;
            Destroy(other.gameObject);
        }
        else if (other == backwardDashTrigger)
        {
            canBackwardDash = true;
            Destroy(other.gameObject);
        }
        else if (other == forwardRunTrigger)
        {
            canForwardRun = true;
            Destroy(other.gameObject);
        }
        else if (other == crouchRunTrigger)
        {
            canCrouchRun = true;
            Destroy(other.gameObject);
        }
        else if (other == specialJumpTrigger)
        {
            enableSpecialJump = true;
            Destroy(other.gameObject);
        }
        else if (other == speedBoostTrigger)
        {
            enableSpeedBoost = true;
            Destroy(other.gameObject);
        }
    }


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
        lastDownwardDashTime = -downwardDashCooldown;
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

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Modify move speed and jump forces based on stamina
        float staminaMultiplier = currentStamina / maxStamina;

        float effectiveMoveSpeed = moveSpeed * staminaMultiplier;
        float effectiveForwardRunSpeed = forwardRunSpeed * staminaMultiplier;

        float effectiveJumpForce = jumpForce * staminaMultiplier;
        float effectiveDoubleJumpForce = doubleJumpForce * staminaMultiplier;
        float effectiveTripleJumpForce = tripleJumpForce * staminaMultiplier;

        Vector3 cameraForward = playerCamera.forward;
        cameraForward.y = 0.0f;
        cameraForward.Normalize();

        Vector3 move = (cameraForward * verticalInput + playerCamera.right * horizontalInput).normalized;
        moveDirection = Vector3.Lerp(moveDirection, move * effectiveMoveSpeed * (isRunning ? 2.0f : 1.0f), isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

        // velocity
        velocity = Vector3.Lerp(velocity, move * effectiveMoveSpeed * (isRunning ? 2.0f : 1.0f), isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

        if (!isGrounded)
        {
            velocityY -= gravity * Time.deltaTime;
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);


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



        //CrouchRunning//
        isCrouchRunning = Input.GetKey(KeyCode.LeftControl) && canCrouchRun;

        // Handle crouch running
        if (isCrouchRunning)
        {
            // Set the target speed during acceleration for the crouch run
            targetCrouchRunSpeed = crouchRunSpeed;
        }
        else if (!Input.GetKey(KeyCode.E) && !isCrouchRunning)
        {
            // Set the target speed during deceleration
            targetCrouchRunSpeed = 0f;
        }

        // Crouch run
        if (isCrouchRunning)
        {
            Vector3 forwardMove = playerCamera.forward;
            forwardMove.y = 0.0f;
            forwardMove.Normalize();

            controller.Move(forwardMove * targetCrouchRunSpeed * Time.deltaTime);
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


        // Downward Dash input and execution (R)
        if (canDownwardDash && Input.GetKeyDown(KeyCode.R) && !isDownwardDashing && Time.time - lastDownwardDashTime >= downwardDashCooldown)
        {
            StartCoroutine(DownwardDash());
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
        if (enableSpecialJump)
        {
            foreach (Collider trigger in specialJumpTriggers)
            {
                if (trigger != null && trigger.bounds.Contains(transform.position))
                {
                    SpecialJump();
                    break; // Exit the loop after the first trigger found
                }
            }
        }

        // Check if the player is inside any of the trigger zones and activate the speed boost
        if (enableSpeedBoost)
        {
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
        }

        controller.Move((moveDirection + Vector3.up * velocityY) * Time.deltaTime);
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
        if (canBackwardDash && !isBackwardDashing && Time.time - lastBackwardDashTime >= backwardDashCooldown)
        {
            // Check if the backward dash weapon is active
            if (backwardDashWeapon == null || backwardDashWeapon.activeSelf)
            {
                isBackwardDashing = true;
                lastBackwardDashTime = Time.time;

                Vector3 dashDirection = -playerCamera.forward; // Dash backward

                float startTime = Time.time;

                while (Time.time - startTime < backwardDashTime)
                {
                    controller.Move(dashDirection.normalized * backwardDashSpeed * Time.deltaTime);
                    yield return null;
                }

                isBackwardDashing = false;
            }
        }
    }


    private IEnumerator DownwardDash()
    {
        isDownwardDashing = true;
        lastDownwardDashTime = Time.time;

        Vector3 dashDirection = -Vector3.up; // Dash downward

        float startTime = Time.time;

        while (Time.time - startTime < downwardDashTime)
        {
            controller.Move(dashDirection.normalized * downwardDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDownwardDashing = false;
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
