using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// 앱 복귀 시 오프라인 동안 완료된 배달을 즉시 처리하고 주기적으로 완료 여부를 검사함
public class OfflineProgressService : MonoBehaviour
{

    // 배달 완료 여부를 반복 검사할 시간 간격임
    [SerializeField, Min(0.1f)] private float processInterval = 1f;

    // 완료된 배달 처리를 담당하는 서비스임
    private DeliveryService deliveryService;
    // 주기적 배달 완료 검사를 실행하는 코루틴임
    private Coroutine processCoroutine;
    /// <summary>
    /// 완료된 배달을 즉시 처리한 뒤 주기적 배달 완료 검사를 시작함
    /// </summary>
    public void CheckOffline()
    {
        // 현재 시간을 기준으로 완료된 배달을 즉시 처리함
        bool processResult = ProcessNow();

        // 즉시 처리에 실패했다면 경고를 출력하고 종료함
        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
            return;
        }
        // 주기적 배달 완료 검사를 시작함
        bool startResult = StartProcessing();

        // 주기적 검사 시작에 실패했다면 경고를 출력함
        if (!startResult)
        {
            Debug.LogWarning("[OfflineProgressService] 주기적 배달 완료 검사 시작 실패");
        }
    }
    /// <summary>
    /// 애플리케이션이 일시정지 상태에서 복귀하면 완료된 배달을 즉시 처리함
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        // 애플리케이션이 일시정지되는 경우에는 처리하지 않음
        if (pauseStatus) return;
        // 애플리케이션 복귀 시 완료된 배달을 즉시 처리함
        bool processResult = ProcessNow();
        // 즉시 처리에 실패했다면 경고를 출력함
        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
        }
    }
    /// <summary>
    /// 애플리케이션이 다시 포커스를 얻으면 완료된 배달을 즉시 처리함
    /// </summary>
    private void OnApplicationFocus(bool focus)
    {
        // 애플리케이션이 포커스를 잃은 경우에는 처리하지 않음
        if (!focus) return;
        // 애플리케이션 복귀 시 완료된 배달을 즉시 처리함
        bool processResult = ProcessNow();
        // 즉시 처리에 실패했다면 경고를 출력함
        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
        }
    }
    /// <summary>
    /// 컴포넌트가 비활성화되면 주기적 배달 완료 검사를 중단함
    /// </summary>
    private void OnDisable()
    {
        // 실행 중인 배달 완료 검사 코루틴을 중단함
        StopProcessing();
    }

    /// <summary>
    /// OfflineProgressService에서 사용할 배달 서비스를 등록함
    /// </summary>
    public bool Initialize(DeliveryService service)
    {
        // 전달받은 배달 서비스가 없다면 초기화하지 않음
        if (service == null) return false;
        // 완료된 배달 처리에 사용할 서비스를 저장함
        deliveryService = service;
        // 배달 서비스 등록이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 설정된 시간 간격으로 배달 완료 여부를 검사하는 코루틴을 시작함
    /// </summary>
    public bool StartProcessing()
    {
        // 배달 서비스가 등록되지 않았다면 검사를 시작하지 않음
        if (deliveryService == null) return false;
        // 검사 시간 간격이 유효하지 않다면 검사를 시작하지 않음
        if (processInterval <= 0f) return false;
        // 검사 코루틴이 이미 실행 중이라면 중복 실행하지 않음
        if (processCoroutine != null) return false;
        // 주기적 배달 완료 검사 코루틴을 시작하고 참조를 저장함
        processCoroutine = StartCoroutine(ProcessDeliveryRoutine());
        // 주기적 검사가 시작되었음을 반환함
        return true;
    }
    /// <summary>
    /// 실행 중인 주기적 배달 완료 검사를 중단함
    /// </summary>
    public bool StopProcessing()
    {
        // 실행 중인 검사 코루틴이 없다면 중단하지 않음
        if (processCoroutine == null) return false;
        // 현재 실행 중인 검사 코루틴을 중단함
        StopCoroutine(processCoroutine);
        // 코루틴 참조를 초기화함
        processCoroutine = null;
        // 주기적 검사가 중단되었음을 반환함
        return true;
    }
    /// <summary>
    /// 현재 시간을 기준으로 완료된 배달을 즉시 처리함
    /// </summary>
    public bool ProcessNow()
    {
        // 배달 서비스가 등록되지 않았다면 완료 처리를 실행하지 않음
        if (deliveryService == null) return false;
        // 완료 시간이 지난 배달을 찾아 결과 생성을 처리함
        deliveryService.ProcessCompletedDeliveries();
        // 완료된 배달 처리가 실행되었음을 반환함
        return true;
    }
    /// <summary>
    /// 설정된 시간 간격마다 완료된 배달을 반복 처리함
    /// </summary>
    private IEnumerator ProcessDeliveryRoutine()
    {
        // 배달 서비스가 등록되지 않았다면 코루틴을 종료함
        if (deliveryService == null) yield break;
        // 컴포넌트가 활성화된 동안 배달 완료 검사를 반복함
        while (true)
        {
            // 현재 시간을 기준으로 완료된 배달을 처리함
            ProcessNow();
            // 게임 시간 배율과 관계없이 설정된 시간만큼 대기함
            yield return new WaitForSecondsRealtime(processInterval);
        }
    }
}
