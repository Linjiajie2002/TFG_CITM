using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 全能摄像机播放引擎
// 同时支持同一个轨道内的“硬切(CameraClipData)”和“平滑(SmoothCameraClipData)”
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
    public string cameraTrackName = "Camera"; // 保持叫 Camera 即可

    [Header("=== Edit 模式预览 ===")]
    [Tooltip("开启后，Scrub 进 Clip 时 editCamera 也跟着移动（方便预览）")]
    public bool previewOnEditCamera = false;

    // ── 内部 ──
    private bool isPlaying = false;
    private bool audienceMode = false;   // true = 观众模式，忽略所有 Clip
    private float lastCheckedTime = -999f;

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

        // 统一获取当前时间激活的方块
        TimelineEventData activeEvt = FindActiveClipEvent(currentTime);

        if (activeEvt != null)
        {
            Vector3 targetPos = defaultPosition;
            Quaternion targetRot = Quaternion.Euler(defaultRotation);

            // 🌟 核心分流：看看这个方块绑定的数据是哪种类型
            if (activeEvt.customData is CameraClipData hardData)
            {
                // 【情况 A】遇到了旧版的硬切数据
                targetPos = hardData.Position;
                targetRot = hardData.Rotation;
            }
            else if (activeEvt.customData is SmoothCameraClipData smoothData)
            {
                // 计算当前在 Clip 中的进度 (0.0 ~ 1.0)
                float progress = (currentTime - activeEvt.startTime) / activeEvt.duration;
                progress = Mathf.Clamp01(progress);

                if (smoothData.useMidPoint)
                {
                    // ==================================================
                    // 开启了中间点：使用贝塞尔曲线 (De Casteljau's algorithm)
                    // 呈现出如同电影一般的完美弧线过渡，拒绝僵硬折线！
                    // ==================================================

                    // 1. 位置贝塞尔插值
                    Vector3 p0 = smoothData.point1.Position;
                    Vector3 p1 = smoothData.midPoint.Position;
                    Vector3 p2 = smoothData.point2.Position;

                    Vector3 p01 = Vector3.Lerp(p0, p1, progress);
                    Vector3 p12 = Vector3.Lerp(p1, p2, progress);
                    targetPos = Vector3.Lerp(p01, p12, progress);

                    // 2. 旋转球面贝塞尔插值 (完美丝滑的角度跟拍)
                    Quaternion q0 = smoothData.point1.Rotation;
                    Quaternion q1 = smoothData.midPoint.Rotation;
                    Quaternion q2 = smoothData.point2.Rotation;

                    Quaternion q01 = Quaternion.Slerp(q0, q1, progress);
                    Quaternion q12 = Quaternion.Slerp(q1, q2, progress);
                    targetRot = Quaternion.Slerp(q01, q12, progress);
                }
                else
                {
                    // ==================================================
                    // 没有开启中间点：普通的直线匀速移动
                    // ==================================================
                    targetPos = Vector3.Lerp(smoothData.point1.Position, smoothData.point2.Position, progress);
                    targetRot = Quaternion.Lerp(smoothData.point1.Rotation, smoothData.point2.Rotation, progress);
                }
            }

            // 应用计算出来的坐标和旋转
            ApplyToPlayCamera(targetPos, targetRot);

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, targetPos, targetRot);
        }
        else
        {
            // 轨道上当前时间点没有任何方块，归位
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, defaultPosition, Quaternion.Euler(defaultRotation));
        }
    }

    // ==========================================
    // 寻找当前时间下正在生效的 Clip Event
    // ==========================================
    private TimelineEventData FindActiveClipEvent(float currentTime)
    {
        if (timeline.allEvents == null || timeline.allTracks == null) return null;

        var cameraIndices = new HashSet<int>();
        foreach (var track in timeline.allTracks)
        {
            if (track.trackName == cameraTrackName) cameraIndices.Add(track.trackIndex);
        }

        TimelineEventData foundEvent = null;
        float bestStart = -1f;

        foreach (var evt in timeline.allEvents)
        {
            if (!cameraIndices.Contains(evt.trackIndex)) continue;

            float end = evt.startTime + evt.duration;
            if (currentTime >= evt.startTime && currentTime < end && evt.startTime > bestStart)
            {
                bestStart = evt.startTime;
                foundEvent = evt;
            }
        }
        return foundEvent;
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
    // 外部接口
    // ==========================================
    public void SetAudienceMode(bool value)
    {
        audienceMode = value;
        if (audienceMode) ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
    }

    public void ForceRefresh() { lastCheckedTime = -999f; }

    public void ResetEditCameraToDefault()
    {
        if (isPlaying || editCamera == null) return;
        editCamera.transform.position = defaultPosition;
        editCamera.transform.rotation = Quaternion.Euler(defaultRotation);
    }
}