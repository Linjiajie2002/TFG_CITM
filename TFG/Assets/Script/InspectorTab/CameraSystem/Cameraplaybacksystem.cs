using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 全能摄像机播放引擎
// ==========================================
public class CameraPlaybackSystem : MonoBehaviour
{
    [Header("=== 摄像头引用 ===")]
    public Camera editCamera;
    public Camera playCamera;

    [Header("=== 默认位置 ===")]
    public Vector3 defaultPosition = new Vector3(0f, 3f, -8f);
    public Vector3 defaultRotation = new Vector3(10f, 0f, 0f);

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Camera 轨道名 ===")]
    public string cameraTrackName = "Camera";

    [Header("=== Edit 模式预览 ===")]
    public bool previewOnEditCamera = false;

    private bool isPlaying = false;
    private bool audienceMode = false;
    private float lastCheckedTime = -999f;

    void Start() { ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation)); }

    void Update()
    {
        if (timeline == null) return;

        bool nowPlaying = timeline.musicSource != null && timeline.musicSource.isPlaying;

        if (nowPlaying != isPlaying)
        {
            isPlaying = nowPlaying;
            if (!isPlaying)
            {
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

    private void TickCamera(float currentTime)
    {
        if (audienceMode)
        {
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
            return;
        }

        TimelineEventData activeEvt = FindActiveClipEvent(currentTime);

        if (activeEvt != null)
        {
            Vector3 targetPos = defaultPosition;
            Quaternion targetRot = Quaternion.Euler(defaultRotation);

            if (activeEvt.customData is CameraClipData hardData)
            {
                targetPos = hardData.Position;
                targetRot = hardData.Rotation;
            }
            else if (activeEvt.customData is SmoothCameraClipData smoothData)
            {
                float progress = (currentTime - activeEvt.startTime) / activeEvt.duration;
                progress = Mathf.Clamp01(progress);

                if (smoothData.useMidPoint)
                {
                    // ==================================================
                    // 终极丝滑版：反推贝塞尔控制点
                    // 既能 100% 精准穿过中间点，又能保证全程顺滑无卡顿！
                    // ==================================================
                    targetPos = GetSmoothBezierPoint(smoothData.point1.Position, smoothData.midPoint.Position, smoothData.point2.Position, progress);
                    targetRot = GetSmoothBezierRotation(smoothData.point1, smoothData.midPoint, smoothData.point2, progress);
                }
                else
                {
                    // 直线移动
                    targetPos = Vector3.Lerp(smoothData.point1.Position, smoothData.point2.Position, progress);

                    // 依然使用我们之前写好的防 180 度翻车插值
                    targetRot = Quaternion.Euler(
                        Mathf.LerpAngle(smoothData.point1.rotX, smoothData.point2.rotX, progress),
                        Mathf.LerpAngle(smoothData.point1.rotY, smoothData.point2.rotY, progress),
                        Mathf.LerpAngle(smoothData.point1.rotZ, smoothData.point2.rotZ, progress)
                    );
                }
            }

            ApplyToPlayCamera(targetPos, targetRot);

            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, targetPos, targetRot);
        }
        else
        {
            ApplyToPlayCamera(defaultPosition, Quaternion.Euler(defaultRotation));
            if (previewOnEditCamera && !isPlaying && editCamera != null)
                ApplyToCamera(editCamera, defaultPosition, Quaternion.Euler(defaultRotation));
        }
    }

    // ==========================================
    // 🌟 终极平滑算法核心库 
    // ==========================================

    // 1. 位置平滑：逆向计算出能让曲线刚好穿过 pMid 的控制点
    private Vector3 GetSmoothBezierPoint(Vector3 p0, Vector3 pMid, Vector3 p2, float t)
    {
        Vector3 pCtrl = 2f * pMid - 0.5f * p0 - 0.5f * p2;
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * pCtrl + t * t * p2;
    }

    // 2. 角度平滑分配器
    private Quaternion GetSmoothBezierRotation(SmoothCameraClipData.CamPoint p0, SmoothCameraClipData.CamPoint pMid, SmoothCameraClipData.CamPoint p2, float t)
    {
        float x = GetSmoothBezierAngle(p0.rotX, pMid.rotX, p2.rotX, t);
        float y = GetSmoothBezierAngle(p0.rotY, pMid.rotY, p2.rotY, t);
        float z = GetSmoothBezierAngle(p0.rotZ, pMid.rotZ, p2.rotZ, t);
        return Quaternion.Euler(x, y, z);
    }

    // 3. 单轴角度平滑算法（彻底消除 360 度折返陷阱）
    private float GetSmoothBezierAngle(float a0, float aMid, float a2, float t)
    {
        // 先把角度变成连续的相对值（防止 359度 和 1度 之间算出个 180度的奇葩控制点）
        float mid = a0 + Mathf.DeltaAngle(a0, aMid);
        float end = mid + Mathf.DeltaAngle(mid, a2);

        // 计算角度的幽灵控制点
        float ctrl = 2f * mid - 0.5f * a0 - 0.5f * end;

        float u = 1f - t;
        return u * u * a0 + 2f * u * t * ctrl + t * t * end;
    }

    // ==========================================
    // 🌟 解决 180 度翻转问题的“独门秘方”
    // ==========================================
    private Quaternion LerpEuler(SmoothCameraClipData.CamPoint p1, SmoothCameraClipData.CamPoint p2, float t)
    {
        // Mathf.LerpAngle 会智能判断两个角度的最短平转路径（比如 180 到 0 会走水平面递减）
        // 完全杜绝了因为四元数插值导致的 X/Z 轴（俯仰/横滚）畸变乱翻
        float x = Mathf.LerpAngle(p1.rotX, p2.rotX, t);
        float y = Mathf.LerpAngle(p1.rotY, p2.rotY, t);
        float z = Mathf.LerpAngle(p1.rotZ, p2.rotZ, t);

        return Quaternion.Euler(x, y, z);
    }

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