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

    // 【新增】核心连线：让 StageManager 认识 TimelineManager
    [Header("Timeline Connection")]
    public TimelineManager timelineManager;

    private GameObject currentCharacter;
    private Animator charAnimator;

    void Start()
    {
        // genera character and music
        SetupContent();

        // start in customization mode
        EnterCustomizationMode();
    }

    // generate character and prepare music
    void SetupContent()
    {
        // get selected indices
        int charIndex = GameManager.Instance.selectedCharIndex;
        int musicIndex = GameManager.Instance.selectedMusicIndex;

        // generate character
        if (GameManager.Instance.characterPrefabs.Length > charIndex)
        {
            GameObject prefab = GameManager.Instance.characterPrefabs[charIndex];

            // spawn character at spawn point
            currentCharacter = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            // get Animator component
            charAnimator = currentCharacter.GetComponentInChildren<Animator>();

            // if no Animator found, log error
            if (charAnimator == null)
            {
                Debug.LogError("No Animator found");
            }
        }

        // Prepare music
        if (GameManager.Instance.musicClips.Length > musicIndex)
        {
            musicPlayer.clip = GameManager.Instance.musicClips[musicIndex];
        }

        // --- 【新增核心逻辑：通知时间轴干活】 ---
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
            // 防呆提示：如果你忘了连线，控制台会立刻告诉你！
            Debug.LogError("【严重错误】StageManager 上的 Timeline Manager 槽位是空的！请在 Inspector 里把 Timeline_Root 拖进去！");
        }
    }

    //customization mode
    public void EnterCustomizationMode()
    {
        // UI change
        if (customizationCanvas != null) customizationCanvas.SetActive(true);
        if (concertCanvas != null) concertCanvas.SetActive(false);

        // Camera change
        if (editorCamera != null) editorCamera.gameObject.SetActive(true);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);

        // pause music
        if (musicPlayer != null) musicPlayer.Stop();
    }

    // Concert mode
    public void StartConcert()
    {
        // Ui change
        if (customizationCanvas != null) customizationCanvas.SetActive(false);
        if (concertCanvas != null) concertCanvas.SetActive(true);

        // Camera change
        if (editorCamera != null) editorCamera.gameObject.SetActive(false);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(true);

        // play music
        if (musicPlayer != null && musicPlayer.clip != null)
        {
            musicPlayer.Play();

            // get song duration
            float songDuration = musicPlayer.clip.length;

            // Set timer to return to main menu after song ends 
            Invoke("BackToMainMenu", songDuration);

            Debug.Log($"Durancion: {songDuration}");
        }

        // play dance
        PlaySelectedDance();
    }

    // play dance according to selected music
    void PlaySelectedDance()
    {
        if (charAnimator != null)
        {
            int musicIndex = GameManager.Instance.selectedMusicIndex;
            string[] danceNames = GameManager.Instance.danceStateNames;

            //check if index is valid
            if (danceNames.Length > musicIndex)
            {
                string stateName = danceNames[musicIndex];


                charAnimator.CrossFade(stateName, 0.1f);


                //Debug.Log($"{stateName}");
            }
            else
            {
                Debug.LogWarning("GameManager no have Dance State Names");
            }
        }
    }

    // back to main menu
    public void BackToMainMenu()
    {

        CancelInvoke();
        SceneManager.LoadScene(0);
    }
}