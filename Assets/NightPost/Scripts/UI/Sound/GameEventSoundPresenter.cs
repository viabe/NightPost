using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 게임 이벤트에 붙는 효과음을 한곳에서 처리한다.
    /// 화면을 보고 있지 않아도 울려야 하는 소리(편지 도착·배달 완료·해금)라
    /// 각 UI 컨트롤러가 아니라 상시 활성 오브젝트가 구독한다.
    ///
    /// 버튼 클릭음은 UISoundButton(인스펙터), 동작 성공음은 각 컨트롤러가 담당한다.
    /// 여기서 다루는 건 "시스템이 알려주는 사건"뿐이다.
    ///
    /// 주의: CurrencyChanged와 DeliveryResultChecked는 결과를 여러 건 수령할 때
    /// 연달아 발생하므로 여기서 다루지 않는다. 보상음은 수령 버튼 쪽에서 한 번만 울린다.
    /// </summary>
    public class GameEventSoundPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.LetterReceived += OnLetterReceived;
            GameEvents.ReplyReceived += OnReplyReceived;
            GameEvents.DeliveryCompleted += OnDeliveryCompleted;
            GameEvents.FacilityUpgraded += OnFacilityUpgraded;
            GameEvents.RouteUnlocked += OnRouteUnlocked;
            GameEvents.CourierUnlocked += OnCourierUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.LetterReceived -= OnLetterReceived;
            GameEvents.ReplyReceived -= OnReplyReceived;
            GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
            GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
            GameEvents.RouteUnlocked -= OnRouteUnlocked;
            GameEvents.CourierUnlocked -= OnCourierUnlocked;
        }

        // 편지가 여러 통 한꺼번에 들어와도 UISoundPlayer의 쿨다운이 한 번으로 묶는다.
        private void OnLetterReceived(int letterID) => UISoundPlayer.Play(ESFXType.MessengerBirdArrive);

        private void OnReplyReceived(int replyID) => UISoundPlayer.PlayAccent(ESFXType.ReplyArrive);

        private void OnDeliveryCompleted(int letterID) => UISoundPlayer.Play(ESFXType.DeliveryComplete);

        private void OnFacilityUpgraded(int facilityID, int currentLevel) => UISoundPlayer.PlayAccent(ESFXType.FacilityUpgrade);

        private void OnRouteUnlocked(int routeID) => UISoundPlayer.PlayAccent(ESFXType.ContentUnlock);

        private void OnCourierUnlocked(int courierID) => UISoundPlayer.PlayAccent(ESFXType.ContentUnlock);
    }
}
