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
    public string version   = "1.0";
    public string savedAt   = "";
    public string sceneName = "";

    public List<TrackSaveData> tracks = new List<TrackSaveData>();
    public List<ClipSaveData>  clips  = new List<ClipSaveData>();

    // 记录哪些模块的 startPanel 已关闭（即已添加轨道）
    public List<string> activatedModules = new List<string>();
}

// ── 轨道 ────────────────────────────────────────────────────
[Serializable]
public class TrackSaveData
{
    public string trackName;
    public int    trackIndex;
    public bool   allowOverlap;
}

// ── Clip 通用壳 ─────────────────────────────────────────────
[Serializable]
public class ClipSaveData
{
    public string eventName;
    public int    trackIndex;
    public float  startTime;
    public float  duration;

    // 类型判别字段：决定 customDataJson 解析成哪个类
    // 取值："Camera" | "PointLight" | "VFX" | "Shader_Voronoi" | "Shader_Base" | ""
    public string customDataType = "";

    // 把具体数据二次序列化成 JSON 字符串存在这里
    public string customDataJson = "";
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
    public float rangeMin;     public float rangeMax;
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
    public bool  loop;
    public string vfxPrefabName;
    // 范围
    public float posXMin; public float posXMax;
    public float posYMin; public float posYMax;
    public float posZMin; public float posZMax;
    public float rotMin;  public float rotMax;
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
    public float vignetteIntMin;    public float vignetteIntMax;
    public float glowMin; public float glowMax;
}
