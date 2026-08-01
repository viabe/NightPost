using UnityEngine;
using UnityEngine.EventSystems;

namespace NightPost.UI
{
    /// <summary>
    /// 잠긴 UI 요소(하단 메뉴, 미해금 지역/노선, 미구현 버튼 등)에 붙여
    /// 탭하면 안내 토스트를 띄운다. "완성형 UI + MVP 동작"(F15) 전략의 잠금 처리 도구:
    /// 껍데기는 그려두고, 아직 동작하지 않는 요소는 안내로 막는다.
    ///
    /// 사용법 (둘 중 하나):
    ///   1) 잠긴 요소가 그냥 이미지 → 그 Image에 Raycast Target을 켜고 이 컴포넌트를 붙인다.
    ///      (버튼일 필요 없음. IPointerClickHandler로 탭을 직접 받는다.)
    ///   2) 잠긴 요소가 이미 Button → Button.onClick에 ShowNotice() 를 연결한다.
    ///
    /// 원칙(사운드 명세): 잠금은 벌이 아니므로 담백한 안내만. 경고음/부정 표현 없음.
    /// </summary>
    public class LockedTapNotice : MonoBehaviour, IPointerClickHandler
    {
        public enum Preset
        {
            Locked,       // "준비 중입니다. 곧 만나요."
            LockedRegion, // "이 지역은 아직 길이 열리지 않았어요."
            Custom,       // 아래 _customMessage 사용
        }

        [SerializeField] private Preset _preset = Preset.Locked;
        [SerializeField, TextArea] private string _customMessage = "";

        public void OnPointerClick(PointerEventData eventData) => ShowNotice();

        /// <summary>안내 토스트 표시. Button.onClick 에서도 호출 가능(public void).</summary>
        public void ShowNotice()
        {
            if (ToastController.Instance == null)
            {
                Debug.LogWarning("[LockedTapNotice] ToastController가 씬에 없습니다.");
                return;
            }
            ToastController.Instance.Show(Message);
        }

        private string Message
        {
            get
            {
                switch (_preset)
                {
                    case Preset.LockedRegion:
                        return ToastController.LockedRegion;
                    case Preset.Custom:
                        return string.IsNullOrEmpty(_customMessage) ? ToastController.Locked : _customMessage;
                    default:
                        return ToastController.Locked;
                }
            }
        }
    }
}
