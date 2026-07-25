using System;
using System.Collections.Generic;
using UnityEngine;
public enum ERegionType
{
    None,
    // 기본 지역
    Town,
    // 산
    Mountain,
    // 외각 지역
    Outskirts
}
public enum EVehicleType
{
    None,
    // 도보
    Walking,
    // 자전거
    Bicycle,
    // 긴 노선 / 긴급 편지
    Motorcycle,
    // 무게 조건
    Truck
}
public enum ECourierTraitType
{
    None,
    // 산길 노선 배달 시간 감소
    MountainExpert,
    // 야간 편지 또는 야간 노선 시간 감소
    NightExpert,
    // 긴급 편지 배달 시간 감소
    UrgentExpert,
    // 무거운 편지 시간 페널티 감소
    HeavyLoadExpert
}
[Serializable]
public class CourierTraitData
{
    // 특성 종류
    [SerializeField] private ECourierTraitType traitType = ECourierTraitType.None;
    // 배달 시간 감소율
    [SerializeField, Range(0f, 1f)] private float timeReductionRate = 0.2f;
    public ECourierTraitType TraitType => traitType;
    public float TimeReductionRate => timeReductionRate;
}
public class CourierStaticData : ScriptableObject
{
    // 배달부 ID
    [SerializeField] private int courierID = 0;
    // 배달부 이름
    [SerializeField] private string courierName = string.Empty;
    // 이동 수단
    [SerializeField] private EVehicleType transportation = EVehicleType.None;
    // 기본 속도
    [SerializeField] private float speed = 1.0f;
    // 특성
    [SerializeField] private CourierTraitData trait;
    // 이미지
    [SerializeField] private Sprite courierImage = null;

    public int CourierID => courierID;
    public string CourierName => courierName;
    public EVehicleType Transportation => transportation;
    public float Speed => speed;
    public CourierTraitData Trait => trait;
    public Sprite CourierImage => courierImage;
}
