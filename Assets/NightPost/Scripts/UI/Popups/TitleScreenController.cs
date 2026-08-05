using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 시작 화면. 게임 씬(PostOffice)과 분리된 별도 씬에서 동작한다.
    ///
    /// 게임 씬에는 GameBootstrap이 있어 Awake에서 세이브 로드·해금·오프라인 정산까지
    /// 자동으로 끝낸다. 즉 "새 게임/이어하기"를 물어볼 지점이 게임 씬 안에는 없다.
    /// 그래서 선택은 이 화면에서 먼저 받고, 그 결과에 맞춰 게임 씬을 연다.
    ///   - 이어하기 → 그대로 게임 씬 로드(GameBootstrap이 기존 세이브를 불러온다)
    ///   - 새 게임  → 세이브 파일을 지운 뒤 게임 씬 로드(GameBootstrap이 신규로 시작한다)
    ///
    /// 주의: 세이브 삭제 API가 없어 파일을 직접 지운다.
    ///       SaveService에 DeleteSaveData()가 생기면 그 호출로 교체할 것.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        // SaveService와 같은 값이어야 한다(SaveService.DatabaseFileName).
        private const string SaveFileName = "NightPostSave.db";

        [Header("이동할 게임 씬")]
        [Tooltip("Build Settings에 등록된 씬 이름")]
        [SerializeField] private string _gameSceneName = "PostOffice";

        [Header("버튼")]
        [SerializeField] private Button _continueButton;  // 이어하기 (세이브 있을 때만 활성)
        [SerializeField] private Button _newGameButton;   // 새 게임
        [SerializeField] private Button _quitButton;      // 종료 (선택)

        [Header("새 게임 확인")]
        [Tooltip("기존 기록을 지우기 전에 띄우는 확인 창. 기본 비활성.")]
        [SerializeField] private GameObject _confirmPanel;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("표시")]
        [SerializeField] private GameObject _noSaveNotice; // 세이브 없을 때 안내 (선택)
        [SerializeField] private TMP_Text _versionText;    // 선택

        private bool _isLoading; // 중복 클릭으로 씬을 두 번 여는 것을 막는다

        private void Awake()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
                _continueButton.onClick.AddListener(ContinueGame);
            }
            if (_newGameButton != null)
            {
                _newGameButton.onClick.RemoveAllListeners();
                _newGameButton.onClick.AddListener(RequestNewGame);
            }
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(QuitGame);
            }
            if (_confirmYesButton != null)
            {
                _confirmYesButton.onClick.RemoveAllListeners();
                _confirmYesButton.onClick.AddListener(StartNewGame);
            }
            if (_confirmNoButton != null)
            {
                _confirmNoButton.onClick.RemoveAllListeners();
                _confirmNoButton.onClick.AddListener(HideConfirm);
            }
        }

        private void Start()
        {
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
            if (_versionText != null) _versionText.text = $"v{Application.version}";

            RefreshSaveState();
        }

        /// <summary>세이브 유무에 따라 이어하기 버튼을 켜고 끈다.</summary>
        private void RefreshSaveState()
        {
            bool hasSave = HasSaveFile();
            if (_continueButton != null) _continueButton.interactable = hasSave;
            if (_noSaveNotice != null) _noSaveNotice.SetActive(!hasSave);
        }

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private static bool HasSaveFile() => File.Exists(SaveFilePath);

        // ── 이어하기 ──
        public void ContinueGame()
        {
            if (_isLoading) return;
            if (!HasSaveFile())
            {
                // 버튼이 비활성이라 보통은 오지 않지만, 파일이 중간에 사라진 경우를 대비한다.
                RefreshSaveState();
                ToastController.Instance?.Show("이어할 기록이 없어요.");
                return;
            }
            LoadGameScene();
        }

        // ── 새 게임 ──
        /// <summary>기존 기록이 있으면 확인부터 받는다.</summary>
        public void RequestNewGame()
        {
            if (_isLoading) return;

            if (!HasSaveFile())
            {
                LoadGameScene(); // 지울 게 없으면 바로 시작
                return;
            }

            if (_confirmPanel != null)
            {
                _confirmPanel.SetActive(true);
                return;
            }

            // 확인 창이 없으면 기록을 지우지 않는다(실수로 날리는 것을 막는다).
            Debug.LogError("[Title] 새 게임 확인 창이 연결되지 않아 진행하지 않는다.", this);
        }

        private void HideConfirm()
        {
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
        }

        /// <summary>기존 세이브를 지우고 새로 시작한다.</summary>
        public void StartNewGame()
        {
            if (_isLoading) return;
            HideConfirm();

            if (!DeleteSaveFile())
            {
                ToastController.Instance?.Show("기록을 지우지 못했어요.");
                RefreshSaveState();
                return;
            }

            LoadGameScene();
        }

        // TODO(교체): SaveService에 DeleteSaveData()가 추가되면 그 호출로 바꾼다.
        private bool DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
                return true;
            }
            catch (IOException e)
            {
                // 파일이 잠겨 있는 경우(다른 프로세스가 DB를 열고 있음 등)
                Debug.LogError($"[Title] 세이브 삭제 실패: {e.Message}");
                return false;
            }
        }

        // ── 공통 ──
        private void LoadGameScene()
        {
            if (string.IsNullOrEmpty(_gameSceneName))
            {
                Debug.LogError("[Title] 이동할 씬 이름이 비어 있다.", this);
                return;
            }

            _isLoading = true;
            SceneManager.LoadScene(_gameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
