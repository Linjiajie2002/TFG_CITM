using UnityEngine;
using UnityEngine.SceneManagement; 

public class StageManager : MonoBehaviour
{
    [Header("=== Basic component binding ===")]
    public Transform spawnPoint;      // Character generate position
    public AudioSource musicPlayer;   // AudioSource 

    [Header("=== Mode A: Custom/Screen 2 ===")]
    public GameObject customizationCanvas; // Edoit UI Canvas
    public Camera editorCamera;            // God Editor Camera

    [Header("=== Mode B: Concert/Screen 3 ===")]
    public GameObject concertCanvas;       // Play UI Canvas
    public Camera audienceCamera;          // Presenter Camera


    private GameObject currentCharacter;

    void Start()
    {
        
        SetupContent();
        EnterCustomizationMode();
    }



    void SetupContent()
    {
        // Picked indices
        int charIndex = GameManager.Instance.selectedCharIndex;
        int musicIndex = GameManager.Instance.selectedMusicIndex;

        // Generate character
        if (GameManager.Instance.characterPrefabs.Length > charIndex)
        {
            GameObject prefab = GameManager.Instance.characterPrefabs[charIndex];
            currentCharacter = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        }

        // Setup music
        if (GameManager.Instance.musicClips.Length > musicIndex)
        {
            musicPlayer.clip = GameManager.Instance.musicClips[musicIndex];
           
        }
    }

    // Edit Mode
    public void EnterCustomizationMode()
    {
        // UI change
        if (customizationCanvas != null) customizationCanvas.SetActive(true);
        if (concertCanvas != null) concertCanvas.SetActive(false);

        // Camera change
        if (editorCamera != null) editorCamera.gameObject.SetActive(true);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(false);

        // stop music
        if (musicPlayer != null) musicPlayer.Stop();
    }

    //Concert Mode
    public void StartConcert()
    {
        // UI change
        if (customizationCanvas != null) customizationCanvas.SetActive(false);
        if (concertCanvas != null) concertCanvas.SetActive(true);

        // camera change
        if (editorCamera != null) editorCamera.gameObject.SetActive(false);
        if (audienceCamera != null) audienceCamera.gameObject.SetActive(true);

        // Play music
        if (musicPlayer != null && musicPlayer.clip != null)
        {
            musicPlayer.Play();

            // Get music duration
            float songDuration = musicPlayer.clip.length;

            //We schedule return to main menu after music ends
            Invoke("BackToMainMenu", songDuration);

            Debug.Log($"The show is about to begin! {songDuration} automatically return to the main menu in seconds¡£");
        }
        else
        {
            Debug.LogWarning("Note: Without a music clip, the end time cannot be calculated automatically£¡");
        }
    }

    // Force back to main menu
    public void BackToMainMenu()
    {
        CancelInvoke();
        SceneManager.LoadScene(0);
    }
}