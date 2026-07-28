using System;
using System.Collections.Generic;
using UnityEngine;


// StaticData 조회 시스템 : 변하지 않는 원본 데이터 조회
// LetterStaticData, CouierStaticData, RouteStaticData, FacilityStaticData, ReplyStaticData 보관 및 아이디 조회

public class StaticDataCatalog : MonoBehaviour
{
    // 가지고 있어야하는 static data리스트
    [SerializeField] private List<LetterStaticData> letterStaticDatas = new();
    [SerializeField] private List<CourierStaticData> courierStaticDatas = new();
    [SerializeField] private List<RouteStaticData> routeStaticDatas = new();
    [SerializeField] private List<FacilityStaticData> facilityStaticDatas = new();
    [SerializeField] private List<ReplyStaticData> replyStaticDatas = new();

    // 초기에 Dictionary에 ID, 정적 데이터 저장
    private Dictionary<int, LetterStaticData> letterStaticDataDic;
    private Dictionary<int, CourierStaticData> courierStaticDataDic;
    private Dictionary<int, RouteStaticData> routeStaticDataDic;
    private Dictionary<int, FacilityStaticData> facilityStaticDataDic;
    private Dictionary<int, ReplyStaticData> replyStaticDataDic;

    private Dictionary<int, ReplyStaticData> replyByLetterIDDic;

    private void Awake()
    {
        SetDictionary();
    }

    #region 초기화 함수
    private void SetDictionary()
    {
        letterStaticDataDic = BuildDictionary(letterStaticDatas, (LetterStaticData data) => data.LetterID);
        courierStaticDataDic = BuildDictionary(courierStaticDatas, (CourierStaticData data) => data.CourierID);
        routeStaticDataDic = BuildDictionary(routeStaticDatas, (RouteStaticData data) => data.RouteID);
        facilityStaticDataDic = BuildDictionary(facilityStaticDatas, (FacilityStaticData data) => data.FacilityID);
        replyStaticDataDic = BuildDictionary(replyStaticDatas, (ReplyStaticData data) => data.ReplyID);
        replyByLetterIDDic = BuildDictionary(replyStaticDatas, data => data.LinkedLetterID);
    }
    // Func<T, int> : T를 매개변수로 받고 int로 반환하는 함수
    // Func : 매서드를 변수처럼 전달하는 델리게이트 타입임 <-> Action은 반환값 X
    private Dictionary<int, T> BuildDictionary<T>(List<T> dataList, Func<T, int> getID) where T : UnityEngine.Object
    {
        Dictionary<int, T> dic = new();
        foreach(T data in dataList)
        {
            if (data == null)
            {
                Debug.LogWarning("[BuildDictionary] null 데이터가 있습니다.");
                continue;
            }
            int id = getID(data);
            if (dic.ContainsKey(id))
            {
                Debug.LogError($"[BuildDictionary] {typeof(T).Name} 중복 ID: {id}");
                continue;
            }
            
            dic.Add(id, data);
        }
        return dic;
    }
    #endregion
    #region 조회 함수
    // 편지 데이터 조회
    public IReadOnlyList<LetterStaticData> Letters()
    {
        return letterStaticDatas;
    }
    public LetterStaticData GetLetter(int letterID)
    {
        if (letterStaticDataDic.TryGetValue(letterID, out LetterStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 존재하지 않는 Letter ID: {letterID}");

        return null;
    }

    // 배달부 조회
    public IReadOnlyList<CourierStaticData> Couriers()
    {
        return courierStaticDatas;
    }
    public CourierStaticData GetCourier(int courierID)
    {
        if (courierStaticDataDic.TryGetValue(courierID, out CourierStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 존재하지 않는 courierID ID: {courierID}");

        return null;
    }
    // 루틴 조회
    public IReadOnlyList<RouteStaticData> Routes()
    {
        return routeStaticDatas;
    }
    public RouteStaticData GetRoute(int routeID)
    {
        if (routeStaticDataDic.TryGetValue(routeID, out RouteStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 존재하지 않는 routeID ID: {routeID}");

        return null;
    }
    // 시설 조회
    public IReadOnlyList<FacilityStaticData> Facilities()
    {
        return facilityStaticDatas;
    }
    public FacilityStaticData GetFacility(int facilityID)
    {
        if (facilityStaticDataDic.TryGetValue(facilityID, out FacilityStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 존재하지 않는 facilityID ID: {facilityID}");

        return null;
    }
    // 대답 조회
    public IReadOnlyList<ReplyStaticData> Replies()
    {
        return replyStaticDatas;
    }
    public ReplyStaticData GetReply(int replyID)
    {
        if (replyStaticDataDic.TryGetValue(replyID, out ReplyStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 존재하지 않는 replyID ID: {replyID}");

        return null;
    }
    public ReplyStaticData GetReplyByLetterID(int letterID)
    {

        if (replyByLetterIDDic.TryGetValue(letterID, out ReplyStaticData data)) return data;

        Debug.LogWarning($"[StaticDataCatalog] 편지 ID {letterID}에 연결된 답장이 없습니다.");


        return null;
    }
    #endregion
}
