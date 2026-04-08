using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PerformanceEndManager : MonoBehaviour
{
    [Header("=== 监听目标 ===")]
    public AudioSource musicSource;

    [Header("=== UI 引用 ===")]
    public GameObject endScreenPanel;
    public Button manualExitButton;
    public Button endScreenReturnButton;

    [Header("=== 场景跳转配置 ===")]
    public string menuSceneName = "MainMenu";

    private bool isPerformanceModeActive = false;
    private bool hasTriggeredEnd = false;

    // 记录曾经到达过的最高时间，防 Timeline 倒带
    private float highestTimeReached = 0f;

    void Start()
    {
        // 自动寻路：如果没拖面板，自动全图搜捕名为 "EndPanel" 的物体
        if (endScreenPanel == null)
        {
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (t.gameObject.scene.isLoaded && t.name.ToLower().Contains("endpanel"))
                {
                    endScreenPanel = t.gameObject;
                    Debug.Log("【PerformanceEndManager】自动抓取成功：找到了结束面板 " + t.name);
                    break;
                }
            }
        }

        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        if (manualExitButton != null)
            manualExitButton.onClick.AddListener(ReturnToMenu);

        if (endScreenReturnButton != null)
            endScreenReturnButton.onClick.AddListener(ReturnToMenu);
    }

    public void ActivatePerformanceMode()
    {
        if (musicSource == null)
        {
            TimelineManager tm = FindAnyObjectByType<TimelineManager>();
            if (tm != null && tm.musicSource != null) musicSource = tm.musicSource;
            else musicSource = FindAnyObjectByType<AudioSource>();
        }

        if (musicSource != null && musicSource.clip != null)
        {
            isPerformanceModeActive = true;
            hasTriggeredEnd = false;
            highestTimeReached = 0f;
            Debug.Log($"【PerformanceEndManager】演出模式已激活！目标音乐总时长: {musicSource.clip.length} 秒");
        }
        else
        {
            Debug.LogError("【PerformanceEndManager】激活失败！没有找到可用的 AudioSource！");
        }
    }

    void Update()
    {
        if (!isPerformanceModeActive || musicSource == null || musicSource.clip == null || hasTriggeredEnd)
            return;

        if (musicSource.time > highestTimeReached)
        {
            highestTimeReached = musicSource.time;
        }

        // 提前 0.3 秒拦截
        float triggerThreshold = musicSource.clip.length - 0.3f;

        if (musicSource.time >= triggerThreshold || highestTimeReached >= triggerThreshold)
        {
            Debug.Log($"【PerformanceEndManager】成功截获音乐结束信号！当前时间: {musicSource.time}, 最高记录时间: {highestTimeReached}");
            TriggerEndScreen();
        }
    }

    public void TriggerEndScreen()
    {
        hasTriggeredEnd = true;
        isPerformanceModeActive = false;

        if (endScreenPanel != null)
        {
            Debug.Log("【PerformanceEndManager】即将弹出结束面板！");
            endScreenPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("【PerformanceEndManager】警告：没找到 End Screen Panel，直接执行退回菜单操作！");
            ReturnToMenu();
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(menuSceneName))
        {
            Debug.Log($"【PerformanceEndManager】正在加载场景：{menuSceneName}");
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError("【PerformanceEndManager】报错：场景名字为空，无法跳转！");
        }
    }
}