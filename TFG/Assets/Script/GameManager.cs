using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ���� ��Դ�б� ��������������������������������������������������������������������������������������������
    [Header("=== Character ===")]
    public GameObject[] characterPrefabs;   // ��ɫ Prefab
    public string[] characterNames;         // ��ɫ���֣���ʾ�� UI �ϣ�
    public Sprite[] characterSprites;       // ��ɫԤ��ͼ������ѡ����棩
    public Sprite[] characterAvatars;

    [Header("=== Stage��Scene��===")]
    public string[] stageSceneNames;        // ����ʵ�� Build �������� Stage1, Stage2��
    public string[] stageDisplayNames;      // ������ʾ����
    public Sprite[] stageSprites;           // ����Ԥ��ͼ
    public Sprite[] stageAvatars;

    [Header("=== Music ===")]
    public AudioClip[] musicClips;          // ����Ƭ��
    public string[] musicNames;             // ��������
    public Sprite[] musicSprites;           // ר�����棨��ѡ��û�о����գ�
    public Sprite[] musicAvatars;

    [Header("=== Dance Animation ===")]
    public string[] danceStateNames;        // �赸����״̬��

    // ���� ��ҵ�ǰѡ��-1 = δѡ��������������������������������������������������������������
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

            // ���ؼ��޸�����ǿ���������ݣ���ֹ Unity �����ϴβ��Ե����ݵ���һ���־ͽ�����Ϸ
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
    // ����ȫ��ѡ�ò��ܽ����Ӧ����Ϸ����
    // ==========================================
    public bool AllSelected =>
        selectedCharIndex >= 0 &&
        selectedStageIndex >= 0 &&
        selectedMusicIndex >= 0;

    // ==========================================
    // �������ѡ�еĳ���
    // ==========================================
    public void LoadSelectedStage()
    {
        if (!AllSelected) return;

        // ��ȫ��飺ȷ�����������鷶Χ��
        if (selectedStageIndex >= 0 && selectedStageIndex < stageSceneNames.Length)
        {
            string targetSceneName = stageSceneNames[selectedStageIndex];
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("��������Խ���δ���ó�������");
        }
    }

    // ==========================================
    // ȡ��ĳ��ѡ��
    // ==========================================
    public void ClearCharacter() { selectedCharIndex = -1; }
    public void ClearStage() { selectedStageIndex = -1; }
    public void ClearMusic() { selectedMusicIndex = -1; }

    // ���� ��ȫ��ȡ���� ������������������������������������������������������������������������������������
    public string GetCharName(int i) => (characterNames != null && i >= 0 && i < characterNames.Length) ? characterNames[i] : $"Character {i + 1}";
    public string GetStageName(int i) => (stageDisplayNames != null && i >= 0 && i < stageDisplayNames.Length) ? stageDisplayNames[i] : $"Stage {i + 1}";
    public string GetMusicName(int i) => (musicNames != null && i >= 0 && i < musicNames.Length) ? musicNames[i] : $"Music {i + 1}";

    public Sprite GetCharSprite(int i) => (characterSprites != null && i >= 0 && i < characterSprites.Length) ? characterSprites[i] : null;
    public Sprite GetStageSprite(int i) => (stageSprites != null && i >= 0 && i < stageSprites.Length) ? stageSprites[i] : null;
    public Sprite GetMusicSprite(int i) => (musicSprites != null && i >= 0 && i < musicSprites.Length) ? musicSprites[i] : null;
    public AudioClip GetMusicClip(int i) => (musicClips != null && i >= 0 && i < musicClips.Length) ? musicClips[i] : null;

    public Sprite GetCharAvatar(int i) => (characterAvatars != null && i >= 0 && i < characterAvatars.Length) ? characterAvatars[i] : GetCharSprite(i);
    public Sprite GetStageAvatar(int i) => (stageAvatars != null && i >= 0 && i < stageAvatars.Length) ? stageAvatars[i] : GetStageSprite(i);
    public Sprite GetMusicAvatar(int i) => (musicAvatars != null && i >= 0 && i < musicAvatars.Length) ? musicAvatars[i] : GetMusicSprite(i);

    public int GetCharCount() => characterPrefabs?.Length ?? 0;
    public int GetStageCount() => stageSceneNames?.Length ?? 0;
    public int GetMusicCount() => musicClips?.Length ?? 0;
}