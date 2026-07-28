using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class OfflineProgressService : MonoBehaviour
{

    [SerializeField, Min(0.1f)] private float processInterval = 1f;

    private DeliveryService deliveryService;
    private Coroutine processCoroutine;
    public void CheckOffline()
    {
        bool processResult = ProcessNow();

        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
            return;
        }
        bool startResult = StartProcessing();

        if (!startResult)
        {
            Debug.LogWarning("[OfflineProgressService] 주기적 배달 완료 검사 시작 실패");
        }
    }
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) return;
        bool processResult = ProcessNow();
        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
        }
    }
    private void OnApplicationFocus(bool focus)
    {
        if(!focus) return;
        bool processResult = ProcessNow();
        if (!processResult)
        {
            Debug.LogWarning("[OfflineProgressService] 즉시 배달 완료 검사 실패");
        }
    }
    private void OnDisable()
    {
        StopProcessing();
    }

    public bool Initialize(DeliveryService service)
    {
        if (service == null) return false;
        deliveryService = service;
        return true;
    }
    public bool StartProcessing()
    {
        if (deliveryService == null) return false;
        if(processInterval <= 0f) return false;
        if (processCoroutine != null) return false;
        processCoroutine = StartCoroutine(ProcessDeliveryRoutine());
        return true;
    }
    public bool StopProcessing()
    {
        if (processCoroutine == null) return false;
        StopCoroutine(processCoroutine);
        processCoroutine = null;
        return true;
    }
    public bool ProcessNow()
    {
        if(deliveryService == null) return false;
        deliveryService.ProcessCompletedDeliveries();
        return true;
    }
    private IEnumerator ProcessDeliveryRoutine()
    {
        if(deliveryService == null) yield break;
        while(true)
        {
            ProcessNow();
            yield return new WaitForSecondsRealtime(processInterval);
        }
    }
}
