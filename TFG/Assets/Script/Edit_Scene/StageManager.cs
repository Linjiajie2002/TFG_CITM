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
            currentCharacter = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
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

            Debug.Log($"【StageManager】正在向时间轴发送数据... 舞蹈: {currentDance}");
            timelineManager.SetupDynamicTimeline(charAnimator, musicPlayer, currentDance);
        }
        else
        {
            Debug.LogError("【严重错误】StageManager 上的 Timeline Manager 槽位是空的！");
        }
    }

    public void EnterCustomizationMode()
    {
        if (customizationCanvas != null)
        {
            Canvas canvas = customizationCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            else customizationCanvas.SetActive(true);
        }

        if (concertCanvas != null) concertCanvas.SetActive(false);
        if (editorCamera != null) editorCamera.gameObject.SetActive(true);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);

        if (musicPlayer != null) musicPlayer.Pause();
    }

    public void StartConcert()
    {
        if (customizationCanvas != null)
        {
            Canvas canvas = customizationCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = false;
            else customizationCanvas.SetActive(false);
        }

        if (concertCanvas != null) concertCanvas.SetActive(true);
        if (editorCamera != null) editorCamera.gameObject.SetActive(false);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(true);

        if (musicPlayer != null && musicPlayer.clip != null)
        {
            musicPlayer.time = 0f;
            musicPlayer.Play();

            if (charAnimator != null) charAnimator.speed = 0f;

            // 🔪 已经彻底删除了 Invoke("BackToMainMenu", songDuration); 
            // 现在的结局由 PerformanceEndManager 全权接管！
            Debug.Log($"Concert Duration: {musicPlayer.clip.length} - 正在播放...");
        }
    }

}