using UnityEngine;
using UnityEngine.Rendering;

public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Character Properties")]
    public CharacterController characterController;
    public Animator characterAnimator;
    public bool isDead = false;
    public float characterSpeed = 4.0f;
    public float jumpHeight = 1.3f;
    public float gravity = -30f;
    public Vector3 velocity;

    [Header("Camera Properties")] 
    public Transform cam;
    public float turnSmoothTime = 0.15f;

    // Private variables
    float turnSmoothVelocity;
    private Transform activePlatform;

    private PlayerFootstepsFMOD footstepFMOD;
    private PlayerSwimFMOD swimFMOD;
    private PlayerDiveFMOD diveFMOD;
    private WaterBuoyancy waterBuoyancy;

    private void Awake()
    {
        footstepFMOD = GetComponent<PlayerFootstepsFMOD>();
        if (footstepFMOD == null)
        {
            Debug.LogError("PlayerFootstepsFMOD NOT found on this GameObject");
        }
        else
        {
            Debug.Log("PlayerFootstepsFMOD successfully found");
        }

        diveFMOD = GetComponent<PlayerDiveFMOD>();
        swimFMOD = GetComponent<PlayerSwimFMOD>();
        waterBuoyancy = GetComponent<WaterBuoyancy>();
    }

    void Update()
    {
        if (Inventory.isInventoryOpen)
        {
            return;
        }

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontal = 0f;
        float vertical = 0f;
        float jump = 0f;

        if (!isDead)
        {
            if (RaftConstruction.isBuildingMode)
            {
                if (Input.GetKey(KeyCode.W)) vertical = 1f;
                if (Input.GetKey(KeyCode.S)) vertical = -1f;
                if (Input.GetKey(KeyCode.A)) horizontal = -1f;
                if (Input.GetKey(KeyCode.D)) horizontal = 1f;
            }
            else
            {
                horizontal = Input.GetAxis("Horizontal");
                vertical = Input.GetAxis("Vertical");
            }
            
            jump = Input.GetAxisRaw("Jump"); 
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f) 
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;                    
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime); 
            transform.rotation = Quaternion.Euler(0f, angle, 0f);                                                            

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            Vector3 rayStart = transform.position + Vector3.up * (characterController.height / 2f);
            if (Physics.Raycast(rayStart, moveDirection, out RaycastHit hit, characterController.radius + 0.5f))
            {
                if (hit.collider.CompareTag("Barricade"))
                {
                    float safeDistance = characterController.radius + characterController.skinWidth + 0.02f;
                    if (hit.distance <= safeDistance)
                    {
                        moveDirection = Vector3.ProjectOnPlane(moveDirection, hit.normal).normalized;
                    }
                }
            }

            characterController.Move(characterSpeed * Time.deltaTime * moveDirection.normalized);    
        }

        if (jump > 0f && characterController.isGrounded) 
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            transform.SetParent(null);
            activePlatform = null;
        }

        velocity.y += gravity * Time.deltaTime;

        if (velocity.y < -30f)
        {
            velocity.y = -30f;
        }

        characterController.Move(velocity * Time.deltaTime);

        if (characterController.isGrounded && activePlatform != null)
        {
            if (transform.parent != activePlatform)
            {
                transform.SetParent(activePlatform);
            }
        }
        else if (!characterController.isGrounded)
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
        }

        float inputSpeed = new Vector3(horizontal, 0f, vertical).magnitude;
        bool isMoving = inputSpeed >= 0.1f;

        if (footstepFMOD != null)
        {
            bool isGrounded = characterController.isGrounded;
            footstepFMOD.HandleMovement(isMoving, isGrounded, inputSpeed * characterSpeed);
        }

        if (swimFMOD != null && waterBuoyancy != null)
        {
            swimFMOD.HandleSwim(waterBuoyancy.isInWater, isMoving);
        }

        if (diveFMOD != null && waterBuoyancy != null)
        {
            diveFMOD.HandleDive(waterBuoyancy.isInWater);
        }

        if (characterAnimator != null && waterBuoyancy != null)
        {
            bool shouldSwim = waterBuoyancy.isInWater && !characterController.isGrounded;
            characterAnimator.SetBool("isSwimming", shouldSwim);
        }

        activePlatform = null;
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Raft") && hit.normal.y > 0.5f)
        {
            activePlatform = hit.transform.root;
        }
    }
}