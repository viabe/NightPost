using System.Collections.Generic;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 편지 겉면 → 배정 → 배달 시작 흐름을 실제 시스템에 잇는 코디네이터 Presenter.
    /// 아키텍처: 버튼 입력 → GameFlowController/Service 호출 → 시스템이 GameEvents 발생.
    /// (이 클래스는 명령 흐름 담당이라 GameEvents 구독은 하지 않는다. 화면 표시는 팝업에 위임.)
    ///
    /// 실제 배달 시작 조건(DeliveryService.CanStartDelivery):
    ///   - 편지 상태 == Waiting (New면 먼저 분류: New→Waiting)
    ///   - 노선 RegionType == 편지 DestinationRegion, 그리고 노선 해금됨
    ///   - 배달부 보유 + 현재 미배달
    /// </summary>
    public class DeliveryFlowPresenter : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerDataManager _playerData;
        [SerializeField] private StaticDataCatalog _catalog;
        [SerializeField] private LetterService _letterService;

        /// <summary>배달 가능한(New/Waiting) 첫 편지의 겉면을 연다. 버튼 OnClick에 직접 연결 가능(void).</summary>
        public void OpenFirstAvailableLetter()
        {
            if (_letterService == null) { Debug.LogWarning("[DeliveryFlow] LetterService 미연결"); return; }

            IReadOnlyList<LetterStaticData> letters = _letterService.GetAvailableLetters();
            if (letters == null || letters.Count == 0)
            {
                Debug.Log("[DeliveryFlow] 배달 가능한 편지가 없습니다. (DebugReceiveCatalogLetters로 시드 가능)");
                ToastController.Instance?.Show("새로 온 편지가 없어요.");
                return;
            }
            OpenEnvelope(letters[0].LetterID);
        }

        /// <summary>특정 편지의 겉면 팝업을 실데이터로 연다.</summary>
        public void OpenEnvelope(int letterID)
        {
            LetterStaticData letter = _catalog != null ? _catalog.GetLetter(letterID) : null;
            if (letter == null) return;

            EnvelopePopupController popup = PopupManager.Instance != null
                ? PopupManager.Instance.Get<EnvelopePopupController>(UIScreenId.EnvelopePopup) : null;
            if (popup == null) { Debug.LogWarning("[DeliveryFlow] EnvelopePopup 미등록"); return; }

            LetterProgressData progress = _playerData != null ? _playerData.GetLetterProgress(letterID) : null;

            popup.Open(new EnvelopeModel
            {
                LetterId = letterID,
                Title = letter.LetterTitle,
                SenderName = letter.SenderName,
                RegionLabel = RegionLabel(letter.DestinationRegion),
                Reward = letter.LetterReward,
                IsUrgent = letter.Urgency == ELetterUrgency.Urgent,
                IsHeavy = letter.Weight == ELetterWeight.Heavy,
                IsRead = progress != null && progress.IsRead,
                OnAssign = () => OnAssignRequested(letterID),
            });
        }

        // 배정하기: 편지 선택(읽음) + 필요 시 분류(New→Waiting) 후 배정 팝업으로.
        private void OnAssignRequested(int letterID)
        {
            if (_flow == null) return;
            if (!_flow.SelectLetter(letterID))
            {
                Debug.LogWarning("[DeliveryFlow] 편지 선택 실패");
                ToastController.Instance?.Show("이 편지는 지금 배정할 수 없어요.");
                return;
            }

            LetterProgressData progress = _playerData != null ? _playerData.GetLetterProgress(letterID) : null;
            if (progress != null && progress.State == ELetterProgressState.New)
            {
/*                if (! _flow.CompleteSelectedLetterSorting())
                    Debug.LogWarning("[DeliveryFlow] 분류 완료 실패");*/
            }
            OpenAssignment(letterID);
        }

        /// <summary>선택된 편지에 맞는 배달부/노선 목록으로 배정 팝업을 연다.</summary>
        public void OpenAssignment(int letterID)
        {
            LetterStaticData letter = _catalog != null ? _catalog.GetLetter(letterID) : null;
            if (letter == null) return;

            AssignmentPopupController popup = PopupManager.Instance != null
                ? PopupManager.Instance.Get<AssignmentPopupController>(UIScreenId.Assignment) : null;
            if (popup == null) { Debug.LogWarning("[DeliveryFlow] AssignmentPopup 미등록"); return; }

            // 보유한 배달부만, 배달 중이면 선택 불가
            List<CourierOption> couriers = new();
            foreach (CourierStaticData c in _catalog.Couriers())
            {
                if (c == null) continue;
                if (!_playerData.IsCourierOwned(c.CourierID)) continue;
                couriers.Add(new CourierOption
                {
                    CourierId = c.CourierID,
                    Name = c.CourierName,
                    VehicleLabel = VehicleLabel(c.Transportation),
                    IsAvailable = !_playerData.IsCourierDelivering(c.CourierID),
                });
            }

            // 편지 지역과 일치하는 노선만, 해금 안 됐으면 선택 불가
            List<RouteOption> routes = new();
            foreach (RouteStaticData r in _catalog.Routes())
            {
                if (r == null) continue;
                if (r.RegionType != letter.DestinationRegion) continue;
                routes.Add(new RouteOption
                {
                    RouteId = r.RouteID,
                    Name = r.RouteName,
                    DifficultyLabel = DifficultyLabel(r.Difficulty),
                    IsUnlocked = _playerData.IsRouteUnlocked(r.RouteID),
                });
            }

            popup.Open(new AssignmentModel
            {
                LetterId = letterID,
                LetterTitle = letter.LetterTitle,
                RegionLabel = RegionLabel(letter.DestinationRegion),
                IsUrgent = letter.Urgency == ELetterUrgency.Urgent,
                Couriers = couriers,
                Routes = routes,
                Estimate = (courierId, routeId) => EstimateFor(letter, courierId, routeId),
                OnStartDelivery = OnStartDelivery,
            });
        }

        // 예상치: 현재 DeliveryService 계산과 동일 = ceil(노선기본시간 / 배달부속도). 보상 = 편지 기본 보상.
        private DeliveryEstimate EstimateFor(LetterStaticData letter, int courierId, int routeId)
        {
            CourierStaticData courier = _catalog.GetCourier(courierId);
            RouteStaticData route = _catalog.GetRoute(routeId);
            int seconds = 0;
            if (courier != null && route != null && courier.Speed > 0f && route.BaseDeliveryTimeSeconds > 0f)
                seconds = Mathf.CeilToInt(route.BaseDeliveryTimeSeconds / courier.Speed);
            return new DeliveryEstimate { Seconds = seconds, Reward = letter.LetterReward };
        }

        // 성공 시 true를 돌려주면 배정 팝업이 닫힌다. 실패면 false → 팝업 유지 + 토스트 안내.
        private bool OnStartDelivery(int courierId, int routeId)
        {
            if (_flow == null) return false;
            bool ok = _flow.StartSelectedLetterDelivery(courierId, routeId);
            if (ok)
            {
                Debug.Log($"[DeliveryFlow] 배달 시작 성공 (courier={courierId}, route={routeId})");
            }
            else
            {
                Debug.LogWarning($"[DeliveryFlow] 배달 시작 실패 — 보유/배달중/노선해금/지역/Waiting 상태 확인 (courier={courierId}, route={routeId})");
                ToastController.Instance?.Show("지금은 배달을 시작할 수 없어요.");
            }
            return ok;
        }

        /// <summary>[샌드박스 디버그] 세이브에 편지가 없을 때 카탈로그 앞쪽 편지 몇 통을 수신시킨다.</summary>
        public void DebugReceiveCatalogLetters()
        {
            if (_letterService == null || _catalog == null) return;
            int count = 0;
            foreach (LetterStaticData letter in _catalog.Letters())
            {
                if (letter == null) continue;
                if (_letterService.ReceiveLetter(letter.LetterID)) count++;
                if (count >= 4) break;
            }
            Debug.Log($"[DeliveryFlow] 디버그 편지 {count}통 수신");
        }

        // ── enum → 표시명 ──
        private static string RegionLabel(ERegionType r)
        {
            switch (r)
            {
                case ERegionType.Town: return "마을";
                case ERegionType.Mountain: return "산간";
                case ERegionType.Outskirts: return "외곽";
                default: return "미지정";
            }
        }

        private static string VehicleLabel(EVehicleType v)
        {
            switch (v)
            {
                case EVehicleType.Walking: return "도보";
                case EVehicleType.Bicycle: return "자전거";
                case EVehicleType.Motorcycle: return "오토바이";
                case EVehicleType.Truck: return "트럭";
                default: return "-";
            }
        }

        private static string DifficultyLabel(ERouteDifficulty d)
        {
            switch (d)
            {
                case ERouteDifficulty.Easy: return "쉬움";
                case ERouteDifficulty.Normal: return "보통";
                case ERouteDifficulty.Hard: return "어려움";
                default: return "-";
            }
        }
    }
}
