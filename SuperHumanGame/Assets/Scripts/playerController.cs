using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using UnityEngine.UI;

public class playerController : MonoBehaviour
{
    [Header("Movement")]
    float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float climbSpeed;
    public float teleportDistance;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask Ground;
    public bool grounded;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Climbing")]
    public Climbing climbingScript;
    public bool climbing;

    public Transform orientation;
    Vector3 spawnPoint;
    Rigidbody rb;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;

    public RawImage cursor;
    Color defaultColor;
    public Color teleportColor;

    void Start()
    {
        defaultColor = cursor.color;
        teleportColor.a = 1;

        spawnPoint = transform.position;

        moveSpeed = walkSpeed;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();

        //ground check -- 0.5f is half the players height and 0.2f is extra length
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, Ground);

        speedControl();

        if (grounded)
        {
            rb.linearDamping = 0;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    private void FixedUpdate()
    {
        PlayerMovement();
    }

    //logarithmic drag formula WIP
    private void ApplyDrag(float coeff)
    {
        Vector3 force = (coeff * rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude);
        rb.AddForce(-force);
    }

    private void speedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        //limit velocity
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        //Space to Jump
        if (Input.GetKeyDown(KeyCode.Space) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        //LMB to teleport
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;

            if (Physics.Raycast(ray, out hit, teleportDistance))
            {
                transform.position = hit.point;
            }
        }

        //RMB to teleport to spawn
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            gameObject.transform.position = spawnPoint;
        }

        if (Physics.Raycast(ray, out hit, teleportDistance))
        {
            cursor.color = teleportColor;
        }
        else
        {
            cursor.color = defaultColor;
        }
    }

    void PlayerMovement()
    {
        if (climbingScript.exitingWall) return;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        //calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Mode - Climbing
        if (climbing == true)
        {
            moveSpeed = climbSpeed;
        }

        // Mode - Sprinting
        else if (grounded && Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = sprintSpeed;

        }
        else
        {
            moveSpeed = walkSpeed;
        }
    }
    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
}
