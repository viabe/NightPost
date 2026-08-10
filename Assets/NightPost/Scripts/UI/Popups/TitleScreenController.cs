using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 시작 화면. 게임 씬(PostOffice)과 분리된 씬에서 동작한다.
    ///
    /// 모바일 방치형 관례에 맞춰 진입구를 하나만 둔다.
    /// 세이브 슬롯이 하나뿐이라 "새 게임 / 이어하기"를 물을 이유가 없다.
    /// 기록이 있으면 이어지고, 없으면 새로 시작된다 — 판단은 게임 씬의
    /// GameBootstrap이 알아서 하므로 이 화면은 씬을 여는 일만 한다.
    ///
    /// 데이터 초기화가 필요하면 게임 안 설정 화면에서 다루는 편이 낫다
    /// (SaveService에 삭제 API가 추가된 뒤).
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Header("이동할 게임 씬")]
        [Tooltip("Build Settings에 등록된 씬 이름")]
        [SerializeField] private string _gameSceneName = "PostOffice";

        [Header("진입")]
        [Tooltip("화면 전체를 덮는 투명 버튼을 쓰면 '아무 곳이나 탭'이 된다.")]
        [SerializeField] private Button _startButton;
        [Tooltip("'터치하여 시작' 같은 안내 문구. 지정하면 부드럽게 점멸한다.")]
        [SerializeField] private TMP_Text _startHintText;
        [SerializeField] private float _blinkCycleSeconds = 1.6f;

        [Header("보조")]
        [SerializeField] private Button _settingsButton;      // 선택
        [Tooltip("설정 버튼 동작. 설정 화면이 준비되기 전엔 잠금 안내로 연결해도 된다.")]
        [SerializeField] private UnityEvent _onSettingsClicked;
        [SerializeField] private TMP_Text _versionText;       // 선택

        private bool _isLoading; // 연타로 씬을 두 번 여는 것을 막는다

        private void Awake()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(StartGame);
            }
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(OpenSettings);
            }
        }

        private void Start()
        {
            if (_versionText != null) _versionText.text = $"v{Application.version}";
        }

        private void Update()
        {
            if (_startHintText == null || _blinkCycleSeconds <= 0f) return;

            // 0.35~1.0 사이를 왕복하며 문구를 점멸시킨다(완전히 사라지지 않게).
            float t = Mathf.PingPong(Time.unscaledTime / _blinkCycleSeconds, 1f);
            Color c = _startHintText.color;
            c.a = Mathf.Lerp(0.35f, 1f, t);
            _startHintText.color = c;
        }

        /// <summary>게임 씬으로 진입. 세이브가 있으면 이어지고 없으면 새로 시작된다.</summary>
        public void StartGame()
        {
            if (_isLoading) return;

            if (string.IsNullOrEmpty(_gameSceneName))
            {
                Debug.LogError("[Title] 이동할 씬 이름이 비어 있다.", this);
                return;
            }

            _isLoading = true;
            SceneManager.LoadScene(_gameSceneName);
        }

        private void OpenSettings() => _onSettingsClicked?.Invoke();
    }
}
