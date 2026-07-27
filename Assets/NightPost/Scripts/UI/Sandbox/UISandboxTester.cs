using NightPost.UI;
using UnityEngine;

/// <summary>
/// UISandbox 전용 임시 테스터. 버튼 OnClick에 각 메서드를 연결해 UI 골격을 검증한다.
/// 검증이 끝나면 삭제해도 된다(도메인 로직 아님).
/// </summary>
public class UISandboxTester : MonoBehaviour
{
    [SerializeField] private HUDController _hud;

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
}
