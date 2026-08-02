using System.Collections.Generic;
using NightPost.UI;
using UnityEngine;

/// <summary>
/// UISandbox 전용 임시 테스터. 버튼 OnClick에 각 메서드를 연결해 UI 골격을 검증한다.
/// 검증이 끝나면 삭제해도 된다(도메인 로직 아님).
/// </summary>
public class UISandboxTester : MonoBehaviour
{
    [SerializeField] private HUDController _hud;
    [SerializeField] private PlayerDataManager _playerData;   // 실제 재화 테스트용

    // ── 팝업 ────────────────────────────────
    public void ShowConfirmInfo()
    {
        var popup = PopupManager.Instance != null
            ? PopupManager.Instance.Get<ConfirmPopup>(UIScreenId.Confirm)
            : null;
        if (popup == null) { Debug.LogWarning("[Tester] ConfirmPopup 미등록 — Id가 Confirm인지, PopupManager _popupRoot 아래에 있는지 확인"); return; }

        popup.Open(ConfirmModel.Info("테스트 알림입니다.", () => Debug.Log("[Tester] 확인 눌림")));
    }

    public void ShowConfirmYesNo()
    {
        var popup = PopupManager.Instance != null
            ? PopupManager.Instance.Get<ConfirmPopup>(UIScreenId.Confirm)
            : null;
        if (popup == null) { Debug.LogWarning("[Tester] ConfirmPopup 미등록"); return; }

        popup.Open(new ConfirmModel
        {
            Title = "확인",
            Message = "도보 배달부를 고용할까요?",
            ConfirmText = "고용",
            OnConfirm = () => Debug.Log("[Tester] 고용 확정"),
            OnCancel = () => Debug.Log("[Tester] 취소"),
        });
    }

    // ── 편지 겉면 ────────────────────────────
    public void ShowEnvelope()
    {
        var popup = PopupManager.Instance != null
            ? PopupManager.Instance.Get<EnvelopePopupController>(UIScreenId.EnvelopePopup)
            : null;
        if (popup == null) { Debug.LogWarning("[Tester] EnvelopePopup 미등록 — Id가 EnvelopePopup인지, PopupManager _popupRoot 아래에 있는지 확인"); return; }

        popup.Open(new EnvelopeModel
        {
            LetterId = 1001,
            Title = "감이 익었다는 소식",
            SenderName = "김순자",
            RegionLabel = "산간",
            Reward = 30,
            IsUrgent = true,
            IsHeavy = false,
            IsRead = false,
            OnAssign = ShowAssignment,   // 배정하기 → Assignment 팝업 (미니 통합 테스트)
        });
    }

    // ── 배정 (배달부 · 노선 선택) ─────────────
    public void ShowAssignment()
    {
        var popup = PopupManager.Instance != null
            ? PopupManager.Instance.Get<AssignmentPopupController>(UIScreenId.Assignment)
            : null;
        if (popup == null) { Debug.LogWarning("[Tester] AssignmentPopup 미등록 — Id가 Assignment인지, PopupManager _popupRoot 아래에 있는지 확인"); return; }

        popup.Open(new AssignmentModel
        {
            LetterId = 1001,
            LetterTitle = "감이 익었다는 소식",
            RegionLabel = "산간",
            IsUrgent = true,
            Couriers = new List<CourierOption>
            {
                new CourierOption { CourierId = 2001, Name = "느릿 아저씨", VehicleLabel = "도보",   IsAvailable = true },
                new CourierOption { CourierId = 2002, Name = "바퀴 청년",   VehicleLabel = "자전거", IsAvailable = true },
                new CourierOption { CourierId = 2003, Name = "부릉 씨",     VehicleLabel = "오토바이", IsAvailable = false }, // 배달 중
            },
            Routes = new List<RouteOption>
            {
                new RouteOption { RouteId = 3001, Name = "산길 노선", DifficultyLabel = "보통",   IsUnlocked = true },
                new RouteOption { RouteId = 3002, Name = "고개 노선", DifficultyLabel = "어려움", IsUnlocked = false }, // 잠김
            },
            Estimate = (courierId, routeId) =>
            {
                // 가짜 계산: 노선 기본 180초, 자전거(2002)면 절반, 급함이면 다시 절반
                int sec = 180;
                if (courierId == 2002) sec /= 2;
                sec /= 2; // 급함 편지 가정
                return new DeliveryEstimate { Seconds = sec, Reward = 30 };
            },
            OnStartDelivery = (courierId, routeId) =>
            {
                Debug.Log($"[Tester] 배달 시작 → courier={courierId}, route={routeId} (서비스 연결 예정)");
                return true; // 가짜 테스트라 항상 성공 처리
            },
        });
    }

