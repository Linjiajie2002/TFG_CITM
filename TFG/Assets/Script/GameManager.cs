using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Source")]
    public GameObject[] characterPrefabs; // Character List
    public AudioClip[] musicClips;        // Music List

    [Header("Stage")]
    public string[] stageSceneNames;

    [Header("Player Select")]
    public int selectedCharIndex = 0;
    public int selectedStageIndex = 0;
    public int selectedMusicIndex = 0;

    void Awake()
    {
        // GameManager Instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // If back to Menu Scene, destroy duplicate GameManager
            Destroy(gameObject);
        }
    }
}