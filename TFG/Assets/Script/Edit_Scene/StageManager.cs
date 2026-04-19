using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public Transform spawnPoint;
    public AudioSource musicPlayer;

    [Header("Configuration Panel Mode")]//Customization Mode
    public GameObject customizationCanvas;
    public Camera editorCamera;

    [Header("Concert Mode")]//Concert Mode
    public GameObject concertCanvas;
    public Camera audienceCamera;

    [Header("Spawn Settings")]
    [Tooltip("根据不同歌曲(musicIndex)设置角色的初始Y轴旋转角度。比如 180 就是转个身。")]
    public float[] danceRotationYOffsets;

    [Header("Timeline Connection")]
    public TimelineManager timelineManager;

    private GameObject currentCharacter;
    private Animator charAnimator;

    void Start()
    {
        SetupContent();
        EnterCustomizationMode();
    }

    void SetupContent()
    {
        int charIndex = GameManager.Instance.selectedCharIndex;
        int musicIndex = GameManager.Instance.selectedMusicIndex;

        if (GameManager.Instance.characterPrefabs.Length > charIndex)
        {
            GameObject prefab = GameManager.Instance.characterPrefabs[charIndex];

            // 🌟 【新增逻辑】：计算最终的旋转角度
            Quaternion finalRotation = spawnPoint.rotation;
            // 如果你在面板里为这首歌配置了专属的 Y 轴旋转，就叠加上去
            if (danceRotationYOffsets != null && musicIndex < danceRotationYOffsets.Length)
            {
                finalRotation *= Quaternion.Euler(0, danceRotationYOffsets[musicIndex], 0);
            }

            // 使用计算好的 finalRotation 来生成角色
            currentCharacter = Instantiate(prefab, spawnPoint.position, finalRotation);
            charAnimator = currentCharacter.GetComponentInChildren<Animator>();

            if (charAnimator == null) Debug.LogError("No Animator found");
        }

        if (GameManager.Instance.musicClips.Length > musicIndex)
        {
            musicPlayer.clip = GameManager.Instance.musicClips[musicIndex];
        }

        if (timelineManager != null)
        {
            string currentDance = "Default_Dance";
            if (GameManager.Instance.danceStateNames.Length > musicIndex)
            {
                currentDance = GameManager.Instance.danceStateNames[musicIndex];
            }
            timelineManager.SetupDynamicTimeline(charAnimator, musicPlayer, currentDance);
        }
    }

    public void EnterCustomizationMode()
    {
        // 1. 切换界面
        if (customizationCanvas != null)
        {
            Canvas canvas = customizationCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            else customizationCanvas.SetActive(true);
        }
        if (concertCanvas != null) concertCanvas.SetActive(false);

        // 2. 告诉大管家退回待机模式
        AudienceModeSystem audienceSys = FindObjectOfType<AudienceModeSystem>();
        if (audienceSys != null)
        {
            audienceSys.StopEverything();
        }
        else
        {
            if (editorCamera != null) editorCamera.gameObject.SetActive(true);
            if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);
            if (musicPlayer != null) musicPlayer.Pause();
        }
    }

    public void StartConcert()
    {
        // 1. 切换界面
        if (customizationCanvas != null)
        {
            Canvas canvas = customizationCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = false;
            else customizationCanvas.SetActive(false);
        }
        if (concertCanvas != null) concertCanvas.SetActive(true);

        // 2. 呼叫大管家：动用 VIP 特权，直接开始正式演出！
        AudienceModeSystem audienceSys = FindObjectOfType<AudienceModeSystem>();
        if (audienceSys != null)
        {
            audienceSys.PlayAsConcert();
        }
        else
        {
            if (editorCamera != null) editorCamera.gameObject.SetActive(false);
            if (audienceCamera != null)
            {
                audienceCamera.gameObject.SetActive(true);
                audienceCamera.targetTexture = null;
            }
            if (musicPlayer != null && musicPlayer.clip != null)
            {
                musicPlayer.time = 0f;
                musicPlayer.Play();
                if (charAnimator != null) charAnimator.speed = 0f;
            }
        }
    }
}