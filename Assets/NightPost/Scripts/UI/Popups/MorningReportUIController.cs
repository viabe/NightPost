using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 아침 보고. 재접속 시 밤사이 결과를 요약해 보여주고, 보상·답장을 수령한다.
    /// 해금 조건을 채운 노선이 있으면 여기서 바로 해금한다(게임 내 유일한 노선 해금 경로).
    ///
    /// 구성:
    ///   1) 요약    — GameFlowController.GetMorningReport() → MorningReportData
    ///   2) 완료 결과 — PlayerDataManager.GetUncheckedDeliveryResults()
    ///                  "모두 받기" → SelectDeliveryResult(letterID) + CheckSelectedDeliveryResult()
    ///                  (일괄 확인 API가 없어 편지 단위로 반복 호출한다)
    ///   3) 노선 해금 — ProgressionService.CanUnlockRoute() 로 후보를 추리고
    ///                  GameFlowController.UnlockRoute(routeID) 로 해금
    ///
    /// 자동 표시는 ShowIfHasReport()를 부팅 시 호출해 처리한다(보고할 게 없으면 열지 않는다).
    /// </summary>
    public class MorningReportUIController : MonoBehaviour, IUIScreen
    {
        [Header("의존성")]
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerDataManager _playerData;
        [SerializeField] private StaticDataCatalog _catalog;
        [SerializeField] private ProgressionService _progression;
        [SerializeField] private PlayerController _playerController;

        [Header("패널")]
        [SerializeField] private GameObject _panel;

        [Header("요약")]
        [SerializeField] private TMP_Text _headerText;        // "밤사이 편지 3건이 도착했습니다"
        [SerializeField] private TMP_Text _deliveryCountText; // 완료 배달 수
        [SerializeField] private TMP_Text _rewardText;        // 받을 보상 합계
        [SerializeField] private TMP_Text _replyCountText;    // 안 읽은 답장 수
        [SerializeField] private TMP_Text _letterCapacityText;// "4 / 10"

        [Header("완료 결과")]
        [SerializeField] private Transform _resultListRoot;
        [SerializeField] private MorningReportRowItem _resultRowPrefab;
        [SerializeField] private GameObject _resultEmpty;     // 완료 건 없을 때
        [Tooltip("확인 = 보상·답장 수령 후 닫기. 받을 게 없어도 닫히도록 항상 활성이다.")]
        [SerializeField] private Button _confirmButton;       // "확인"

        [Header("노선 해금")]
        [SerializeField] private GameObject _routeSection;    // 해금 가능 노선 없으면 통째로 숨김
        [SerializeField] private Transform _routeListRoot;
        [SerializeField] private RouteUnlockRowItem _routeRowPrefab;

        [Tooltip("선택. 우상단 X 등 별도 닫기 버튼이 있을 때만 연결한다(보상 수령 없이 닫힘).")]
        [SerializeField] private Button _closeButton;

        private bool _isOpen;
        private bool _subscribed;

        private readonly List<MorningReportRowItem> _resultRows = new();
        private readonly List<RouteUnlockRowItem> _routeRows = new();

        /// <summary>다른 화면이 열릴 때 닫아야 하는지 판단하는 데 쓰인다.</summary>
        public bool IsScreenOpen => _isOpen;

        private void OnDestroy() => UIScreenRouter.Unregister(this);

        private void Awake()
        {
            UIScreenRouter.Register(this);
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(ConfirmAndClose);
            }
        }

        // ── 열기 / 닫기 ──
        /// <summary>보고할 내용이 있을 때만 연다. 접속 직후 1회 호출용.</summary>
        public void ShowIfHasReport()
        {
            MorningReportData report = _flow != null ? _flow.GetMorningReport() : null;
            if (report == null) return;

            bool hasSomething = report.UncheckedDeliveryCount > 0
                                || report.UnreadReplyCount > 0
                                || report.UnlockableRouteCount > 0;
            if (!hasSomething) return;

            Open();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            // 서로 대체 관계인 화면이므로 열려 있던 다른 화면을 먼저 닫는다.
            UIScreenRouter.NotifyOpened(this);

            if (_panel != null) _panel.SetActive(true);
            if (_playerController != null) _playerController.SetControlEnabled(false);

            if (_flow == null) Debug.LogError("[MorningReport] GameFlowController 미연결", this);
            if (_playerData == null) Debug.LogError("[MorningReport] PlayerDataManager 미연결", this);

            UISoundPlayer.Play(ESFXType.ReportOpen);

            Subscribe();
            RefreshAll();
        }

        public void Close()
        {
            // _isOpen 여부와 무관하게 항상 닫는다(에디터에서 패널을 켜둔 채 실행한 경우 대비).
            _isOpen = false;

            Unsubscribe();
            ClearResultRows();
            ClearRouteRows();

            if (_panel != null) _panel.SetActive(false);
            if (_playerController != null) _playerController.SetControlEnabled(true);
        }

        // ── 갱신 ──
        private void RefreshAll()
        {
            RefreshSummary();
            RefreshResults();
            RefreshRoutes();
        }

        private void RefreshSummary()
        {
            MorningReportData report = _flow != null ? _flow.GetMorningReport() : null;
            if (report == null) return;

            if (_headerText != null)
            {
                _headerText.text = report.UncheckedDeliveryCount > 0
                    ? $"밤사이 편지 {report.UncheckedDeliveryCount}건이 도착했습니다."
                    : "밤사이 새로 도착한 편지는 없습니다.";
            }

            if (_deliveryCountText != null) _deliveryCountText.text = $"{report.UncheckedDeliveryCount}건";
            if (_rewardText != null) _rewardText.text = report.ClaimableRewardAmount.ToString("N0");
            if (_replyCountText != null) _replyCountText.text = $"{report.UnreadReplyCount}통";
            if (_letterCapacityText != null)
                _letterCapacityText.text = $"{report.CurrentLetterCount} / {report.MaxLetterCapacity}";
        }

        private void RefreshResults()
        {
            ClearResultRows();

            int count = 0;
            if (_playerData != null && _resultRowPrefab != null && _resultListRoot != null)
            {
                IReadOnlyList<DeliveryResultData> results = _playerData.GetUncheckedDeliveryResults();
                if (results != null)
                {
                    foreach (DeliveryResultData result in results)
                    {
                        if (result == null) continue;

                        LetterStaticData letter = _catalog != null ? _catalog.GetLetter(result.LetterID) : null;
                        bool hasReply = _catalog != null && _catalog.GetReplyByLetterID(result.LetterID) != null;

                        MorningReportRowItem row = Instantiate(_resultRowPrefab, _resultListRoot);
                        row.gameObject.SetActive(true);
                        row.Setup(letter != null ? letter.LetterTitle : $"편지 {result.LetterID}",
                                  result.RewardAmount, hasReply);
                        _resultRows.Add(row);
                        count++;
                    }
                }
            }

            if (_resultEmpty != null) _resultEmpty.SetActive(count == 0);
            // 확인 버튼은 닫기 역할도 하므로 받을 게 없어도 비활성화하지 않는다.
        }

        private void RefreshRoutes()
        {
            ClearRouteRows();

            int count = 0;
            if (_progression != null && _catalog != null && _routeRowPrefab != null && _routeListRoot != null)
            {
                IReadOnlyList<RouteStaticData> routes = _catalog.Routes();
                if (routes != null)
                {
                    foreach (RouteStaticData route in routes)
                    {
                        if (route == null) continue;
                        // 조건을 충족했고 아직 해금하지 않은 노선만 후보다.
                        if (!_progression.CanUnlockRoute(route.RouteID)) continue;

                        RouteUnlockRowItem row = Instantiate(_routeRowPrefab, _routeListRoot);
                        row.gameObject.SetActive(true);
                        row.Setup(route.RouteID, route.RouteName, UILabels.Region(route.RegionType), OnUnlockRoute);
                        _routeRows.Add(row);
                        count++;
                    }
                }
            }

            if (_routeSection != null) _routeSection.SetActive(count > 0);
        }

        // ── 동작 ──
        /// <summary>
        /// 확인. 미확인 결과를 모두 수령(보상·답장)한 뒤 화면을 닫는다.
        /// 노선 해금은 각 행의 버튼으로 미리 처리하므로 여기서 건드리지 않는다.
        /// </summary>
        private void ConfirmAndClose()
        {
            ClaimAllResults();
            Close();
        }

        /// <summary>미확인 결과를 모두 확인해 보상과 답장을 받는다.</summary>
        private void ClaimAllResults()
        {
            if (_flow == null || _playerData == null) return;

            IReadOnlyList<DeliveryResultData> results = _playerData.GetUncheckedDeliveryResults();
            if (results == null || results.Count == 0) return;

            // 확인 처리 중 원본 목록이 바뀌므로 편지 ID를 먼저 복사해 둔다.
            List<int> letterIds = new();
            foreach (DeliveryResultData result in results)
                if (result != null) letterIds.Add(result.LetterID);

            int claimed = 0;
            foreach (int letterId in letterIds)
            {
                if (!_flow.SelectDeliveryResult(letterId)) continue;
                if (_flow.CheckSelectedDeliveryResult()) claimed++;
            }

            // 여러 건을 받아도 보상음은 한 번만 울린다.
            if (claimed > 0) UISoundPlayer.PlayAccent(ESFXType.RewardCollect);
        }

        private void OnUnlockRoute(int routeId)
        {
            if (_flow == null) return;

            if (!_flow.UnlockRoute(routeId))
            {
                Debug.LogWarning($"[MorningReport] 노선 해금 실패 routeID={routeId}");
                ToastController.Instance?.Show("지금은 이 노선을 열 수 없어요.");
                return;
            }

            RefreshAll();
        }

        private void ClearResultRows()
        {
            for (int i = 0; i < _resultRows.Count; i++)
                if (_resultRows[i] != null) Destroy(_resultRows[i].gameObject);
            _resultRows.Clear();
        }

        private void ClearRouteRows()
        {
            for (int i = 0; i < _routeRows.Count; i++)
                if (_routeRows[i] != null) Destroy(_routeRows[i].gameObject);
            _routeRows.Clear();
        }

        // ── 이벤트 ──
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEvents.DeliveryResultChecked += OnDeliveryResultChecked;
            GameEvents.RouteUnlocked += OnRouteUnlocked;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.DeliveryResultChecked -= OnDeliveryResultChecked;
            GameEvents.RouteUnlocked -= OnRouteUnlocked;
        }

        private void OnDeliveryResultChecked(int letterID) { if (_isOpen) RefreshAll(); }
        private void OnRouteUnlocked(int routeID) { if (_isOpen) RefreshAll(); }
    }
}
