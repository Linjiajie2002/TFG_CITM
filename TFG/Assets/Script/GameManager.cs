using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── 资源列表 ──────────────────────────────────────────────
    [Header("=== Character ===")]
    public GameObject[] characterPrefabs;   // 角色 Prefab
    public string[] characterNames;         // 角色名字（显示在 UI 上）
    public Sprite[] characterSprites;       // 角色预览图（用于选择界面）
    public Sprite[] characterAvatars;

    [Header("=== Stage（Scene）===")]
    public string[] stageSceneNames;        // 场景实际 Build 名（例如 Stage1, Stage2）
    public string[] stageDisplayNames;      // 场景显示名字
    public Sprite[] stageSprites;           // 场景预览图
    public Sprite[] stageAvatars;

    [Header("=== Music ===")]
    public AudioClip[] musicClips;          // 音乐片段
    public string[] musicNames;             // 音乐名字
    public Sprite[] musicSprites;           // 专辑封面（可选，没有就留空）
    public Sprite[] musicAvatars;

    [Header("=== Dance Animation ===")]
    public string[] danceStateNames;        // 舞蹈动画状态名

    // ── 玩家当前选择（-1 = 未选）──────────────────────────────
    [HideInInspector] public int selectedCharIndex = -1;
    [HideInInspector] public int selectedStageIndex = -1;
    [HideInInspector] public int selectedMusicIndex = -1;

    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 【关键修复】：强制重置数据，防止 Unity 缓存上次测试的数据导致一开局就进入游戏
            selectedCharIndex = -1;
            selectedStageIndex = -1;
            selectedMusicIndex = -1;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // 三项全部选好才能进入对应的游戏场景
    // ==========================================
    public bool AllSelected =>
        selectedCharIndex >= 0 &&
        selectedStageIndex >= 0 &&
        selectedMusicIndex >= 0;

    // ==========================================
    // 进入玩家选中的场景
    // ==========================================
    public void LoadSelectedStage()
    {
        if (!AllSelected) return;

        // 安全检查：确保索引在数组范围内
        if (selectedStageIndex >= 0 && selectedStageIndex < stageSceneNames.Length)
        {
            string targetSceneName = stageSceneNames[selectedStageIndex];
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("场景索引越界或未配置场景名！");
        }
    }

    // ==========================================
    // 取消某项选择
    // ==========================================
    public void ClearCharacter() { selectedCharIndex = -1; }
    public void ClearStage() { selectedStageIndex = -1; }
    public void ClearMusic() { selectedMusicIndex = -1; }

    // ── 安全获取数据 ──────────────────────────────────────────
    public string GetCharName(int i) => (characterNames != null && i >= 0 && i < characterNames.Length) ? characterNames[i] : $"Character {i + 1}";
    public string GetStageName(int i) => (stageDisplayNames != null && i >= 0 && i < stageDisplayNames.Length) ? stageDisplayNames[i] : $"Stage {i + 1}";
    public string GetMusicName(int i) => (musicNames != null && i >= 0 && i < musicNames.Length) ? musicNames[i] : $"Music {i + 1}";

    public Sprite GetCharSprite(int i) => (characterSprites != null && i >= 0 && i < characterSprites.Length) ? characterSprites[i] : null;
    public Sprite GetStageSprite(int i) => (stageSprites != null && i >= 0 && i < stageSprites.Length) ? stageSprites[i] : null;
    public Sprite GetMusicSprite(int i) => (musicSprites != null && i >= 0 && i < musicSprites.Length) ? musicSprites[i] : null;

    public Sprite GetCharAvatar(int i) => (characterAvatars != null && i >= 0 && i < characterAvatars.Length) ? characterAvatars[i] : GetCharSprite(i);
    public Sprite GetStageAvatar(int i) => (stageAvatars != null && i >= 0 && i < stageAvatars.Length) ? stageAvatars[i] : GetStageSprite(i);
    public Sprite GetMusicAvatar(int i) => (musicAvatars != null && i >= 0 && i < musicAvatars.Length) ? musicAvatars[i] : GetMusicSprite(i);

    public int GetCharCount() => characterPrefabs?.Length ?? 0;
    public int GetStageCount() => stageSceneNames?.Length ?? 0;
    public int GetMusicCount() => musicClips?.Length ?? 0;
}