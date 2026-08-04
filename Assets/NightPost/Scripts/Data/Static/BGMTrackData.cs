using UnityEngine;

[System.Serializable]
public class BGMTrackData
{
    [SerializeField] private EBGMType bgmType;
    [SerializeField] private AudioClip audioClip;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

    public EBGMType BGMType => bgmType;
    public AudioClip AudioClip => audioClip;
    public float BgmVolume => bgmVolume;
}
