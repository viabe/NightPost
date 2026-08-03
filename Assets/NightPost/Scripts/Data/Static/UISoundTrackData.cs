using UnityEngine;

[System.Serializable]
public class UISoundTrackData
{
    [SerializeField] private EUISoundType soundType;
    [SerializeField] private AudioClip audioClip;

    public EUISoundType SoundType => soundType;
    public AudioClip AudioClip => audioClip;
}
