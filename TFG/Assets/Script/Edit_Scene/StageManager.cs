using UnityEngine;
using UnityEngine.SceneManagement; // 用于场景跳转

public class StageManager : MonoBehaviour
{
    [Header("=== 1. 基础组件 (必填) ===")]
    public Transform spawnPoint;      // 角色生成的中心位置
    public AudioSource musicPlayer;   // 播放音乐的组件

    [Header("=== 2. 模式 A: 自定义/画面2 (必填) ===")]
    public GameObject customizationCanvas; // 编辑界面的UI (包含开始按钮)
    public Camera editorCamera;            // 编辑用的上帝视角相机

    [Header("=== 3. 模式 B: 演唱会/画面3 (必填) ===")]
    public GameObject concertCanvas;       // 演出时的UI (如果没有可以不填)
    public Camera audienceCamera;          // 观众席相机

    // --- 内部私有变量 ---
    private GameObject currentCharacter;   // 当前生成的角色
    private Animator charAnimator;         // 角色的动画控制器

    void Start()
    {
        // 1. 生成角色、准备音乐
        SetupContent();

        // 2. 【强制初始化】进入自定义模式
        // 这确保刚进去时，编辑相机是开的，观众相机是关的，防止冲突
        EnterCustomizationMode();
    }

    // --- 核心流程：生成内容 ---
    void SetupContent()
    {
        // 从 GameManager 获取玩家选择的索引
        int charIndex = GameManager.Instance.selectedCharIndex;
        int musicIndex = GameManager.Instance.selectedMusicIndex;

        // A. 生成角色
        if (GameManager.Instance.characterPrefabs.Length > charIndex)
        {
            GameObject prefab = GameManager.Instance.characterPrefabs[charIndex];

            // 在指定位置生成
            currentCharacter = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            // 获取角色身上的 Animator 组件
            charAnimator = currentCharacter.GetComponent<Animator>();

            // 如果Prefab没挂Animator，为了防止报错，加个Log提醒
            if (charAnimator == null)
            {
                Debug.LogError("错误：你的角色Prefab上没有 Animator 组件！无法跳舞！");
            }
        }

        // B. 准备音乐 (装载Clip，但暂不播放)
        if (GameManager.Instance.musicClips.Length > musicIndex)
        {
            musicPlayer.clip = GameManager.Instance.musicClips[musicIndex];
        }
    }

    // --- 模式切换：进入自定义/预览 ---
    public void EnterCustomizationMode()
    {
        // 1. UI 切换
        if (customizationCanvas != null) customizationCanvas.SetActive(true);
        if (concertCanvas != null) concertCanvas.SetActive(false);

        // 2. 摄像机切换 (解决冲突的关键)
        if (editorCamera != null) editorCamera.gameObject.SetActive(true);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);

        // 3. 停止音乐 (防止还没开始就响了)
        if (musicPlayer != null) musicPlayer.Stop();
    }

    // --- 模式切换：开始演唱会 (绑定给 Start 按钮) ---
    public void StartConcert()
    {
        // 1. UI 切换
        if (customizationCanvas != null) customizationCanvas.SetActive(false);
        if (concertCanvas != null) concertCanvas.SetActive(true);

        // 2. 摄像机切换
        if (editorCamera != null) editorCamera.gameObject.SetActive(false);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(true);

        // 3. 播放音乐 & 设定自动结束
        if (musicPlayer != null && musicPlayer.clip != null)
        {
            musicPlayer.Play();

            // 获取歌曲长度
            float songDuration = musicPlayer.clip.length;

            // 设定定时器：歌曲结束时自动调用 BackToMainMenu
            Invoke("BackToMainMenu", songDuration);

            Debug.Log($"演出开始！时长: {songDuration}秒");
        }

        // 4. 播放舞蹈 (使用 GameManager 里的名字)
        PlaySelectedDance();
    }

    // --- 辅助：播放舞蹈逻辑 ---
    void PlaySelectedDance()
    {
        if (charAnimator != null)
        {
            int musicIndex = GameManager.Instance.selectedMusicIndex;
            string[] danceNames = GameManager.Instance.danceStateNames;

            // 检查是否有对应的名字配置
            if (danceNames.Length > musicIndex)
            {
                string stateName = danceNames[musicIndex];

                // CrossFade 比 Play 更平滑，0.1f 是过渡时间
                charAnimator.CrossFade(stateName, 0.1f);

                Debug.Log($"尝试播放动作: {stateName}");
            }
            else
            {
                Debug.LogWarning("GameManager里没有配置对应的舞蹈状态名 (Dance State Names)！");
            }
        }
    }

    // --- 流程结束：返回菜单 ---
    public void BackToMainMenu()
    {
        // 取消所有定时器 (防止玩家手动点退出后，定时器还在跑)
        CancelInvoke();

        // 加载主菜单 (确保 Index 是 0)
        SceneManager.LoadScene(0);
    }
}