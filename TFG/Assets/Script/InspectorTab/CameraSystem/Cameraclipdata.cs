using UnityEngine;

// ==========================================
// 每个摄像头 Clip 的参数数据
// 由 CameraClipInspectorPanel 写入，由 CameraPlaybackSystem 读取
// ==========================================
[System.Serializable]
public class CameraClipData
{
    // ---------- 位置 ----------
    public float posX = 0f;
    public float posY = 2f;
    public float posZ = -5f;

    // ---------- 旋转 (欧拉角) ----------
    public float rotX = 15f;   // 俯仰
    public float rotY = 0f;    // 偏航
    public float rotZ = 0f;    // 横滚

    // ---------- Slider 范围（在面板预制体里配置，运行时不变） ----------
    [Header("X轴范围")]
    public float posXMin = -20f;
    public float posXMax = 20f;

    [Header("Y轴范围")]
    public float posYMin = 0f;
    public float posYMax = 15f;

    [Header("Z轴范围")]
    public float posZMin = -20f;
    public float posZMax = 5f;

    // ---------- 快捷属性 ----------
    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
}