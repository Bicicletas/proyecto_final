using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    Rigidbody _playerRigidbody;

    private GrapplingController grapplingController;
    private ForceFieldShooter forceFieldShooter;

    [SerializeField] private Transform virtualCam;
    [SerializeField] private Transform cam;

    [Header("Speed Parametres\n")]
    [SerializeField] private float force = 30f;
    [SerializeField] private float groundDrag = 10f;
    [SerializeField] private float airMultiplyer;
    [SerializeField] private float fieldOfView;

    [HideInInspector] public bool canMove = true;
    private bool canSlide = true;
    private bool isSliding;
    private bool isRunning;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    [Header("Jump Parametres\n")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float playerHight;

    public LayerMask whatIsGorund;

    private bool canJump = true;
    [HideInInspector] public bool canSecondJump;
    [HideInInspector] public bool isGrounded = true;

    private bool canWallJumpRight;
    private bool canWallJumpLeft;
    private bool canWallJumpFront;
    private bool canWallJumpBack;

    [Header("Wall Attatch\n")]
    public LayerMask whatIsWalls;

    private bool canAttatch;
    private bool isWalledRight;
    private bool isWalledLeft;
    private bool isWalledFront;
    private bool isWalledBack;

    [Header("Gravity Modifier\n")]
    [SerializeField] private float wallGrav = -1f;
    [SerializeField] private float normalGrav = -10f;


    void Start()
    {
        _playerRigidbody = GetComponent<Rigidbody>();
        grapplingController = FindObjectOfType<GrapplingController>();
        forceFieldShooter = FindObjectOfType<ForceFieldShooter>();

        Cursor.lockState = CursorLockMode.Locked;

        transform.rotation = new Quaternion(0, -180, 0, 0);
    }

    private void Update()
    {
        if (canMove)
        {
            PlayerInput();
            SpeedControl();
            Running(isRunning);

            transform.rotation = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0);

            if (isGrounded)
            {
                _playerRigidbody.drag = groundDrag;
                canAttatch = true;
                CantWallJump();
                Physics.gravity = new Vector3(0, normalGrav, 0);
            }
            else
            {
                _playerRigidbody.drag = 1f;
            }

            if (isWalledRight || isWalledLeft || isWalledBack || isWalledFront)
            {
                Physics.gravity = new Vector3(0, wallGrav, 0);
            }
            else if (!isGrounded)
            {
                Physics.gravity = new Vector3(0, normalGrav, 0);
            }

            if (isWalledRight)
            {
                canWallJumpRight = true;
                ResetOtherJumps(isWalledBack, isWalledFront, isWalledLeft);
            }
            else if (isWalledLeft)
            {
                canWallJumpLeft = true;
                ResetOtherJumps(isWalledBack, isWalledFront, isWalledRight);
            }
            else if (isWalledFront)
            {
                canWallJumpFront = true;
                ResetOtherJumps(isWalledBack, isWalledLeft, isWalledRight);
            }
            else if (isWalledBack)
            {
                canWallJumpBack = true;
                ResetOtherJumps(isWalledLeft, isWalledFront, isWalledRight);
            }
        }
    }

    void FixedUpdate()
    {
        MovePlayer();

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHight * 0.5f + 0.1f, whatIsGorund);
    }

    void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space) && isGrounded)
        {
            if (canJump)
            {
                canJump = false;

                JumpMechanic();

                Invoke(nameof(JumpReset), jumpCooldown);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && canSecondJump && !grapplingController.isGrappled && !isGrounded)
        {
            canSecondJump = false;

            JumpMechanic();
        }

        if (canAttatch)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                AttatchWallRight();
                AttatchWallLeft();
                AttatchWallFront();
                AttatchWallBack();

                if (!isWalledRight || !isWalledLeft || !isWalledBack || !isWalledFront)
                {
                    Physics.gravity = new Vector3(0, normalGrav, 0);
                    CantWallJump();
                }
            }
            else
            {
                NotWalled();
                Invoke(nameof(WallJumpReset), jumpCooldown);
            }
        }
        else
        {
            Physics.gravity = new Vector3(0, normalGrav, 0);

            NotWalled();
            CantWallJump();
        }
        
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (canJump && canAttatch)
            {
                if (canWallJumpRight)
                {
                    canJump = false;
                    canAttatch = false;
                    canWallJumpRight = false;

                    WallJumpMechanic(-transform.right);

                    Invoke(nameof(WallJumpReset), jumpCooldown);
                }
                
                if (canWallJumpLeft)
                {
                    canJump = false;
                    canAttatch = false;
                    canWallJumpLeft = false;

                    WallJumpMechanic(transform.right);

                    Invoke(nameof(WallJumpReset), jumpCooldown);
                }

                if (canWallJumpFront)
                {
                    canJump = false;
                    canAttatch = false;
                    canWallJumpFront = false;

                    WallJumpMechanic(-transform.forward);

                    Invoke(nameof(WallJumpReset), jumpCooldown);
                }

                if (canWallJumpBack)
                {
                    canJump = false;
                    canAttatch = false;
                    canWallJumpBack = false;

                    WallJumpMechanic(transform.forward);

                    Invoke(nameof(WallJumpReset), jumpCooldown);
                }
            }
        }


        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isRunning = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isRunning = false;
        }

        if (Input.GetKey(KeyCode.LeftShift) && isGrounded && canSlide)
        {
            canSlide = false;
            isSliding = true;
            GetComponent<CapsuleCollider>().height = 1f;
            _playerRigidbody.AddForce(Vector3.down * force, ForceMode.Impulse);
        }
        else if(Input.GetKeyUp(KeyCode.LeftShift) || !isGrounded)
        {
            GetComponent<CapsuleCollider>().height = 2f;
            canSlide = true;
            isSliding = false;
        }
    }

    void AttatchWallLeft()
    {
        isWalledLeft = Physics.Raycast(transform.position, -transform.right, playerHight * 0.25f + 0.25f, whatIsWalls);
    }

    void AttatchWallRight()
    {
        isWalledRight = Physics.Raycast(transform.position, transform.right, playerHight * 0.25f + 0.25f, whatIsWalls);
    }

    void AttatchWallFront()
    {
        isWalledFront = Physics.Raycast(transform.position, transform.forward, playerHight * 0.25f + 0.25f, whatIsWalls);
    }

    void AttatchWallBack()
    {
        isWalledBack = Physics.Raycast(transform.position, -transform.forward, playerHight * 0.25f + 0.25f, whatIsWalls);
    }

    void MovePlayer()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (isGrounded)
        {
            if (isSliding)
            {
                _playerRigidbody.AddForce(moveDirection.normalized * force * 2f, ForceMode.Force);
            }
            else if (isRunning)
            {
                _playerRigidbody.AddForce(moveDirection.normalized * force * 13f, ForceMode.Force);
            }
            else
            {
                _playerRigidbody.AddForce(moveDirection.normalized * force * 10f, ForceMode.Force);
            }
        }
        else if (isWalledRight || isWalledLeft || isWalledBack || isWalledFront)
        {
            _playerRigidbody.AddForce(moveDirection.normalized * force * 2f, ForceMode.Force);
        }
        else if (grapplingController.isGrappled)
        {
            _playerRigidbody.AddForce(moveDirection.normalized * force * 6f * airMultiplyer, ForceMode.Force);
        }
        else
        {
            _playerRigidbody.AddForce(moveDirection.normalized * force * 8f * airMultiplyer, ForceMode.Force);
        }   
    }

    void Running(bool b)
    {

        float fov;
        fov = virtualCam.GetComponent<CinemachineVirtualCamera>().m_Lens.FieldOfView;

        if (b)
        {
            fov = Mathf.Lerp(fov, fieldOfView + 5, 0.05f);
        }
        else if(isGrounded)
        {
            fov = Mathf.Lerp(fov, fieldOfView, 0.05f);
        }

        virtualCam.GetComponent<CinemachineVirtualCamera>().m_Lens.FieldOfView = fov;
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(_playerRigidbody.velocity.x, 0, _playerRigidbody.velocity.z);
        float newForce = force / 2f;

        if(flatVel.magnitude > newForce && !isRunning)
        {
            Vector3 limitedVel = flatVel.normalized * newForce;
            _playerRigidbody.velocity = new Vector3(limitedVel.x, _playerRigidbody.velocity.y, limitedVel.z);
        }
    }

    void JumpMechanic()
    {
        _playerRigidbody.velocity = new Vector3(_playerRigidbody.velocity.x, 0f, _playerRigidbody.velocity.z);

        _playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void WallJumpMechanic(Vector3 direction)
    {
        _playerRigidbody.velocity = new Vector3(_playerRigidbody.velocity.x, 0f, _playerRigidbody.velocity.z);

        _playerRigidbody.AddForce(Vector3.up * jumpForce * 0.7f, ForceMode.Impulse);
        _playerRigidbody.AddForce(direction * jumpForce * 0.8f, ForceMode.Impulse);
    }
    
    void JumpReset()
    {
        canJump = true;
        canAttatch = true;
    }

    void WallJumpReset()
    {
        canJump = true;
        canAttatch = true;
        CantWallJump();
    }

    void NotWalled()
    {
        isWalledBack = false;
        isWalledFront = false;
        isWalledLeft = false;
        isWalledRight = false;
    }

    void CantWallJump()
    {
        canWallJumpBack = false;
        canWallJumpFront = false;
        canWallJumpLeft = false;
        canWallJumpRight = false;
    }

    void ResetOtherJumps(bool b1, bool b2, bool b3)
    {
        b1 = false;
        b2 = false;
        b3 = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SecondJump"))
        {
            canSecondJump = true;
            StartCoroutine(Delay(other));
            Destroy(other.gameObject, 0.45f);
            other.GetComponent<Animator>().Play("forcefield_destroy");
        }
    }

    IEnumerator Delay(Collider other)
    {
        yield return new WaitForSeconds(0.40f);
        Instantiate(forceFieldShooter.destroyParticlePurple, other.transform.position, Quaternion.identity);
    }
}
