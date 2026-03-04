using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputSystem_Actions InputActions { get; private set; }

    // 외부(GridSystem 등)에서 실시간으로 가져다 쓸 입력값들
    public Vector2 Move { get; private set; }
    public float Scroll { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        InputActions.Enable();

        // Move 액션 구독 (입력될 때와 손을 뗄 때 모두 갱신)
        InputActions.Player.Move.performed += OnMove;
        InputActions.Player.Move.canceled += OnMove;

        // Scroll 액션 구독 (마우스 휠)
        InputActions.Player.Scroll.performed += OnScroll;
        InputActions.Player.Scroll.canceled += OnScroll;
    }

    private void OnDisable()
    {
        InputActions.Disable();

        InputActions.Player.Move.performed -= OnMove;
        InputActions.Player.Move.canceled -= OnMove;

        InputActions.Player.Scroll.performed -= OnScroll;
        InputActions.Player.Scroll.canceled -= OnScroll;
    }

    // CallbackContext를 통해 실시간으로 변수 갱신
    private void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        // Vector2로 설정된 마우스 스크롤의 Y축 값만 추출
        Vector2 scrollValue = context.ReadValue<Vector2>();
        Scroll = scrollValue.y;
    }
}