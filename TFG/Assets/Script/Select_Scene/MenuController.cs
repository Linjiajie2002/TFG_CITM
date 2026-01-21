using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    //SetCharacterIndex
    public void SetCharacterIndex(int index)
    {
        GameManager.Instance.selectedCharIndex = index;
        Debug.Log("PickCharacter: " + index);
    }

    // SetStageIndex
    public void SetStageIndex(int index)
    {
        GameManager.Instance.selectedStageIndex = index;
        Debug.Log("PickScene: " + index);
    }

    // Music Index
    public void SetMusicIndex(int index)
    {
        GameManager.Instance.selectedMusicIndex = index;
        Debug.Log("PickMusic: " + index);
    }

    // Next Button Click
    public void OnNextButtonClick()
    {
        // Get selected stage index
        int stageIndex = GameManager.Instance.selectedStageIndex;

        // Setup scene name and load
        if (GameManager.Instance.stageSceneNames.Length > stageIndex)
        {
            string sceneName = GameManager.Instance.stageSceneNames[stageIndex];
            //Load Scene
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Error£ºGameManager no have stage name£¡");
        }
    }
}