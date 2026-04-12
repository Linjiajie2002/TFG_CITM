using UnityEngine;
using UnityEngine.UI;

// ==========================================
// 挂在全局 UI 控制台的某个独立面板或管理器上
// 用于全局切换“编辑视角”和“演出视角”的监控预览
// ==========================================
public class ChangeCameraView : MonoBehaviour
{
    [Header("=== 相机预览切换按钮 ===")]
    public Button editModeButton;             // 点击预览 EditCamera
    public Button playModeButton;             // 点击预览 PlayCamera

    [Header("=== 场景相机与 UI 渲染 ===")]
    public Camera editCamera;                 // 你的编辑相机
    public Camera playCamera;                 // 你的演出相机 (AudienceCamera)
    public RawImage previewRawImage;          // VJ面板上显示画面的 RawImage
    public RenderTexture editRenderTexture;   // EditCamera 用的 RenderTexture
    public RenderTexture playRenderTexture;   // PlayCamera 用的 RenderTexture

    private void Awake()
    {
        // 绑定相机切换按钮事件
        if (editModeButton != null) editModeButton.onClick.AddListener(SwitchToEditCamera);
        if (playModeButton != null) playModeButton.onClick.AddListener(SwitchToPlayCamera);
    }

    private void Start()
    {
        // 软件启动时，默认切到编辑相机预览
        SwitchToEditCamera();
    }

    // ==========================================
    // 切换到 EditCamera 预览
    // ==========================================
    public void SwitchToEditCamera()
    {
        if (editCamera != null)
        {
            editCamera.gameObject.SetActive(true);
            if (editRenderTexture != null) editCamera.targetTexture = editRenderTexture;
        }

        if (playCamera != null)
        {
            playCamera.gameObject.SetActive(false);
        }

        if (previewRawImage != null && editRenderTexture != null)
        {
            previewRawImage.texture = editRenderTexture;
        }

        Debug.Log("<color=cyan>[全局控制台] 切换至 EditCamera 预览</color>");
    }

    // ==========================================
    // 切换到 PlayCamera 预览
    // ==========================================
    public void SwitchToPlayCamera()
    {
        if (playCamera != null)
        {
            playCamera.gameObject.SetActive(true);
            // 在 UI 面板里看 PlayCamera，必须临时给它接上 RenderTexture
            if (playRenderTexture != null) playCamera.targetTexture = playRenderTexture;
        }

        if (editCamera != null)
        {
            editCamera.gameObject.SetActive(false);
        }

        if (previewRawImage != null && playRenderTexture != null)
        {
            previewRawImage.texture = playRenderTexture;
        }

        Debug.Log("<color=green>[全局控制台] 切换至 PlayCamera 演出视角预览</color>");
    }
}