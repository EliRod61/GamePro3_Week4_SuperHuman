using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.Burst;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using Unity.VisualScripting;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    float currentMoveSpeed;
    float desiredMoveSpeed;
    float lastDesiredMoveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float climbSpeed;
    float drag;

    public float speedIncreaseMultiplier;
    public float speedBoostMultiplier;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    [SerializeField] float airMultiplier;
    bool readyToJump;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Timers")]
    public float walkingSound_Timer = 0f;
    public float BoostCooldown = 5f, BoostTimer;
    public float TeleportCooldown = 5f, TeleportTimer;
    public float ReturnCooldown = 4f, ReturnTimer;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;
    BouncePad Standing_On;
    Checkpoints checkpoints;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    RaycastHit slopeHit;
    bool exitingSlope;

    [Header("Blob Shadow")]
    public GameObject shadow;
    public RaycastHit hit;
    public float offset;

    [Header("Boost")]
    public GameObject BoostBarMeter;
    public GameObject speedParticle;

    [Header("Teleportation")]
    public RawImage cursor;
    UnityEngine.Color defaultColor;
    public UnityEngine.Color teleportColor;
    Vector3 positionBeforeTeleport;
    public float teleportDistance;
    public bool canTeleport = true, canReturnTeleport = true;

    [Header("References")]
    public Climbing climbingScript;
    public GameObject pauseMenu;
    public Transform orientation;
    [SerializeField] Animator deathAnim;

    Rigidbody rb;
    Vector3 spawnPoint;
    Vector3 moveDirection;
    float horizontalInput;
    float verticalInput;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        teleporting,
        climbing,
        air
    }
    public bool walking, inAir, wallrunning, climbing, playerIsMoving;
    void Start()
    {
        defaultColor = cursor.color;
        teleportColor.a = 1;

        spawnPoint = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
    }

    void Update()
    {
        GroundDetection();

        walking = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.2f);
        playerIsMoving = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f);

        TimerManager();
        MyInput();
        TeleportSkill();
        SpeedControl();
        StateHandler();

        // handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

        if (gameObject.transform.position.y < -25f)
        {
            StartCoroutine(DeathScene());
        }
    }
    void FixedUpdate()
    {
        MovePlayer();

        Ray downRay = new Ray(new Vector3(this.transform.position.x, this.transform.position.y - offset, this.transform.position.z), -Vector3.up);

        //gets the hit from the raycast and converts it unto a vector3
        Vector3 hitPosition = hit.point;
        //transform the shadow to the location
        //shadow.transform.position = hitPosition;

        //Cast a ray straight downwards, reads back where it leads
        if (Physics.Raycast(downRay, out hit))
        {
            print(hit.transform.tag);
        }

        QuadraticDrag(drag);
    }

    void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // Mode - Sprinting
        if (grounded && Input.GetKey(KeyCode.LeftShift))
        {
            state = MovementState.sprinting;

        }

        //Pause
        if (Input.GetKeyDown(pauseKey))
        {
            pauseMenu.SetActive(true);
        }

    }
    void StateHandler()
    {
        switch (state)
        {
            case MovementState.walking:
                desiredMoveSpeed = walkSpeed;
                drag = 1f;
                break;
            case MovementState.sprinting:
                desiredMoveSpeed = sprintSpeed;
                drag = 1f;
                break;
            case MovementState.teleporting:
                desiredMoveSpeed = 0f;
                drag = 3f;
                break;
            case MovementState.climbing:
                desiredMoveSpeed = climbSpeed;
                drag = 1f;
                break;
            case MovementState.air:
                drag = 15f;
                break;
        }

        //check if desiredMoveSpeed has changed drastically
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 20f && currentMoveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            currentMoveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    IEnumerator SmoothlyLerpMoveSpeed()
    {
        //smooothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - currentMoveSpeed);
        float startValue = currentMoveSpeed;

        while (time < difference)
        {
            currentMoveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeAngleIncrease;//slopeIncreaseMultiplier 
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        currentMoveSpeed = desiredMoveSpeed;
    }

    IEnumerator DeathScene()
    {
        deathAnim.Play("ScreenFade_In");
        yield return new WaitForSeconds(1.45f);
        RespawnPlayer();
        deathAnim.Play("ScreenFade_Out");
    }

    void MovePlayer()
    {
        if (climbingScript.exitingWall) return;

        // calculate movement direction and walk in the direction you are looking
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * currentMoveSpeed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 1f, ForceMode.Force);
        }

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f * airMultiplier, ForceMode.Force);
        if (climbingScript.exitingWall) return;

        //turn gravity off while on slope
        if (!wallrunning) rb.useGravity = !OnSlope();
    }

    void SpeedControl()
    {
        //limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > currentMoveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * currentMoveSpeed;
        }

        //limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > currentMoveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * currentMoveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }
    
    // Method to calculate drag force
    public static double CalculateDragForce(double dragCoefficient, double airDensity, double crossSectionalArea, double velocity)
    {
        // Applying the drag equation: Fd = 0.5 * Cd * rho * A * v^2
        double dragForce = 0.5 * dragCoefficient * airDensity * crossSectionalArea * Math.Pow(velocity, 2);
        return dragForce;
    }

    public double QuadraticDrag(double dragCoefficient)
    {
        // Example values for the drag force calculation
        //double dragCoefficient = 1;//0.47; // for a typical car
        double airDensity = 1.225;     // air density at sea level in kg/m³
        double crossSectionalArea = 2.5;  // in m² (example car)
        double velocity = currentMoveSpeed;      // speed in m/s

        double dragForce = CalculateDragForce(dragCoefficient, airDensity, crossSectionalArea, velocity);
        return -dragForce / 1;
    }

    void Jump()
    {
        exitingSlope = true;

        //SoundManager.PlaySound(SoundSource.Player, SoundType.Player_Jumping, 0.2f, System.Random(0.9f, 1.2f);

        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * SetBounceStrength(), ForceMode.Impulse);
    }
    void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    public void UpdateCheckpoint(Vector3 pos)
    {
        spawnPoint = pos;
    }
    public void RespawnPlayer()
    {
        gameObject.transform.position = spawnPoint;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    void GroundDetection()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
    }
    void OnTriggerEnter(Collider other)
    {
        //Death
        if (other.gameObject.CompareTag("Hazard"))
        {
            RespawnPlayer();
        }

        if (other.gameObject.GetComponent<Checkpoints>())
        {
            GameObject checkpoint = other.gameObject.GetComponent<GameObject>();
        }

        BouncePad bouncePad = other.gameObject.GetComponent<BouncePad>();
        if (bouncePad != null)
        {
            Standing_On = bouncePad;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        BouncePad bouncePad = other.gameObject.GetComponent<BouncePad>();
        if (bouncePad != null)
        {
            Standing_On = null;
        }

        if (other.gameObject.CompareTag("Slope"))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

    }

    public float SetBounceStrength()
    {
        if (Standing_On) return Standing_On.Bounce_Strength;
        return jumpForce;

    }

    void TeleportSkill()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, teleportDistance, whatIsGround))
        {
            cursor.color = teleportColor;

            //LMB to teleport
            if (Input.GetKeyUp(KeyCode.Mouse0) && canTeleport == true)
            {
                canTeleport = false;
                TeleportTimer = TeleportCooldown;

                if (grounded)
                    positionBeforeTeleport = gameObject.transform.position;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                transform.position = hit.point;
            }
        }
        else
        {
            cursor.color = defaultColor;
        }

        //RMB to teleport to position before teleport
        if (Input.GetKeyUp(KeyCode.Mouse1) && canReturnTeleport == true)
        {
            canReturnTeleport = false;
            ReturnTimer = ReturnCooldown;
            gameObject.transform.position = positionBeforeTeleport;
        }
    }

    void TimerManager()
    {
        if (canTeleport == false)
        {
            TeleportTimer -= Time.deltaTime;

            if (TeleportTimer <= 0f)
            {
                canTeleport = true;
            }
        }

        if (canReturnTeleport == false)
        {
            ReturnTimer -= Time.deltaTime;

            if (ReturnTimer <= 0f)
            {
                canReturnTeleport = true;
            }
        }
    }


}
