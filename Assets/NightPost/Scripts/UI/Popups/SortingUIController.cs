using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 분류(Sorting) UI. 분류대(Station_Sorting) 도착 시 Open()으로 열린다.
    /// New 편지를 골라 지역·긴급도·무게를 각각 단일 선택하고 제출한다.
    /// 정답이면 편지가 Waiting으로 넘어가고, 오답이면 New를 유지한다.
    ///
    /// 통합 명세서 v1.1 부록 C 기준. 규칙:
    ///   - 정답 판정·상태 전환은 GameFlowController→SortingService에서만 수행(UI는 표현만).
    ///   - Open에서 입력 차단·초기 조회·이벤트 구독, Close에서 대칭 정리·입력 복구.
    ///   - enum 미선택은 enum 기본값이 아니라 hasSelected bool 3개로 판정.
    ///
    /// 의존성은 SerializeField로 연결(이 프로젝트의 서비스는 MonoBehaviour라 Inspector 연결 가능).
    /// </summary>
    public class SortingUIController : MonoBehaviour
    {
        [Header("의존성")]
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private LetterService _letterService;
        [SerializeField] private PlayerDataManager _playerData;
        [SerializeField] private PlayerController _playerController;

        [Header("패널")]
        [SerializeField] private GameObject _sortingPanel;   // 켜고 끄는 분류 화면 루트
        [SerializeField] private GameObject _blockRaycast;    // 뒤 클릭 차단(선택)

        [Header("편지 목록")]
        [SerializeField] private Transform _letterListRoot;   // Layout Group 권장
        [SerializeField] private SortingLetterItem _letterItemPrefab;
        [SerializeField] private GameObject _emptyState;      // New 편지 0개일 때

        [Header("편지 상세")]
        [SerializeField] private TMP_Text _senderText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        [Header("선택 하이라이트 (배열 인덱스 = enum 정수값 순서)")]
        [SerializeField] private GameObject[] _regionMarks;   // [None,Town,Mountain,Outskirts]
        [SerializeField] private GameObject[] _urgencyMarks;  // [Normal,Urgent]
        [SerializeField] private GameObject[] _weightMarks;   // [Light,Normal,Heavy]

        [Header("제출/피드백")]
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _regionError;
        [SerializeField] private GameObject _urgencyError;
        [SerializeField] private GameObject _weightError;
        [SerializeField] private GameObject _successView;

        [Header("성공 후 전환")]
        [Tooltip("분류 성공 시 호출. 예: 이 화면 Close → 배달 UI Open")]
        [SerializeField] private UnityEvent _onSortingSucceeded;

        // 런타임 상태
        private int _selectedLetterID = -1;
        private ERegionType _selectedRegion;
        private ELetterUrgency _selectedUrgency;
        private ELetterWeight _selectedWeight;
        private bool _hasSelectedRegion;
        private bool _hasSelectedUrgency;
        private bool _hasSelectedWeight;
        private bool _isOpen;
        private bool _isSubmitting;
        private bool _subscribed;

        private readonly List<SortingLetterItem> _items = new();

        private void Awake()
        {
            if (_submitButton != null)
            {
                _submitButton.onClick.RemoveAllListeners();
                _submitButton.onClick.AddListener(SubmitSorting);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
        }

        // ── 열기 / 닫기 (Station UnityEvent, 닫기 버튼에서 호출) ──
        public void Open()
        {
            if (_isOpen) return; // 중복 Open 차단
            _isOpen = true;
            _isSubmitting = false;

            if (_sortingPanel != null) _sortingPanel.SetActive(true);
            if (_blockRaycast != null) _blockRaycast.SetActive(true);
            if (_playerController != null) _playerController.SetControlEnabled(false);

            Subscribe();
            ResetSortingSelection();
            ClearDetail();
            HideFeedback();
            RefreshLetterList();
        }

        public void Close()
        {
            // _isOpen 여부와 무관하게 항상 닫는다.
            // (에디터에서 패널을 켜둔 채 실행하면 Open()을 거치지 않아 _isOpen이 false인데,
            //  여기서 early return 하면 닫기 버튼이 아무 반응도 없는 것처럼 보인다)
            _isOpen = false;
            _isSubmitting = false;

            ResetTemporaryState();
            Unsubscribe();

            if (_sortingPanel != null) _sortingPanel.SetActive(false);
            if (_blockRaycast != null) _blockRaycast.SetActive(false);
            if (_playerController != null) _playerController.SetControlEnabled(true);
            // 편지 진행 상태는 여기서 바꾸지 않는다.
        }

        // ── 편지 선택 ──
        public void SelectLetter(int letterID)
        {
            if (letterID <= 0 || !_isOpen || _isSubmitting) return;
            if (_flow == null) return;

            if (!_flow.SelectLetter(letterID))
            {
                RefreshLetterList(); // 선택 실패 시 조용히 재조회
                return;
            }

            _selectedLetterID = letterID;
            RefreshSelectedLetterView(letterID); // 상세(제목/발신자/본문) 표시

            ResetSortingSelection(); // 새 편지를 고르면 이전 분류 선택·오답 초기화
            HideFeedback();
            HighlightSelectedItem(letterID);
            UpdateSubmitButtonState();
        }

        // ── 분류 3종 선택 (옵션 버튼 OnClick에서 int 인자로 호출) ──
        public void SelectRegion(int regionValue)
        {
            _selectedRegion = (ERegionType)regionValue;
            _hasSelectedRegion = true;
            SetHighlights(_regionMarks, regionValue);
            if (_regionError != null) _regionError.SetActive(false);
            UpdateSubmitButtonState();
        }

        public void SelectUrgency(int urgencyValue)
        {
            _selectedUrgency = (ELetterUrgency)urgencyValue;
            _hasSelectedUrgency = true;
            SetHighlights(_urgencyMarks, urgencyValue);
            if (_urgencyError != null) _urgencyError.SetActive(false);
            UpdateSubmitButtonState();
        }

        public void SelectWeight(int weightValue)
        {
            _selectedWeight = (ELetterWeight)weightValue;
            _hasSelectedWeight = true;
            SetHighlights(_weightMarks, weightValue);
            if (_weightError != null) _weightError.SetActive(false);
            UpdateSubmitButtonState();
        }

        // ── 제출 ──
        public void SubmitSorting()
        {
            if (!_isOpen || _isSubmitting) return;
            if (_selectedLetterID <= 0) return;
            if (!_hasSelectedRegion || !_hasSelectedUrgency || !_hasSelectedWeight) return;
            if (_flow == null) return;

            _isSubmitting = true; // 첫 클릭 즉시 잠금(중복 제출 차단)
            if (_submitButton != null) _submitButton.interactable = false;

            SortingResultData result = _flow.SubmitSelectedLetterSorting(_selectedRegion, _selectedUrgency, _selectedWeight);

            if (result == null)
            {
                // 요청 실패: 화면 유지, 잠금 해제
                _isSubmitting = false;
                RefreshLetterList();
                UpdateSubmitButtonState();
                return;
            }

            if (!result.IsSuccess)
            {
                // 오답: 편지 New 유지, 틀린 항목만 표시, 다시 제출 가능
                // 실패에는 소리를 내지 않는다(사운드 명세 §5-1).
                ShowIncorrectResult(result);
                _isSubmitting = false;
                UpdateSubmitButtonState();
                return;
            }

            // 정답: 편지 Waiting 전환(시스템에서), 성공 연출 후 다음 화면으로
            HandleSortingSuccess();
        }

        // ── 내부 ──
        private void RefreshLetterList()
        {
            ClearItems();
            if (_letterService == null || _letterItemPrefab == null || _letterListRoot == null) return;

            int count = 0;
            bool selectedStillNew = false;

            foreach (LetterStaticData letter in _letterService.GetAvailableLetters())
            {
                if (letter == null) continue;
                LetterProgressData progress = _playerData != null ? _playerData.GetLetterProgress(letter.LetterID) : null;
                if (progress == null || progress.State != ELetterProgressState.New) continue; // New만 분류 대상

                SortingLetterItem item = Instantiate(_letterItemPrefab, _letterListRoot);
                item.gameObject.SetActive(true);
                item.Setup(letter.LetterID, letter.LetterTitle, letter.SenderName, progress.IsRead, SelectLetter);
                _items.Add(item);
                count++;

                if (letter.LetterID == _selectedLetterID) selectedStillNew = true;
            }

            if (_emptyState != null) _emptyState.SetActive(count == 0);

            // 선택했던 편지가 더 이상 New가 아니면(외부에서 배달됨 등) 선택 초기화
            if (_selectedLetterID > 0 && !selectedStillNew)
            {
                _selectedLetterID = -1;
                ClearDetail();
                ResetSortingSelection();
                HideFeedback();
                UpdateSubmitButtonState();
            }
            else
            {
                HighlightSelectedItem(_selectedLetterID);
            }
        }

        private void RefreshSelectedLetterView(int letterID)
        {
            // 상세는 정적 데이터에서 가져온다(제목/발신자/본문).
            LetterStaticData data = FindStaticFromItems(letterID);
            if (_titleText != null) _titleText.text = data != null ? data.LetterTitle : string.Empty;
            if (_senderText != null) _senderText.text = data != null ? data.SenderName : string.Empty;
            if (_bodyText != null) _bodyText.text = data != null ? data.LetterBody : string.Empty;
        }

        // 목록 아이템이 이미 조회한 정적 데이터를 다시 쓰기 위해 LetterService에서 재조회.
        private LetterStaticData FindStaticFromItems(int letterID)
        {
            if (_letterService == null) return null;
            foreach (LetterStaticData letter in _letterService.GetAvailableLetters())
                if (letter != null && letter.LetterID == letterID) return letter;
            return null;
        }

        private void HighlightSelectedItem(int letterID)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != null) _items[i].SetSelected(_items[i].LetterId == letterID);
        }

        private void UpdateSubmitButtonState()
        {
            if (_submitButton == null) return;
            _submitButton.interactable =
                _isOpen && !_isSubmitting &&
                _selectedLetterID > 0 &&
                _hasSelectedRegion && _hasSelectedUrgency && _hasSelectedWeight;
        }

        private void ShowIncorrectResult(SortingResultData result)
        {
            if (_regionError != null) _regionError.SetActive(!result.IsRegionCorrect);
            if (_urgencyError != null) _urgencyError.SetActive(!result.IsUrgencyCorrect);
            if (_weightError != null) _weightError.SetActive(!result.IsWeightCorrect);
        }

        private void HandleSortingSuccess()
        {
            UISoundPlayer.Play(ESFXType.LetterSortPlace);

            if (_successView != null) _successView.SetActive(true);
            RefreshLetterList();                 // Waiting으로 빠진 편지를 목록에서 제거

            // 화면을 닫지 않고 이어서 다음 편지를 분류할 수 있도록 선택·잠금을 초기화한다.
            // (다음 편지를 클릭하면 성공 표시는 SelectLetter의 HideFeedback으로 사라진다)
            _selectedLetterID = -1;
            ResetSortingSelection();
            ClearDetail();
            _isSubmitting = false;
            UpdateSubmitButtonState();

            _onSortingSucceeded?.Invoke();       // 연결돼 있으면 Close 후 배달 UI Open 등
        }

        private void ResetSortingSelection()
        {
            _hasSelectedRegion = false;
            _hasSelectedUrgency = false;
            _hasSelectedWeight = false;
            SetHighlights(_regionMarks, -1);
            SetHighlights(_urgencyMarks, -1);
            SetHighlights(_weightMarks, -1);
            if (_regionError != null) _regionError.SetActive(false);
            if (_urgencyError != null) _urgencyError.SetActive(false);
            if (_weightError != null) _weightError.SetActive(false);
        }

        private void ResetTemporaryState()
        {
            _selectedLetterID = -1;
            ResetSortingSelection();
            ClearDetail();
            HideFeedback();
            _isSubmitting = false;
        }

        private void ClearDetail()
        {
            if (_titleText != null) _titleText.text = string.Empty;
            if (_senderText != null) _senderText.text = string.Empty;
            if (_bodyText != null) _bodyText.text = string.Empty;
        }

        private void HideFeedback()
        {
            if (_regionError != null) _regionError.SetActive(false);
            if (_urgencyError != null) _urgencyError.SetActive(false);
            if (_weightError != null) _weightError.SetActive(false);
            if (_successView != null) _successView.SetActive(false);
        }

        private static void SetHighlights(GameObject[] marks, int activeIndex)
        {
            if (marks == null) return;
            for (int i = 0; i < marks.Length; i++)
                if (marks[i] != null) marks[i].SetActive(i == activeIndex);
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != null) Destroy(_items[i].gameObject);
            _items.Clear();
        }

        // ── 이벤트 (중복 없는 대칭 구독) ──
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEvents.LetterReceived += OnLetterReceived;
            GameEvents.LetterRead += OnLetterRead;
            GameEvents.LetterStateChanged += OnLetterStateChanged;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.LetterReceived -= OnLetterReceived;
            GameEvents.LetterRead -= OnLetterRead;
            GameEvents.LetterStateChanged -= OnLetterStateChanged;
        }

        private void OnLetterReceived(int letterID) { if (_isOpen) RefreshLetterList(); }
        private void OnLetterRead(int letterID) { if (_isOpen) RefreshLetterList(); }
        private void OnLetterStateChanged(int letterID, ELetterProgressState state) { if (_isOpen) RefreshLetterList(); }
    }
}
