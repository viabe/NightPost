using UnityEngine;

// 공통 테스트 초기화 클래스
[DefaultExecutionOrder(-100)]
public class NightPostTestBootstrap : MonoBehaviour
{
    [Header("Test Save Data")]
    [SerializeField] private PlayerSaveData testSaveDataTemplate;

    [Header("References")]
    [SerializeField] private PlayerDataManager playerDataManager;

    public PlayerSaveData RuntimeSaveData { get; private set; }

    private void Awake()
    {
        if (testSaveDataTemplate == null)
        {
            Debug.LogError(
                "[TestBootstrap] testSaveDataTemplate이 설정되지 않았습니다.");
            return;
        }

        if (playerDataManager == null)
        {
            Debug.LogError(
                "[TestBootstrap] PlayerDataManager가 설정되지 않았습니다.");
            return;
        }

        RuntimeSaveData = Instantiate(testSaveDataTemplate);

        RuntimeSaveData.name = $"{testSaveDataTemplate.name}_Runtime";

        playerDataManager.Initialize(RuntimeSaveData);

        Debug.Log(
            "[TestBootstrap] 공통 테스트 저장 데이터 초기화 완료\n" +
            $"Runtime Data: {RuntimeSaveData.name}\n" +
            $"LetterProgress Count: " +
            $"{RuntimeSaveData.LetterProgressesList.Count}\n" +
            $"OwnedCourier Count: " +
            $"{RuntimeSaveData.OwnedCourierIDs.Count}\n" +
            $"UnlockedRoute Count: " +
            $"{RuntimeSaveData.UnlockedRouteIDs.Count}");
    }
}

