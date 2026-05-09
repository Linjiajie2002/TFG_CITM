using UnityEngine;

[System.Serializable]
public class SpotLightClipData
{
    // ---------- 位置 ----------
    public float posX = 0f;
    public float posY = 5f;
    public float posZ = 0f;

    // ---------- 旋转 ----------
    public float rotX = 0f;
    public float rotY = 0f;
    public float rotZ = 0f;

    // ---------- 缩放 ----------
    public float scaleX = 2.5f;
    public float scaleY = 2.35f;
    public float scaleZ = 2.5f;

    // ---------- 旋转动画 ----------
    public bool isRotating = false;
    public float rotationSpeed = 60f;

    // ---------- Shader 参数（对应 ShaderGraph 属性名）----------
    public float alpha = 1f;             // → _Global_Alpha
    public float breathSpeed = 0f;       // → _Breath_Speed
    public Color colorTop = new Color(0f, 0f, 0f, 1f);   // → _Color_Top
    public Color colorBottom = new Color(0f, 0f, 0f, 1f);   // → _Color_Bottom

    // ---------- 物理灯光 ----------
    public float range = 15f;

    // ---------- Slider 范围 ----------
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 20f;
    public float posZMin = -20f; public float posZMax = 20f;
    public float rotMin = -180f; public float rotMax = 180f;
    public float scaleMin = 0.1f; public float scaleMax = 10f;
    public float rotSpeedMin = 0f; public float rotSpeedMax = 360f;
    public float alphaMin = 0f; public float alphaMax = 1f;
    public float breathSpeedMin = -3f; public float breathSpeedMax = 3f;
    public float rangeMin = 1f; public float rangeMax = 50f;

    // ---------- 运行时（不序列化）----------
    [System.NonSerialized] public GameObject runtimeInstance = null;
    // 每个实例独立的材质（用 renderer.material 创建，避免共享）
    [System.NonSerialized] public Material runtimeMaterial = null;

    // ---------- 快捷属性 ----------
    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Vector3 Scale => new Vector3(scaleX, scaleY, scaleZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
}