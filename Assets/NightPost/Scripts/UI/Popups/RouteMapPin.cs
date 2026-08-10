using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>노선 핀의 표시 상태. 점 이미지를 이 값에 맞춰 바꾼다.</summary>
    public enum ERoutePinState
    {
        Locked,     // 진행도 조건 미달
        Unlockable, // 조건을 채워 지금 열 수 있음
        Unlocked,   // 이미 열린 노선
    }

    /// <summary>
    /// 노선 지도 위의 핀 하나. 노선이 몇 개 없으므로 목록처럼 복제하지 않고
    /// 지도 이미지 위에 직접 배치한 뒤 담당 노선 ID만 인스펙터에서 지정한다.
    ///
    /// 표시만 담당한다. 어떤 문구를 띄울지와 해금 요청은 RouteMapPanel이 정한다.
    /// </summary>
    public class RouteMapPin : MonoBehaviour
    {
        [Tooltip("이 핀이 담당하는 노선 ID (Routes.csv의 routeID)")]
        [SerializeField] private int _routeId;

        [Header("표시")]
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _nameText;   // 지역명. 예: "외곽"
        [SerializeField] private TMP_Text _subText;    // 노선 이름 / 해금 안내 / 남은 조건
        [Tooltip("선택. 연결하면 지금 고른 핀에 강조가 켜진다.")]
        [SerializeField] private GameObject _selectedMark;

        [Header("상태 이미지")]
        [Tooltip("상태에 따라 그림이 바뀌는 핀 점")]
        [SerializeField] private Image _dotImage;
        [Tooltip("진행도 조건을 아직 못 채운 상태")]
        [SerializeField] private Sprite _lockedSprite;
        [Tooltip("지금 열 수 있는 상태")]
        [SerializeField] private Sprite _unlockableSprite;
        [Tooltip("이미 열린 상태")]
        [SerializeField] private Sprite _unlockedSprite;

        private Action<int> _onClick;

        public int RouteId => _routeId;

        /// <summary>클릭 콜백을 건다. 패널이 시작할 때 한 번만 부른다.</summary>
        public void Bind(Action<int> onClick)
        {
            _onClick = onClick;

            // 루트 Button만 자동 보정한다(자식 Button은 그 영역에서만 눌림).
            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(Raise);
        }

        /// <summary>상태에 맞는 점 그림과 두 줄 문구를 반영한다.</summary>
        public void Apply(ERoutePinState state, string name, string sub)
        {
            if (_nameText != null) _nameText.text = name;
            if (_subText != null) _subText.text = sub;
            ApplyDotSprite(state);
        }

        /// <summary>상태별 점 그림으로 갈아끼운다. 해당 상태 그림이 없으면 지금 것을 그대로 둔다.</summary>
        private void ApplyDotSprite(ERoutePinState state)
        {
            if (_dotImage == null) return;

            Sprite sprite;
            switch (state)
            {
                case ERoutePinState.Unlockable: sprite = _unlockableSprite; break;
                case ERoutePinState.Unlocked: sprite = _unlockedSprite; break;
                default: sprite = _lockedSprite; break;
            }

            if (sprite == null) return;
            _dotImage.sprite = sprite;
        }

        public void SetSelected(bool on)
        {
            if (_selectedMark != null) _selectedMark.SetActive(on);
        }

        private void Raise() => _onClick?.Invoke(_routeId);
    }
}
