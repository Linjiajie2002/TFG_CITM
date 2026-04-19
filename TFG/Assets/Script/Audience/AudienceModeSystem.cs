using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// ==========================================
// 观众模式系统 (完美修复版)
// ==========================================
public class AudienceModeSystem : MonoBehaviour
{
    public enum Mode { Idle, PureEnjoy, Audience }
    [HideInInspector] public Mode currentMode = Mode.Idle;

    // ─── 引用 ────────────────────────────────────────────────
    [Header("=== 时间轴 ===")]
    public TimelineManager timeline;

    [Header("=== 摄像头 ===")]
    [Tooltip("纯享受模式 / 编辑模式 使用的摄像头")]
    public Camera playerCamera;
    [Tooltip("观众模式专用摄像头（直接 SetActive 切换）")]
    public Camera audiencesCamera;

    [Header("=== Camera 轨道名 ===")]
    public string cameraTrackName = "Camera";

    // ─── 弹窗 ────────────────────────────────────────────────
    [Header("=== 弹窗（有 Camera Clip 时显示）===")]
    public GameObject popup;
    public Button btnPureEnjoy;
    public Button btnAudience;

    // ─── UI ──────────────────────────────────────────────────
    [Header("=== 纯享受模式 ===")]
    [Tooltip("屏幕中央的提示文字（'按C切换'）")]
    public GameObject pureEnjoyHint;

    [Header("=== 观众模式 ===")]
    public GameObject audienceOverlay;
    public CheerStickController cheerStick;

    [Header("=== 编辑 UI 根节点 ===")]
    public GameObject editUIRoot;

    // ─── 内部标志 ────────────────────────────────────────────
    private bool wasPlaying = false;
    private bool hasCameraClips = false;
    private bool modeChosen = false;

    // 【核心修复锁】：标记是否正因为弹窗而处于暂停状态
    private bool isPausedForPopup = false;

    // ==========================================
    void Start()
    {
        if (btnPureEnjoy != null) btnPureEnjoy.onClick.AddListener(OnClickPureEnjoy);
        if (btnAudience != null) btnAudience.onClick.AddListener(OnClickAudience);

        HidePopup();
        ApplyMode(Mode.Idle);
    }

    // ==========================================
    void Update()
    {
        if (timeline == null) return;

        bool isAudioPlaying = timeline.musicSource != null && timeline.musicSource.isPlaying;

        // ── 1. 演出刚开始，且本次还没选过模式 ──
        if (isAudioPlaying && !wasPlaying && !modeChosen)
        {
            OnPlayStarted();
        }

        // ── 2. 演出停止 ──
        if (!isAudioPlaying && wasPlaying)
        {
            // 【核心修复】：如果是弹窗强行按下的暂停，绝对不能当成演出停止！
            if (!isPausedForPopup)
            {
                OnPlayStopped();
            }
        }

        // 记录本帧状态供下一帧对比
        wasPlaying = isAudioPlaying;

        // ── 3. C 键切换 ──
        // 条件：正在播放中 + 已经选完模式 + 当前歌曲确实有镜头数据
        if (isAudioPlaying && modeChosen && hasCameraClips)
        {
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                ToggleMode();
            }
        }
    }

    // ==========================================
    // 演出开始
    // ==========================================
    private void OnPlayStarted()
    {
        hasCameraClips = CheckHasCameraClips();

        if (editUIRoot != null) editUIRoot.SetActive(false);

        if (hasCameraClips)
        {
            // 有摄像头 Clip → 锁定状态，暂停，弹窗
            isPausedForPopup = true;
            PauseAndFreeze();
            ShowPopup();
        }
        else
        {
            // 没有摄像头 Clip → 直接进入观众模式（无需暂停）
            modeChosen = true;
            isPausedForPopup = false;
            ApplyMode(Mode.Audience);
        }
    }

    // ==========================================
    // 演出停止
    // ==========================================
    private void OnPlayStopped()
    {
        // 彻底清理状态，为下次播放做准备
        modeChosen = false;
        hasCameraClips = false;
        isPausedForPopup = false;

        ApplyMode(Mode.Idle);
        HidePopup();
        if (editUIRoot != null) editUIRoot.SetActive(true);
    }

    // ==========================================
    // 弹窗按钮回调
    // ==========================================
    public void OnClickPureEnjoy()
    {
        HidePopup();
        modeChosen = true;
        isPausedForPopup = false; // 解除弹窗暂停锁定
        ApplyMode(Mode.PureEnjoy);
        ResumePlay();
    }

    public void OnClickAudience()
    {
        HidePopup();
        modeChosen = true;
        isPausedForPopup = false; // 解除弹窗暂停锁定
        ApplyMode(Mode.Audience);
        ResumePlay();
    }

    // ==========================================
    // 模式切换执行
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

        // ── 摄像头：直接 SetActive 切断 ──
        if (playerCamera != null) playerCamera.gameObject.SetActive(!isAudience);
        if (audiencesCamera != null) audiencesCamera.gameObject.SetActive(isAudience);

        // ── 提示与 UI ──
        if (pureEnjoyHint != null) pureEnjoyHint.SetActive(isPure && hasCameraClips);
        if (audienceOverlay != null) audienceOverlay.SetActive(isAudience);
        if (cheerStick != null) cheerStick.SetActive(isAudience);
    }

    // ==========================================
    // 辅助工具
    // ==========================================
    private void PauseAndFreeze()
    {
        if (timeline.musicSource != null)
        {
            timeline.musicSource.Pause();
            timeline.musicSource.time = 0f;
        }
        if (timeline.playheadSlider != null)
            timeline.playheadSlider.SetValueWithoutNotify(0f);
    }

    private void ResumePlay()
    {
        // 强制同步状态，防止下一帧出现时序错乱
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