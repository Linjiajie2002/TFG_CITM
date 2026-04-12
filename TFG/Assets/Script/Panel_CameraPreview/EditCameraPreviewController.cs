using UnityEngine;
using UnityEngine.UI;

// ==========================================
// 用于存储机位信息的类
// ==========================================
[System.Serializable]
public class CameraWaypoint
{
    public string pointName = "机位";
    public Vector3 position;
    public Vector3 eulerAngles;
}

public class EditCameraPreviewController : MonoBehaviour
{
    [Header("=== 相机引用 ===")]
    public Transform cameraTransform;

    [Header("=== 控制按钮 ===")]
    public Button leftButton;         // 【修改】：点击切换下一个机位
    public Button rightButton;        // 【修改】：点击切换上一个机位

    [Header("=== 4个预设机位 (在此配置) ===")]
    [Tooltip("提示：在场景里摆好相机后，右键点击脚本组件头部，选择'保存到机位X'")]
    public CameraWaypoint[] waypoints = new CameraWaypoint[4];

    [Header("=== 丝滑过渡设置 ===")]
    public float lerpSpeed = 3f;      // 移动和旋转的丝滑程度（数值越大越快）

    private int currentIndex = 0;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (cameraTransform == null) return;

        // 【修改】：互换了 Left 和 Right 绑定的方法
        if (leftButton != null) leftButton.onClick.AddListener(GoToNextWaypoint);
        if (rightButton != null) rightButton.onClick.AddListener(GoToPreviousWaypoint);

        // 初始化目标点为第 1 个机位（如果有数据的话）
        if (waypoints.Length > 0)
        {
            currentIndex = 0;
            targetPosition = waypoints[0].position;
            targetRotation = Quaternion.Euler(waypoints[0].eulerAngles);

            // 游戏刚开始时，瞬间把相机传送到 1 号位，不进行过渡
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
        }
    }

    void Update()
    {
        if (cameraTransform == null || waypoints.Length == 0) return;

        // 核心 Lerp 逻辑：每一帧都让相机如丝般顺滑地向目标点靠近
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, Time.deltaTime * lerpSpeed);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    // ==========================================
    // 机位切换逻辑
    // ==========================================
    public void GoToNextWaypoint()
    {
        currentIndex++;
        if (currentIndex >= waypoints.Length) currentIndex = 0; // 循环回到第一个
        SetTargetToCurrentIndex();
    }

    public void GoToPreviousWaypoint()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = waypoints.Length - 1; // 循环回到最后一个
        SetTargetToCurrentIndex();
    }

    private void SetTargetToCurrentIndex()
    {
        targetPosition = waypoints[currentIndex].position;
        targetRotation = Quaternion.Euler(waypoints[currentIndex].eulerAngles);
    }


    // ==========================================
    // 💡 开发者黑科技：右键菜单一键保存机位！
    // ==========================================
    private void SaveCurrentToSlot(int index)
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("请先在 Inspector 里拖入 Camera Transform！");
            return;
        }
        waypoints[index].position = cameraTransform.position;
        waypoints[index].eulerAngles = cameraTransform.eulerAngles;
        Debug.Log($"<color=green>✅ 已成功将相机当前位置保存到机位 {index + 1}！</color>");
    }

    [ContextMenu("📸 记录当前视角 -> 保存到【机位 1】")]
    void SaveSlot1() { SaveCurrentToSlot(0); }

    [ContextMenu("📸 记录当前视角 -> 保存到【机位 2】")]
    void SaveSlot2() { SaveCurrentToSlot(1); }

    [ContextMenu("📸 记录当前视角 -> 保存到【机位 3】")]
    void SaveSlot3() { SaveCurrentToSlot(2); }

    [ContextMenu("📸 记录当前视角 -> 保存到【机位 4】")]
    void SaveSlot4() { SaveCurrentToSlot(3); }
}