using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 绑定到画面2的"跳过"按钮
    public void LoadConcertScene()
    {
        // 加载 Index 2 (画面3)
        SceneManager.LoadScene(2);
    }

    // 以后这里会加更多代码，比如保存用户的自定义参数
}