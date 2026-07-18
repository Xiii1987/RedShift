using UnityEngine;



public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip[] hoverClips;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip deniedClip;
    [SerializeField] private AudioClip completeClip;
    [SerializeField] private AudioClip tabChangeClip;
	[SerializeField] private AudioClip DoorOpenClip;
	[SerializeField] private AudioClip DoorCloseClip;



    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayHover()
    {
        if (hoverClips == null || hoverClips.Length == 0)
            return;

        PlayClip(hoverClips[Random.Range(0, hoverClips.Length)]);
    }

    public void PlayConfirm()
    {
        PlayClip(confirmClip);
    }

    public void PlaySuccess()
    {
        PlayClip(successClip);
    }

    public void PlayDenied()
    {
        PlayClip(deniedClip);
    }

    public void PlayComplete()
    {
        PlayClip(completeClip);
    }

    public void PlayTabChange()
    {
        PlayClip(tabChangeClip);
    }
	
	public void PlayDoorOpen()
    {
        PlayClip(DoorOpenClip);
    }
	
	public void PlayDoorClose()
    {
        PlayClip(DoorCloseClip);
    }
	

public void PlaySound(RedshiftUISoundType soundType)
{
    switch (soundType)
    {
        case RedshiftUISoundType.Hover:
            PlayHover();
            break;

        case RedshiftUISoundType.Confirm:
            PlayConfirm();
            break;

        case RedshiftUISoundType.Success:
            PlaySuccess();
            break;

        case RedshiftUISoundType.Denied:
            PlayDenied();
            break;

        case RedshiftUISoundType.Complete:
            PlayComplete();
            break;

        case RedshiftUISoundType.TabChange:
            PlayTabChange();
            break;
		case RedshiftUISoundType.DoorOpen:
            PlayDoorOpen();
            break;
		case RedshiftUISoundType.DoorClose:
            PlayDoorClose();
            break;
    }
}
  
  private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, uiVolume);
    }
}