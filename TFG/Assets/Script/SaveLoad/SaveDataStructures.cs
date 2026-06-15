using System;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// MDF 文件数据结构
// 所有字段使用 [Serializable] + 基本类型，确保 JsonUtility 可序列化
// ==========================================

// ── 根节点 ──────────────────────────────────────────────────
[Serializable]
public class MDFSaveData
{
    public string version = "1.0";
    public string savedAt = "";
    public string sceneName = "";

    public List<TrackSaveData> tracks = new List<TrackSaveData>();
    public List<ClipSaveData> clips = new List<ClipSaveData>();

    // 记录哪些模块的 startPanel 已关闭（即已添加轨道）
    public List<string> activatedModules = new List<string>();
}

// ── 轨道 ────────────────────────────────────────────────────
[Serializable]
public class TrackSaveData
{
    public string trackName;
    public int trackIndex;
    public bool allowOverlap;
}

// ── Clip 通用壳 ─────────────────────────────────────────────
[Serializable]
public class ClipSaveData
{
    public string eventName;
    public int trackIndex;
    public float startTime;
    public float duration;

    // 类型判别字段：决定 customDataJson 解析成哪个类
    // 取值："Camera" | "SmoothCamera" | "PointLight" | "VFX" | "Shader_Voronoi" | "Shader_Base" | "Outline" | "SpotLight" | ""
    public string customDataType = "";

    // 把具体数据二次序列化成 JSON 字符串存在这里
    public string customDataJson = "";
}

// ── Smooth Camera Clip 数据 (新增的平滑相机存档结构) ────────────
[Serializable]
public class CamPointSave
{
    public float posX; public float posY; public float posZ;
    public float rotX; public float rotY; public float rotZ;
}

[Serializable]
public class SmoothCameraClipSave
{
    public CamPointSave point1 = new CamPointSave();
    public CamPointSave point2 = new CamPointSave();
    public CamPointSave midPoint = new CamPointSave();
    public bool useMidPoint;
    public float curveAmount;

    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
}

// ── Camera Clip 数据 ─────────────────────────────────────────
[Serializable]
public class CameraClipSave
{
    public float posX; public float posY; public float posZ;
    public float rotX; public float rotY; public float rotZ;
    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
}

// ── PointLight Clip 数据 ─────────────────────────────────────
[Serializable]
public class PointLightClipSave
{
    public float posX; public float posY; public float posZ;
    public float r; public float g; public float b; public float a;
    public float intensity; public float range;
    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
    public float intensityMin; public float intensityMax;
    public float rangeMin; public float rangeMax;
}

// ── VFX Clip 数据 ────────────────────────────────────────────
[Serializable]
public class VFXClipSave
{
    public float posX; public float posY; public float posZ;
    public float rotX; public float rotY; public float rotZ;
    public float scaleX; public float scaleY; public float scaleZ;
    public float r; public float g; public float b; public float a;
    public float playSpeed;
    public bool loop;
    public string vfxPrefabName;
    // 范围
    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
    public float rotMin; public float rotMax;
    public float scaleMin; public float scaleMax;
    public float speedMin; public float speedMax;
}

// ── Shader 基础 Clip 数据 ────────────────────────────────────
[Serializable]
public class ShaderClipBaseSave
{
    public float fadeInDuration;
    public float fadeOutDuration;
}

// ── Voronoi Shader Clip 数据 ─────────────────────────────────
[Serializable]
public class VoronoiShaderClipSave : ShaderClipBaseSave
{
    public float r; public float g; public float b; public float a;
    public float voronoiSpeed; public float voronoiScale; public float voronoiPower;
    public float vignetteRadiusPower; public float vignetteIntensity;
    public float glowPower;
    // 范围
    public float voronoiSpeedMin; public float voronoiSpeedMax;
    public float voronoiScaleMin; public float voronoiScaleMax;
    public float voronoiPowerMin; public float voronoiPowerMax;
    public float vignetteRadiusMin; public float vignetteRadiusMax;
    public float vignetteIntMin; public float vignetteIntMax;
    public float glowMin; public float glowMax;
}

// ── Outline Clip 数据 ──────────────────────────────────────────
[Serializable]
public class OutlineClipSave
{
    public float r; public float g; public float b; public float a;
    public float colorThreshold;
    public float normalThreshold;
    // 范围
    public float colorThresholdMin; public float colorThresholdMax;
    public float normalThresholdMin; public float normalThresholdMax;
}

// ── SpotLight Clip 数据 ────────────────────────────────────────
[Serializable]
public class SpotLightClipSave
{
    // 位置 / 旋转 / 缩放
    public float posX; public float posY; public float posZ;
    public float rotX; public float rotY; public float rotZ;
    public float scaleX; public float scaleY; public float scaleZ;

    // 旋转动画
    public bool isRotating;
    public float rotationSpeed;
    public float circleRadius;

    // Shader 参数
    public float alpha;
    public float breathSpeed;
    public float topR; public float topG; public float topB; public float topA;
    public float botR; public float botG; public float botB; public float botA;

    // 物理灯光
    public float range;

    // 范围
    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
    public float rotMin; public float rotMax;
    public float scaleMin; public float scaleMax;
    public float rotSpeedMin; public float rotSpeedMax;
    public float circleRadiusMin; public float circleRadiusMax;
    public float alphaMin; public float alphaMax;
    public float breathSpeedMin; public float breathSpeedMax;
    public float rangeMin; public float rangeMax;
}