    // ── 토스트 ──────────────────────────────
    public void ShowLockToast()
    {
        if (ToastController.Instance != null) ToastController.Instance.Show(ToastController.Locked);
    }

    public void ShowRegionToast()
    {
        if (ToastController.Instance != null) ToastController.Instance.Show(ToastController.LockedRegion);
    }

    // ── HUD (임시 값 주입) ──────────────────
    public void RefreshHud()
    {
        if (_hud == null) { Debug.LogWarning("[Tester] HUD 참조가 비어 있습니다."); return; }

        _hud.SetCoin(Random.Range(0, 99999));
        _hud.SetStamp(Random.Range(0, 999));
        _hud.SetLevel(Random.Range(1, 30), Random.Range(0, 3600), 3600);
        _hud.SetMission(0, "편지 분류", Random.Range(0, 60), 60);
        _hud.SetMission(1, "우체통 확인", 1, 1);
        _hud.SetMission(2, "배달 준비", Random.Range(0, 15), 15);
        _hud.SetInboxBadge(true);
    }

    // ── HUDPresenter 이벤트 경로 검증(디버그 전용) ──
    // 주의: 원칙상 UI는 GameEvents.Raise* 를 호출하지 않는다.
    // 이건 시스템(GameBootstrap) 없이 HUDPresenter의 구독만 격리 검증하려는 임시 수단이다.
    // 실데이터 검증은 GameBootstrap이 있는 씬에서 한다.
    public void DebugRaiseCurrency()
    {
        GameEvents.RaiseCurrencyChanged(Random.Range(1000, 99999));
    }

    // 실제 세이브 재화를 더한다(데이터 변경 + CurrencyChanged 발생).
    // 시설 업그레이드처럼 GetCurrency()를 실제로 검사하는 기능 테스트에 사용.
    public void DebugAddCurrency()
    {
        if (_playerData == null) { Debug.LogWarning("[Tester] PlayerDataManager 참조가 비어 있습니다."); return; }
        _playerData.AddCurrency(5000);
    }

    // ── 편지 열람 단독 테스트(가짜 본문) ──
    // 수신함/시스템 없이 LetterRead 레이아웃·스크롤만 빠르게 확인하는 용도.
    public void ShowLetterRead()
    {
        var popup = PopupManager.Instance != null
            ? PopupManager.Instance.Get<LetterReadController>(UIScreenId.LetterRead)
            : null;
        if (popup == null) { Debug.LogWarning("[Tester] LetterRead 미등록 — Id가 LetterRead인지, PopupManager _popupRoot 아래에 있는지 확인"); return; }

        popup.Open(new LetterReadModel
        {
            Title = "감이 익었습니다",
            Sender = "김순자",
            Body = "올해도 감이 잘 익었습니다.\n덕분에 편지가 무사히 닿았어요.\n\n바쁘겠지만 몸 상하지 말고\n가끔은 바다도 보러 오세요.\n\n긴 글이 스크롤되는지 확인하려고\n일부러 여러 줄을 넣어 둡니다.\n잘 읽히면 성공입니다.",
        });
    }
}
