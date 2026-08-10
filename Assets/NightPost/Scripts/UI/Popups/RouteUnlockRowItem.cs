using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 아침 보고의 "해금 가능한 노선" 한 줄.
    /// 조건을 충족한 노선만 여기 표시되므로, 버튼을 누르면 바로 해금된다.
    /// </summary>
    public class RouteUnlockRowItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;      // 노선 이름
        [SerializeField] private TMP_Text _regionText;    // 담당 지역
        [SerializeField] private Button _unlockButton;

        private int _routeId;
        private Action<int> _onUnlock;

        public int RouteId => _routeId;

        public void Setup(int routeId, string name, string region, Action<int> onUnlock)
        {
            _routeId = routeId;
            _onUnlock = onUnlock;

            if (_nameText != null) _nameText.text = name;
            if (_regionText != null) _regionText.text = region;

            if (_unlockButton != null)
            {
                _unlockButton.onClick.RemoveAllListeners();
                _unlockButton.onClick.AddListener(Raise);
            }
        }

        private void Raise() => _onUnlock?.Invoke(_routeId);
    }
}
