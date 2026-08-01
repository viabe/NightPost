using UnityEngine;

// 완료한 배달 횟수를 기준으로 배달부와 노선의 해금 상태를 관리함
public class ProgressionService : MonoBehaviour
{
    // 배달부와 노선 정적 데이터를 조회하는 카탈로그임
    private StaticDataCatalog staticDataCatalog;
    // 플레이어의 보유 배달부와 해금 노선 데이터를 관리하는 매니저임
    private PlayerDataManager playerDataManager;
    /// <summary>
    /// 진행도 해금 처리에 필요한 정적 데이터 카탈로그와 플레이어 데이터 매니저를 등록함
    /// </summary>
    public bool Initialize(StaticDataCatalog catalog, PlayerDataManager dataManager)
    {
        // 필요한 참조 중 하나라도 없다면 초기화하지 않음
        if (catalog == null || dataManager == null) return false;
        // 정적 데이터 카탈로그를 저장함
        staticDataCatalog = catalog;
        // 플레이어 데이터 매니저를 저장함
        playerDataManager = dataManager;
        // 필요한 참조 등록이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 기본 해금 조건이 설정된 배달부와 노선을 플레이어 데이터에 추가함
    /// </summary>
    public void ApplyDefaultUnlocks()
    {
        // 필요한 참조가 등록되지 않았다면 기본 해금을 처리하지 않음
        if (staticDataCatalog == null || playerDataManager == null) return;
        // 등록된 전체 배달부 정적 데이터를 순회함
        foreach (CourierStaticData courierStaticData in staticDataCatalog.Couriers())
        {
            // 유효하지 않은 배달부 데이터는 건너뜀
            if (courierStaticData == null) continue;
            // 해금 조건이 없는 배달부는 건너뜀
            if (courierStaticData.UnlockCondition == null) continue;
            // 기본 해금 대상이 아닌 배달부는 건너뜀
            if (!courierStaticData.UnlockCondition.IsUnlockedByDefault) continue;
            // 이미 보유한 배달부는 중복 추가하지 않음
            if (playerDataManager.IsCourierOwned(courierStaticData.CourierID)) continue;
            // 기본 해금 배달부를 플레이어의 보유 목록에 추가함
            playerDataManager.AddOwnedCourier(courierStaticData.CourierID);
        }

        // 등록된 전체 노선 정적 데이터를 순회함
        foreach (RouteStaticData routeStaticData in staticDataCatalog.Routes())
        {
            // 유효하지 않은 노선 데이터는 건너뜀
            if (routeStaticData == null) continue;
            // 해금 조건이 없는 노선은 건너뜀
            if (routeStaticData.UnlockCondition == null) continue;
            // 기본 해금 대상이 아닌 노선은 건너뜀
            if (!routeStaticData.UnlockCondition.IsUnlockedByDefault) continue;
            // 이미 해금된 노선은 중복 추가하지 않음
            if (playerDataManager.IsRouteUnlocked(routeStaticData.RouteID)) continue;
            // 기본 해금 노선을 플레이어의 해금 목록에 추가함
            playerDataManager.AddUnlockedRoute(routeStaticData.RouteID);
        }

    }
    /// <summary>
    /// 완료한 배달 횟수를 기준으로 새롭게 충족된 배달부와 노선의 해금 조건을 확인함
    /// </summary>
    public void EvaluateProgressUnlocks()
    {
        // 필요한 참조가 등록되지 않았다면 진행도 해금을 처리하지 않음
        if (staticDataCatalog == null || playerDataManager == null) return;
        // 현재까지 완료한 배달 횟수를 조회함
        int completedDeliveryCount = playerDataManager.GetCompletedDeliveryCount();
        // 등록된 전체 배달부 정적 데이터를 순회함
        foreach (CourierStaticData courierStaticData in staticDataCatalog.Couriers())
        {
            // 유효하지 않은 배달부 데이터는 건너뜀
            if (courierStaticData == null) continue;
            // 해금 조건이 없는 배달부는 건너뜀
            if (courierStaticData.UnlockCondition == null) continue;
            // 기본 해금 대상은 진행도 해금 검사에서 제외함
            if (courierStaticData.UnlockCondition.IsUnlockedByDefault) continue;
            // 이미 보유한 배달부는 중복 추가하지 않음
            if (playerDataManager.IsCourierOwned(courierStaticData.CourierID)) continue;
            // 완료한 배달 횟수가 요구 조건보다 적다면 해금하지 않음
            if (completedDeliveryCount < courierStaticData.UnlockCondition.RequiredCompletedDeliveryCount) continue;
            // 해금 조건을 충족한 배달부를 플레이어의 보유 목록에 추가함
            playerDataManager.AddOwnedCourier(courierStaticData.CourierID);
        }
    }

    /// <summary>
    /// 지정한 노선이 플레이어의 진행 조건을 충족해 수동 해금 가능한지 확인함
    /// </summary>
    public bool CanUnlockRoute(int routeID)
    {
        // 정적 데이터 카탈로그 또는 플레이어 데이터 매니저가 등록되지 않았다면 해금할 수 없음
        if (staticDataCatalog == null || playerDataManager == null) return false;
        // 유효하지 않은 노선 ID라면 해금할 수 없음
        if(routeID <= 0) return false;

        // 지정한 ID의 노선 정적 데이터를 조회함
        RouteStaticData routeStaticData = staticDataCatalog.GetRoute(routeID);

        // 해당하는 노선 데이터가 없다면 해금할 수 없음
        if(routeStaticData == null) return false;

        // 노선의 해금 조건이 없다면 해금할 수 없음
        if(routeStaticData.UnlockCondition == null) return false;

        // 기본 해금 노선은 수동 해금 대상이 아니므로 해금할 수 없음
        if(routeStaticData.UnlockCondition.IsUnlockedByDefault) return false;

        // 이미 해금된 노선이라면 다시 해금할 수 없음
        if(playerDataManager.IsRouteUnlocked(routeID)) return false;

        // 현재까지 완료한 배달 횟수를 조회함
        int completedDeliveryCount = playerDataManager.GetCompletedDeliveryCount();

        // 완료한 배달 횟수가 노선의 요구 조건 이상인지 반환함
        return completedDeliveryCount >= routeStaticData.UnlockCondition.RequiredCompletedDeliveryCount;
    }
    /// <summary>
    /// 진행 조건을 충족한 잠긴 노선을 플레이어의 해금 목록에 추가함
    /// </summary>
    public bool UnlockRoute(int routeID)
    {
        // 지정한 노선이 현재 수동 해금 가능한 상태가 아니라면 해금하지 않음
        if(!CanUnlockRoute(routeID)) return false;
        // 플레이어 데이터 매니저에 지정한 노선의 해금을 요청함
        // 노선 해금 처리 결과를 반환함
        return playerDataManager.AddUnlockedRoute(routeID);
    }


}
