using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 도착 편지 UI. 도착 우편함(Station_ArrivalMail) 도착 시 Open()으로 열린다.
    /// 우체국이 보관 중인 편지를 상태별로 보여주는 "현황 화면"이다.
    ///   - 분류 전(New)   → 분류대로 이동
    ///   - 배달 대기(Waiting) → 배달대로 이동
    /// 실제 분류·배정은 각 화면에서 하고, 여기서는 조회와 이동만 담당한다.
    ///
    /// 사용 API(LetterService):
    ///   GetUnsortedLetters() / GetWaitingLetters() / OpenLetter(id)
    ///   GetLetterProgress(id) / GetCurrentLetterCount() / GetMaxLetterCapacity()
    /// 이동은 UnityEvent로 연결한다(다른 컨트롤러를 직접 참조하지 않아 결합을 낮춘다).
    /// </summary>
    public class ArrivalMailUIController : MonoBehaviour
    {
        [Header("의존성")]
        [SerializeField] private LetterService _letterService;
        [SerializeField] private PlayerController _playerController;

        [Header("패널")]
        [SerializeField] private GameObject _panel;

        [Header("보관 현황")]
        [SerializeField] private TMP_Text _capacityText;      // "편지 4 / 10"

        [Header("분류 전 (New)")]
        [SerializeField] private Transform _newListRoot;
        [SerializeField] private SortingLetterItem _newItemPrefab;
        [SerializeField] private GameObject _newEmpty;

        [Header("배달 대기 (Waiting)")]
        [SerializeField] private Transform _waitingListRoot;
        [SerializeField] private SortingLetterItem _waitingItemPrefab;
        [SerializeField] private GameObject _waitingEmpty;

        [Header("상세")]
        [SerializeField] private GameObject _detailRoot;      // 편지 선택 시 켜는 영역
        [SerializeField] private GameObject _detailPlaceholder; // 선택 전 안내
        [SerializeField] private TMP_Text _detailTitle;
        [SerializeField] private TMP_Text _detailSender;
        [SerializeField] private TMP_Text _detailInfo;        // "마을 · 보통 · 급함"
        [SerializeField] private TMP_Text _detailReward;
        [SerializeField] private TMP_Text _detailBody;
        [SerializeField] private TMP_Text _detailState;       // "분류 전" / "배달 대기"

        [Header("이동")]
        [SerializeField] private Button _goSortingButton;     // New 편지 선택 시 활성
        [SerializeField] private Button _goDeliveryButton;    // Waiting 편지 선택 시 활성
        [Tooltip("분류대로 이동. 예: 이 화면 Close → SortingUIController.Open()")]
        [SerializeField] private UnityEvent _onGoSorting;
        [Tooltip("배달대로 이동. 예: 이 화면 Close → DeliveryUIController.Open()")]
        [SerializeField] private UnityEvent _onGoDelivery;

        [SerializeField] private Button _closeButton;

        private int _selectedLetterID = -1;
        private ELetterProgressState _selectedState = ELetterProgressState.New;
        private bool _isOpen;
        private bool _subscribed;

        private readonly List<SortingLetterItem> _newItems = new();
        private readonly List<SortingLetterItem> _waitingItems = new();

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
            if (_goSortingButton != null)
            {
                _goSortingButton.onClick.RemoveAllListeners();
                _goSortingButton.onClick.AddListener(GoSorting);
            }
            if (_goDeliveryButton != null)
            {
                _goDeliveryButton.onClick.RemoveAllListeners();
                _goDeliveryButton.onClick.AddListener(GoDelivery);
            }
        }

        // ── 열기 / 닫기 ──
        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (_panel != null) _panel.SetActive(true);
            if (_playerController != null) _playerController.SetControlEnabled(false);

            if (_letterService == null) Debug.LogError("[ArrivalMail] LetterService 미연결", this);

            Subscribe();
            ClearSelection();
            RefreshAll();
        }

        public void Close()
        {
            // _isOpen 여부와 무관하게 항상 닫는다(에디터에서 패널을 켜둔 채 실행한 경우 대비).
            _isOpen = false;

            ClearSelection();
            Unsubscribe();

            if (_panel != null) _panel.SetActive(false);
            if (_playerController != null) _playerController.SetControlEnabled(true);
        }

        // ── 갱신 ──
        private void RefreshAll()
        {
            RefreshCapacity();
            RefreshNewList();
            RefreshWaitingList();
        }

        private void RefreshCapacity()
        {
            if (_capacityText == null || _letterService == null) return;
            _capacityText.text = $"편지 {_letterService.GetCurrentLetterCount()} / {_letterService.GetMaxLetterCapacity()}";
        }

        private void RefreshNewList()
        {
            ClearList(_newItems);
            if (_letterService == null || _newItemPrefab == null || _newListRoot == null) return;

            int count = 0;
            foreach (LetterStaticData letter in _letterService.GetUnsortedLetters())
            {
                if (letter == null) continue;
                LetterProgressData progress = _letterService.GetLetterProgress(letter.LetterID);
                bool isRead = progress != null && progress.IsRead;

                SortingLetterItem item = Instantiate(_newItemPrefab, _newListRoot);
                item.gameObject.SetActive(true);
                item.Setup(letter.LetterID, letter.LetterTitle, letter.SenderName, isRead, OnNewLetterSelected);
                _newItems.Add(item);
                count++;
            }

            if (_newEmpty != null) _newEmpty.SetActive(count == 0);
            HighlightLists();
        }

        private void RefreshWaitingList()
        {
            ClearList(_waitingItems);
            if (_letterService == null || _waitingItemPrefab == null || _waitingListRoot == null) return;

            int count = 0;
            foreach (LetterStaticData letter in _letterService.GetWaitingLetters())
            {
                if (letter == null) continue;

                SortingLetterItem item = Instantiate(_waitingItemPrefab, _waitingListRoot);
                item.gameObject.SetActive(true);
                // 분류를 마친 편지라 이미 읽은 상태로 표시한다(새 표시 없음).
                item.Setup(letter.LetterID, letter.LetterTitle, letter.SenderName, true, OnWaitingLetterSelected);
                _waitingItems.Add(item);
                count++;
            }

            if (_waitingEmpty != null) _waitingEmpty.SetActive(count == 0);
            HighlightLists();
        }

        // ── 선택 ──
        private void OnNewLetterSelected(int letterID) => SelectLetter(letterID, ELetterProgressState.New);
        private void OnWaitingLetterSelected(int letterID) => SelectLetter(letterID, ELetterProgressState.Waiting);

        private void SelectLetter(int letterID, ELetterProgressState state)
        {
            if (!_isOpen || _letterService == null) return;

            // OpenLetter는 상세 데이터를 돌려주면서 읽음 처리까지 수행한다.
            LetterStaticData data = _letterService.OpenLetter(letterID);
            if (data == null)
            {
                Debug.LogWarning($"[ArrivalMail] 편지 상세 조회 실패 letterID={letterID}");
                RefreshAll();
                return;
            }

            _selectedLetterID = letterID;
            _selectedState = state;

            ShowDetail(data, state);
            HighlightLists();
            UpdateGoButtons();
        }

        private void ShowDetail(LetterStaticData data, ELetterProgressState state)
        {
            if (_detailRoot != null) _detailRoot.SetActive(true);
            if (_detailPlaceholder != null) _detailPlaceholder.SetActive(false);

            if (_detailTitle != null) _detailTitle.text = data.LetterTitle;
            if (_detailSender != null) _detailSender.text = data.SenderName;
            if (_detailBody != null) _detailBody.text = data.LetterBody;
            if (_detailReward != null) _detailReward.text = data.LetterReward.ToString("N0");
            if (_detailState != null) _detailState.text = UILabels.LetterState(state);

            if (_detailInfo != null)
            {
                string info = $"{UILabels.Region(data.DestinationRegion)} · {UILabels.Weight(data.Weight)}";
                string urgent = UILabels.Urgency(data.Urgency);
                if (!string.IsNullOrEmpty(urgent)) info += $" · {urgent}";
                _detailInfo.text = info;
            }
        }

        private void ClearSelection()
        {
            _selectedLetterID = -1;

            if (_detailRoot != null) _detailRoot.SetActive(false);
            if (_detailPlaceholder != null) _detailPlaceholder.SetActive(true);

            HighlightLists();
            UpdateGoButtons();
        }

        private void HighlightLists()
        {
            foreach (var it in _newItems)
                if (it != null) it.SetSelected(it.LetterId == _selectedLetterID);
            foreach (var it in _waitingItems)
                if (it != null) it.SetSelected(it.LetterId == _selectedLetterID);
        }

        private void UpdateGoButtons()
        {
            bool hasSelection = _selectedLetterID > 0;
            if (_goSortingButton != null)
                _goSortingButton.interactable = hasSelection && _selectedState == ELetterProgressState.New;
            if (_goDeliveryButton != null)
                _goDeliveryButton.interactable = hasSelection && _selectedState == ELetterProgressState.Waiting;
        }

        // ── 이동 ──
        private void GoSorting()
        {
            Close();
            _onGoSorting?.Invoke();
        }

        private void GoDelivery()
        {
            Close();
            _onGoDelivery?.Invoke();
        }

        private void ClearList(List<SortingLetterItem> items)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null) Destroy(items[i].gameObject);
            items.Clear();
        }

        // ── 이벤트 ──
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

        private void OnLetterReceived(int letterID) { if (_isOpen) RefreshAll(); }
        private void OnLetterRead(int letterID) { if (_isOpen) { RefreshNewList(); RefreshWaitingList(); } }

        private void OnLetterStateChanged(int letterID, ELetterProgressState state)
        {
            if (!_isOpen) return;

            // 보고 있던 편지가 다른 상태로 넘어갔으면 선택을 푼다.
            if (letterID == _selectedLetterID && state != _selectedState) ClearSelection();
            RefreshAll();
        }
    }
}
