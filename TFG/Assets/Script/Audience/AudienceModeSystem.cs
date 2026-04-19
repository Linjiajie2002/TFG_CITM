using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AudienceModeSystem : MonoBehaviour
{
    public enum Mode { Idle, PureEnjoy, Audience }
    [HideInInspector] public Mode currentMode = Mode.Idle;

    [Header("=== 时间轴 ===")]
    public TimelineManager timeline;

    [Header("=== 摄像头 ===")]
    public Camera playerCamera;
    public Camera audiencesCamera;

    [Header("=== Camera 轨道名 ===")]
    public string cameraTrackName = "Camera";

    [Header("=== 弹窗（有 Camera Clip 时显示）===")]
    public GameObject popup;
    public Button btnPureEnjoy;
    public Button btnAudience;

    [Header("=== 纯享受模式 ===")]
    public GameObject pureEnjoyHint;

    [Header("=== 观众模式 ===")]
    public GameObject audienceOverlay;
    public CheerStickController cheerStick;

    [Header("=== 编辑 UI 根节点 ===")]
    public GameObject editUIRoot;
    public CanvasGroup editUICanvasGroup;

    // ── 内部标志 ──
    private bool wasPlaying = false;
    private bool hasCameraClips = false;
    private bool modeChosen = false;
    private bool isPausedForPopup = false;

    // 正式演出标志
    private bool isOfficialConcert = false;
    private RenderTexture defaultPlayerCamTex;

    void Start()
    {
        if (playerCamera != null)
            defaultPlayerCamTex = playerCamera.targetTexture;

        if (btnPureEnjoy != null) btnPureEnjoy.onClick.AddListener(OnClickPureEnjoy);
        if (btnAudience != null) btnAudience.onClick.AddListener(OnClickAudience);

        HidePopup();
        ApplyMode(Mode.Idle);
    }

    void Update()
    {
        if (timeline == null) return;
        bool isAudioPlaying = timeline.musicSource != null && timeline.musicSource.isPlaying;

        // ==========================================
        // 🛡️ 【核心隔离墙】：Edit 模式下，保安直接下班！
        // ==========================================
        if (!isOfficialConcert)
        {
            // Edit 模式下，允许你按 C 键预览机位（如果不按，就什么都不会发生）
            if (isAudioPlaying && CheckHasCameraClips())
            {
                if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                    ToggleMode();
            }

            wasPlaying = isAudioPlaying;
            return; // 🚨 绝对不往下走！Edit 模式的播放绝不弹窗、不暂停、不变灰！
        }

        // ==========================================
        // 以下全是【正式演出】的专属拦截与监听逻辑
        // ==========================================

        // 正式演出中途，音乐停止了（播完结束）
        if (!isAudioPlaying && wasPlaying)
        {
            if (!isPausedForPopup)
                StopEverything();
        }

        // C 键切换机位
        if (isAudioPlaying && modeChosen && hasCameraClips)
        {
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                ToggleMode();
        }

        wasPlaying = isAudioPlaying;
    }

    // ==========================================
    // 开始正式演出（由 StageManager 呼叫）
    // ==========================================
    public void PlayAsConcert()
    {
        isOfficialConcert = true;
        modeChosen = false;
        isPausedForPopup = false;
        hasCameraClips = CheckHasCameraClips();

        HidePopup();
        GrayOutEditUI(false);

        if (playerCamera != null)
            playerCamera.targetTexture = null;

        if (hasCameraClips)
        {
            // 有 CameraClip：屏息等待，弹窗！
            isPausedForPopup = true;
            if (timeline.musicSource != null)
            {
                timeline.musicSource.time = 0f;
                timeline.musicSource.Pause();
            }
            if (timeline.playheadSlider != null) timeline.playheadSlider.SetValueWithoutNotify(0f);

            ShowPopup();
        }
        else
        {
            // 无 CameraClip：直接全屏观众模式起飞！
            modeChosen = true;
            ApplyMode(Mode.Audience);

            if (timeline.musicSource != null)
            {
                timeline.musicSource.time = 0f;
                timeline.musicSource.Play();
            }
            wasPlaying = true;
        }
    }

    // ==========================================
    // 退出正式演出，退回捏人/Edit界面
    // ==========================================
    public void StopEverything()
    {
        isOfficialConcert = false;
        modeChosen = false;
        hasCameraClips = false;
        isPausedForPopup = false;

        if (timeline != null && timeline.musicSource != null)
            timeline.musicSource.Stop();

        ApplyMode(Mode.Idle);

        if (playerCamera != null)
            playerCamera.targetTexture = defaultPlayerCamTex;

        HidePopup();
        GrayOutEditUI(false);
    }

    // ==========================================
    // 弹窗按钮回调
    // ==========================================
    public void OnClickPureEnjoy()
    {
        HidePopup();
        modeChosen = true;
        isPausedForPopup = false;
        ApplyMode(Mode.PureEnjoy);
        ResumePlay();
    }

    public void OnClickAudience()
    {
        HidePopup();
        modeChosen = true;
        isPausedForPopup = false;
        ApplyMode(Mode.Audience);
        ResumePlay();
    }

    // ==========================================
    // 工具方法
    // ==========================================
    private void ToggleMode()
    {
        ApplyMode(currentMode == Mode.PureEnjoy ? Mode.Audience : Mode.PureEnjoy);
    }

    private void ApplyMode(Mode mode)
    {
        currentMode = mode;
        bool isPure = (mode == Mode.PureEnjoy);
        bool isAudience = (mode == Mode.Audience);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(!isAudience);
            if (isOfficialConcert && !isAudience)
                playerCamera.targetTexture = null;
        }

        if (audiencesCamera != null)
        {
            audiencesCamera.gameObject.SetActive(isAudience);
            if (isAudience) audiencesCamera.targetTexture = null;
        }

        if (pureEnjoyHint != null) pureEnjoyHint.SetActive(isPure && hasCameraClips);
        if (audienceOverlay != null) audienceOverlay.SetActive(isAudience);
        if (cheerStick != null) cheerStick.SetActive(isAudience);
    }

    private void GrayOutEditUI(bool isGray)
    {
        if (editUICanvasGroup != null)
        {
            editUICanvasGroup.alpha = isGray ? 0.5f : 1f;
            editUICanvasGroup.interactable = !isGray;
            editUICanvasGroup.blocksRaycasts = !isGray;
        }
        else if (editUIRoot != null)
        {
            editUIRoot.SetActive(!isGray);
        }
    }

    private void ResumePlay()
    {
        wasPlaying = true;
        if (timeline.musicSource != null)
            timeline.musicSource.Play();
    }

    private void ShowPopup() { if (popup != null) popup.SetActive(true); }
    private void HidePopup() { if (popup != null) popup.SetActive(false); }

    private bool CheckHasCameraClips()
    {
        if (timeline?.allEvents == null || timeline?.allTracks == null) return false;
        foreach (var track in timeline.allTracks)
        {
            if (track.trackName != cameraTrackName) continue;
            foreach (var evt in timeline.allEvents)
                if (evt.trackIndex == track.trackIndex && evt.customData is CameraClipData)
                    return true;
        }
        return false;
    }
}