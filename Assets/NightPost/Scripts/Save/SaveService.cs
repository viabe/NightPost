using UnityEngine;

// 현재 플레이어 상태 제공
public class SaveService : MonoBehaviour
{
    public PlayerSaveData CurrentPlayerData { get; private set; }

    public void CreateNewData()
    {
        CurrentPlayerData = new PlayerSaveData();
    }

    public void LoadPlayerData()
    {

    }

    public void SavePlayerData()
    {

    }
}
