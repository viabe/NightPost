using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    private bool isControlEnabled = true;

    // 플레이어 애니메이션을 제어하는 Animator임
    [SerializeField] private Animator animator;

    // 이동 방향에 따라 스프라이트를 반전하는 Renderer임
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 원본 이미지가 오른쪽을 바라보는지 나타냄
    [SerializeField] private bool defaultFacesRight = true;

    // Animator의 IsMoving 파라미터 해시값임
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    /// <summary>
    /// 애니메이션 처리에 필요한 컴포넌트를 가져옴
    /// </summary>
    private void Awake()
    {
        // 같은 GameObject의 Animator를 가져옴
        animator = GetComponent<Animator>();

        // 같은 GameObject의 SpriteRenderer를 가져옴
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 직접 연결되지 않았다면 같은 오브젝트에서 가져옴
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }
    /// <summary>
    /// 플레이어의 이동 상태와 방향을 애니메이션에 반영함
    /// </summary>
    private void Update()
    {
        // 이동 컴포넌트가 없다면 애니메이션을 갱신하지 않음
        if (playerMovement == null) return;

        // 현재 실제 이동 여부를 Animator에 전달함
        animator.SetBool(IsMovingHash, playerMovement.IsMoving);

        // 이동 방향이 없다면 현재 바라보는 방향을 유지함
        if (Mathf.Approximately(playerMovement.MoveDirection, 0f)) return;

        // 원본 스프라이트 방향을 기준으로 좌우 반전 여부를 결정함
        bool isMovingLeft = playerMovement.MoveDirection < 0f;
        spriteRenderer.flipX = defaultFacesRight ? isMovingLeft : !isMovingLeft;
    }
    /// <summary>
    /// 전달받은 방향으로 플레이어의 수동 이동을 요청함
    /// </summary>
    public void RequestManualMove(float direction)
    {
        // 현재 플레이어 조작이 비활성화된 상태라면 수동 이동 요청을 처리하지 않고 종료함
        if (!isControlEnabled) return;
        // 전달받은 이동 방향을 PlayerMovement에 전달함
        playerMovement.SetManualMoveInput(direction);
    }

    /// <summary>
    /// 현재 남아 있는 플레이어의 수동 이동 입력을 제거함
    /// </summary>
    public void StopManualMove()
    {
        // 조작 가능 여부와 관계없이 수동 이동 입력을 0으로 변경함
        playerMovement.SetManualMoveInput(0);
    }

    /// <summary>
    /// 플레이어의 조작 가능 상태를 변경함
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        // 전달받은 값을 현재 플레이어 조작 가능 상태로 저장함
        isControlEnabled = enabled;

        // 조작을 활성화한 경우 이후 이동 요청을 허용하고 종료함
        if (isControlEnabled) return;

        // 조작을 비활성화한 경우 현재 진행 중인 수동 이동과 자동 이동을 모두 중단함
        playerMovement.StopAllMovement();

    }

    /// <summary>
    /// 전달받은 목표 X좌표로 플레이어의 자동 이동을 요청함
    /// </summary>
    public void RequestAutoMove(float targetX)
    {
        // 현재 플레이어 조작이 비활성화된 상태라면 자동 이동 요청을 처리하지 않고 종료함
        if (!isControlEnabled) return;
        // 전달받은 목표 X좌표를 PlayerMovement에 전달하여 자동 이동을 시작함
        playerMovement.MoveTo(targetX);
    }
}
