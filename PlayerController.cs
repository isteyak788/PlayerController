using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    
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
    public float forwardDashCooldown = 2.0f;

    [Header("Backward Dash Settings")]
    public float backwardDashSpeed = 10.0f;
    public float backwardDashTime = 0.5f;
    public float backwardDashCooldown = 2.0f;
    public GameObject backwardDashWeapon;

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
    private bool isSpeedBoostActive;
    private float targetForwardRunSpeed;
    private float lastForwardDashTime;
    private bool isDashing;
    private float lastBackwardDashTime;
    private bool isBackwardDashing;
    private Vector3 velocity;

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
        //isRunning = false;
        lastGroundedTime = -0.1f;
        lastJumpButtonTime = -0.1f;
        //initialSpeed = moveSpeed;
        remainingJumps = 1;
        isSpeedBoostActive = false;
        lastBackwardDashTime = -forwardDashCooldown;
        lastDownwardDashTime = -downwardDashCooldown;
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            //isRunning = false;
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

        //float horizontalInput = Input.GetAxis("Horizontal");
        //float verticalInput = Input.GetAxis("Vertical");

        //isRunning = Input.GetKey(KeyCode.LeftShift);

        float staminaMultiplier = 1.0f;

        //float effectiveMoveSpeed = moveSpeed * staminaMultiplier;
        float effectiveForwardRunSpeed = forwardRunSpeed * staminaMultiplier;

        float effectiveJumpForce = jumpForce * staminaMultiplier;
        float effectiveDoubleJumpForce = doubleJumpForce * staminaMultiplier;
        float effectiveTripleJumpForce = tripleJumpForce * staminaMultiplier;

        Vector3 cameraForward = playerCamera.forward;
        cameraForward.y = 0.0f;
        cameraForward.Normalize();

        //Vector3 move = (cameraForward * verticalInput + playerCamera.right * horizontalInput).normalized;
        //moveDirection = Vector3.Lerp(moveDirection, move * effectiveMoveSpeed * (isRunning ? 2.0f : 1.0f), isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

        //velocity = Vector3.Lerp(velocity, move * effectiveMoveSpeed * (isRunning ? 2.0f : 1.0f), isGrounded ? Time.deltaTime / accelerationTime : Time.deltaTime / deaccelerationTime);

        if (!isGrounded)
        {
            velocityY -= gravity * Time.deltaTime;
        }

        //isRunning = Input.GetKey(KeyCode.LeftShift);

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

        isCrouchRunning = Input.GetKey(KeyCode.S) && canCrouchRun;

        if (isCrouchRunning)
        {
            targetCrouchRunSpeed = crouchRunSpeed;
        }
        else if (!Input.GetKey(KeyCode.S) && !isCrouchRunning)
        {
            targetCrouchRunSpeed = 0f;
        }

        if (isCrouchRunning)
        {
            Vector3 forwardMove = playerCamera.forward;
            forwardMove.y = 0.0f;
            forwardMove.Normalize();

            controller.Move(forwardMove * targetCrouchRunSpeed * Time.deltaTime);
        }

        if (canForwardDash && Input.GetKeyDown(KeyCode.Q) && !isDashing && Time.time - lastForwardDashTime >= forwardDashCooldown)
        {
            StartCoroutine(ForwardDash());
        }

        if (canBackwardDash && Input.GetKeyDown(KeyCode.R) && !isDashing && Time.time - lastBackwardDashTime >= backwardDashCooldown)
        {
            StartCoroutine(BackwardDash());
        }

        if (canDownwardDash && Input.GetKeyDown(KeyCode.E) && !isDownwardDashing && Time.time - lastDownwardDashTime >= downwardDashCooldown)
        {
            StartCoroutine(DownwardDash());
        }

        if (Input.GetKey(KeyCode.W) && canForwardRun)
        {
            isForwardRunning = true;
            targetForwardRunSpeed = effectiveForwardRunSpeed;
        }
        else if (!Input.GetKey(KeyCode.W) && isForwardRunning)
        {
            targetForwardRunSpeed = 0f;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetForwardRunSpeed, Time.deltaTime / (isForwardRunning ? forwardRunAccelerationTime : forwardRunDecelerationTime));

        if (isForwardRunning)
        {
            Vector3 forwardMove = playerCamera.forward;
            forwardMove.y = 0.0f;
            forwardMove.Normalize();

            controller.Move(forwardMove * currentSpeed * Time.deltaTime);
        }

        if (enableSpecialJump)
        {
            foreach (Collider trigger in specialJumpTriggers)
            {
                if (trigger != null && trigger.bounds.Contains(transform.position))
                {
                    SpecialJump();
                    break;
                }
            }
        }

        if (enableSpeedBoost)
        {
            foreach (Collider trigger in speedBoostTriggers)
            {
                if (trigger != null && trigger.bounds.Contains(transform.position))
                {
                    isSpeedBoostActive = true;
                    break;
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
        lastForwardDashTime = Time.time;

        Vector3 dashDirection = playerCamera.forward;

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
            if (backwardDashWeapon == null || backwardDashWeapon.activeSelf)
            {
                isBackwardDashing = true;
                lastBackwardDashTime = Time.time;

                Vector3 dashDirection = -playerCamera.forward;

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

        Vector3 dashDirection = -Vector3.up;

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

    private void SpecialJump()
    {
        velocityY = specialJumpForce;
        isGrounded = false;
    }
}
