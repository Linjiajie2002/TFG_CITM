using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 硬切摄像头播放引擎 (修复版)
//
// 修复内容：
//   1. Scrub 模式（非播放时拖进度条）也实时更新摄像头
//   2. 不强制 SetActive 摄像头，避免干扰用户的双视角设置
//   3. 摄像头每帧都检查当前时间对应的 Clip，及时归位到默认
// ==========================================
public class CameraPlaybackSystem : MonoBehaviour
{
    [Header("=== 摄像头引用 ===")]
    [Tooltip("Edit 模式下的场景观察摄像头（本系统在 Edit 时不会移动它）")]
    public Camera editCamera;

    [Tooltip("演出播放时使用的摄像头（本系统始终控制它的 position/rotation）")]
    public Camera playCamera;

    [Header("=== 默认摄像头位置（无 Clip 覆盖时使用）===")]
    public Vector3 defaultPosition = new Vector3(0f, 3f, -8f);
    public Vector3 defaultRotation = new Vector3(10f, 0f, 0f);

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Camera 轨道名（必须与 AddModule 时填的名字一致）===")]
    public string cameraTrackName = "Camera";

    [Header("=== 是否在 Edit 预览时也移动 editCamera ===")]
    [Tooltip("开启后，scrub 到 clip 内时 editCamera 也会移到 clip 位置方便预览")]
    public bool previewOnEditCamera = false;

    // ── 内部状态 ──
    private bool isPlaying = false;
    private float lastCheckedTime = -999f;

    void Start()
    {
        ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
    }

    void Update()
    {
        if (timeline == null) return;

        bool nowPlaying = (timeline.musicSource != null && timeline.musicSource.isPlaying);

        // 播放状态切换
        if (nowPlaying != isPlaying)
        {
            isPlaying = nowPlaying;

            if (!isPlaying)
            {
                // 停止演出时立即归位 playCamera
                ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
                lastCheckedTime = -999f; // 强制下一帧重新计算
            }
        }

        float currentTime = timeline.GetCurrentTime();

        // ── 每帧都检查（播放 和 scrub 都走这里）──
        // 用 0.016f 的阈值避免浮点抖动导致每帧都重新设置摄像头
        if (Mathf.Abs(currentTime - lastCheckedTime) > 0.016f || isPlaying)
        {
            lastCheckedTime = currentTime;
            TickCamera(currentTime);
        }
    }

    // ==========================================
    // 核心：根据当前时间决定摄像头位置
    // ==========================================
    private void TickCamera(float currentTime)
    {
        CameraClipData active = FindActiveClipData(currentTime);

        if (active != null)
        {
            // 在 Clip 区间内：使用 Clip 的摄像头参数（硬切）
            ApplyToPlayCamera(active.Position, active.Rotation);

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, active.Position, active.Rotation);
        }
        else
        {
            // 不在任何 Clip 内：回到默认
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, defaultPosition, Quaternion.Euler(defaultRotation));
        }
    }

    // ==========================================
    // 在所有 Camera 轨道的 Clip 里找当前时间激活的
    // ==========================================
    private CameraClipData FindActiveClipData(float currentTime)
    {
        if (timeline.allEvents == null || timeline.allTracks == null) return null;

        // 找所有 camera 轨道的 trackIndex
        HashSet<int> cameraTrackIndices = new HashSet<int>();
        foreach (var track in timeline.allTracks)
        {
            if (track.trackName == cameraTrackName)
                cameraTrackIndices.Add(track.trackIndex);
        }

        // 找当前时间落在哪个 Clip 内（多个重叠时取 startTime 最晚的）
        CameraClipData found = null;
        float bestStart = -1f;

        foreach (var evt in timeline.allEvents)
        {
            if (!cameraTrackIndices.Contains(evt.trackIndex)) continue;
            if (!(evt.customData is CameraClipData data)) continue;

            float clipEnd = evt.startTime + evt.duration;
            if (currentTime >= evt.startTime && currentTime < clipEnd)
            {
                if (evt.startTime > bestStart)
                {
                    bestStart = evt.startTime;
                    found = data;
                }
            }
        }

        return found;
    }

    // ==========================================
    // 摄像头操作辅助
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
    // 供外部（播放/停止按钮）调用，可选
    // ==========================================
    public void OnPlayStarted()
    {
        isPlaying = true;
        lastCheckedTime = -999f;
    }

    public void OnPlayStopped()
    {
        isPlaying = false;
        ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
        lastCheckedTime = -999f;
    }

    // Edit 模式：选中 clip 时从面板调用，把 editCamera 移过去预览
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