using UnityEngine;
using UnityEngine.UI;

public class TogglePrefabButton : MonoBehaviour
{
    public Button button;
    public GameObject target;

    void Start()
    {
        button.onClick.AddListener(() => target.SetActive(!target.activeSelf));
    }
}