using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 硬切摄像头播放引擎
//
// 支持观众模式：当 audienceMode = true 时，
// 不跟随任何 Camera Clip，保持在默认位置
// ==========================================
public class CameraPlaybackSystem : MonoBehaviour
{
    [Header("=== 摄像头引用 ===")]
    [Tooltip("Edit 模式 / 纯享受模式下使用的摄像头")]
    public Camera editCamera;

    [Tooltip("纯享受模式下控制的摄像头（随 Camera Clip 移动）")]
    public Camera playCamera;

    [Header("=== 默认位置（无 Clip 时 / 观众模式时使用）===")]
    public Vector3 defaultPosition = new Vector3(0f, 3f, -8f);
    public Vector3 defaultRotation = new Vector3(10f, 0f, 0f);

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Camera 轨道名 ===")]
    public string cameraTrackName = "Camera";

    [Header("=== Edit 模式预览 ===")]
    [Tooltip("开启后，Scrub 进 Clip 时 editCamera 也跟着移动（方便预览）")]
    public bool previewOnEditCamera = false;

    // ── 内部 ──
    private bool isPlaying = false;
    private bool audienceMode = false;   // true = 观众模式，忽略所有 Clip
    private float lastCheckedTime = -999f;

    // ==========================================
    void Start()
    {
        ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
    }

    void Update()
    {
        if (timeline == null) return;

        bool nowPlaying = timeline.musicSource != null && timeline.musicSource.isPlaying;

        if (nowPlaying != isPlaying)
        {
            isPlaying = nowPlaying;
            if (!isPlaying)
            {
                // 停止时归位
                ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
                lastCheckedTime = -999f;
            }
        }

        float currentTime = timeline.GetCurrentTime();

        if (Mathf.Abs(currentTime - lastCheckedTime) > 0.016f || isPlaying)
        {
            lastCheckedTime = currentTime;
            TickCamera(currentTime);
        }
    }

    // ==========================================
    private void TickCamera(float currentTime)
    {
        // 观众模式：永远用默认位置，不跟 Clip 走
        if (audienceMode)
        {
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
            return;
        }

        CameraClipData active = FindActiveClipData(currentTime);

        if (active != null)
        {
            ApplyToPlayCamera(active.Position, active.Rotation);

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, active.Position, active.Rotation);
        }
        else
        {
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, defaultPosition, Quaternion.Euler(defaultRotation));
        }
    }

    // ==========================================
    private CameraClipData FindActiveClipData(float currentTime)
    {
        if (timeline.allEvents == null || timeline.allTracks == null) return null;

        var cameraIndices = new HashSet<int>();
        foreach (var track in timeline.allTracks)
            if (track.trackName == cameraTrackName) cameraIndices.Add(track.trackIndex);

        CameraClipData found = null;
        float bestStart = -1f;

        foreach (var evt in timeline.allEvents)
        {
            if (!cameraIndices.Contains(evt.trackIndex)) continue;
            if (!(evt.customData is CameraClipData data)) continue;

            float end = evt.startTime + evt.duration;
            if (currentTime >= evt.startTime && currentTime < end && evt.startTime > bestStart)
            {
                bestStart = evt.startTime;
                found = data;
            }
        }
        return found;
    }

    // ==========================================
    private void ApplyToPlayCamera(Vector3 pos, Quaternion rot)
    {
        if (playCamera == null) return;
        playCamera.transform.position = pos;
        playCamera.transform.rotation = rot;
    }

    private void ApplyToCamera(Camera cam, Vector3 pos, Quaternion rot)
    {
        if (cam == null) return;
        cam.transform.position = pos;
        cam.transform.rotation = rot;
    }

    // ==========================================
    // 由 AudienceModeSystem 调用
    // ==========================================

    /// <summary>true = 观众模式，摄像头固定在默认位置</summary>
    public void SetAudienceMode(bool value)
    {
        audienceMode = value;
        if (audienceMode)
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
    }

    /// <summary>强制本帧立即重新计算摄像头（C 键切换后调用）</summary>
    public void ForceRefresh() { lastCheckedTime = -999f; }

    // 其他外部接口（保持兼容）
    public void PreviewInEditMode(CameraClipData data)
    {
        if (isPlaying || editCamera == null || data == null) return;
        editCamera.transform.position = data.Position;
        editCamera.transform.rotation = data.Rotation;
    }

    public void ResetEditCameraToDefault()
    {
        if (isPlaying || editCamera == null) return;
        editCamera.transform.position = defaultPosition;
        editCamera.transform.rotation = Quaternion.Euler(defaultRotation);
    }
}