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
        // 【修改点1】隐藏/显示UI时，优先开关 Canvas 组件，而不是 SetActive
        // 这样可以保证 UI 看不见，但是背后的 TimelineManager 脚本依然存活并在工作！
        if (customizationCanvas != null)
        {
            Canvas canvas = customizationCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            else customizationCanvas.SetActive(true); // 兼容备用
        }

        if (concertCanvas != null) concertCanvas.SetActive(false);
        if (editorCamera != null) editorCamera.gameObject.SetActive(true);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);

        // 【修改点2】从演出切回编辑时，用 Pause 暂停而不是 Stop，防止破坏状态
        if (musicPlayer != null) musicPlayer.Pause();
    }

    public void StartConcert()
    {
        // 同样，只关闭渲染，保留 TimelineManager 的大脑继续运作
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
            // 【核心修复 A】强制将音乐进度归零！确保一切从头开始
            musicPlayer.time = 0f;
            musicPlayer.Play();

            // 【核心修复 B】骗过时间轴：强制把动画速度设为0
            // 这样时间轴在下一帧发现“咦，播放中但速度是0”，就会立刻触发一次完美的从零同步！
            if (charAnimator != null) charAnimator.speed = 0f;

            float songDuration = musicPlayer.clip.length;
            Invoke("BackToMainMenu", songDuration);
            Debug.Log($"Concert Duration: {songDuration}");
        }

        // 【核心修复 C】彻底删除了 PlaySelectedDance() 的调用！
        // 为什么？因为我们要求“演出模式和编辑模式完全一样”！
        // 所以我们放权给隐藏在幕后的 TimelineManager，由它来控制所有的动画！
    }

    public void BackToMainMenu()
    {
        CancelInvoke();
        SceneManager.LoadScene(0);
    }
}