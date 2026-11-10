using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("ResourceRef")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Animator animator ; 

    [Header("Movement")]
    [SerializeField] private float MoveSpeed = 3.5f; // 기본 스피드 입니다.
    [SerializeField] private float gravity = -20f; // 기본 중력입니다.

    [Header("Look")]
    [SerializeField] private float MouseSensitivity = 200f; // 기본 마우스 감도입니다.
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f; // 카메라가 너무 꺾이지 않도록 제한 하는 변수입니다.


    private CharacterController controller; 
    private Vector3 verticalVelocity; // 계단 내려갈 때 땅에 붙어있을 수 있도록 중력을 사용합니다.
    private float pitch; // 카메라의 상하 회전 각도 입니다.

    private float moveSpeedMultiplier = 1.0f; // 만약에 이동속도를 조정하고 싶다면 SetMoveMultiplier 함수를 통해 변경 가능합니다
    private float lookSensitivityMultiplier = 1f; // 만약에 마우스 감도에 영향을 주고 싶다면 SetLookSensitiveMultiplier 함수를 통해 변경 가능합니다.

    private bool canMove = true;
    private bool canLook = true;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed"); // "MoveSpeed" 문자열 대신 MoveSpeedHash를 입력하면 됩니다.

    public float CurrentMoveSpeed => MoveSpeed * moveSpeedMultiplier; // 최종 속도
    public float CurrentLookSensitivity => MouseSensitivity * lookSensitivityMultiplier; // 최종 마우스 감도
    public bool CanMove => canMove; 
    public bool CanLook => canLook; // 카메라 고정, 움직임 고정을 사용하고 싶다면 SetCanMove, SetCanLook 함수를 호출하여 변경 할 수 있습니다.


    private void Awake()
    {
        controller = GetComponent<CharacterController>(); //CharacterController 를 가져옵니다.

        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform; //만약 camera가 없다면 MainCamera를 연결합니다.
        }
    }

    private void Start()
    {
        LockCursor(); //디버깅시 Inspector 창을 조절하기 쉽게 하려고 입력 막아둠.
    }
    // Update is called once per frame
    void Update()
    {
        HandleCursorToggle(); // ESC 누르면 마우스 Lock을 해제
        HandleLook();
        HandleMovement();
        ApplyGravity();
        UpdateAnimator();
    }

    private void HandleCursorToggle()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState= CursorLockMode.Locked;
            Cursor.visible= false;
        }
    }
    private void LockCursor() // 커서를 화면 중앙에 고정하고, 안보이게 합니다.
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void HandleLook() // 시점 회전 함수입니다.
    {
        if (!canLook) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxisRaw("Mouse X")*CurrentLookSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y")*CurrentLookSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if(cameraPivot!=null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

    }

    private void HandleMovement() //이동함수입니다.
    {
        if (!canMove) return;
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            controller.Move(moveDirection * CurrentMoveSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity() // 플레이어에게 중력을 부여하여 바닥에 붙어있게 하고, 만약 바닥이 꺼지는 기믹이 나온다면 이를 이용합니다.
    {
        if(controller.isGrounded && verticalVelocity.y<0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void UpdateAnimator() // 애니메이션 파라미터를 갱신합니다.
    {
        if (animator == null) return;
        float moveAmount = 0f;

        if(canMove)
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            moveAmount = Mathf.Clamp01(new Vector2(inputX, inputZ).magnitude);
        }

        animator.SetFloat(MoveSpeedHash, moveAmount);
    }


    /////////////////////////////////////////
    ///아래 함수들은 외부 제어 함수입니다.///
    ////////////////////////////////////////
    public void SetMoveMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }
    public void SetLookSensitivityMultiplier(float multiplier)
    {
        lookSensitivityMultiplier = Mathf.Max(0f, multiplier);
    }
    public void SetCanMove(bool value)
    {
        canMove = value;
    }
    public void SetCanLook(bool value)
    {
        canLook = value;
    }

    public void ResetMovementSettings()
    {
        moveSpeedMultiplier = 1f;
        lookSensitivityMultiplier = 1f;
        canMove = true;
        canLook = true;
    }
}
