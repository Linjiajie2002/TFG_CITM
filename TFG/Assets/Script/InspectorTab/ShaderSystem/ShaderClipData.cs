using UnityEngine;

// ==========================================
// Shader Clip 基础数据类
//
// 【扩展方式】：
//   新建类继承本类，加入 shader 专属参数字段
//   class GlitchShaderClipData : ShaderClipData { public float glitchFreq; }
//
// 渐入渐出逻辑由 ShaderPlaybackSystem 统一处理，
// 子类不需要关心，只需暴露参数即可。
// ==========================================
[System.Serializable]
public class ShaderClipData
{
    // ---------- 渐变设置 ----------
    [Header("渐入渐出")]
    public float fadeInDuration  = 0.5f;   // 秒，clip 开头渐入时长
    public float fadeOutDuration = 0.5f;   // 秒，clip 结尾渐出时长

    [Header("渐入渐出范围")]
    public float fadeMin = 0f;
    public float fadeMax = 3f;

    // ---------- 当前运行时强度（由系统计算，不可手动设置）----------
    // 0 = 完全隐藏，1 = 完整强度
    [System.NonSerialized] public float currentAlpha = 0f;

    // ---------- 运行时关联的材质实例 ----------
    // ShaderPlaybackSystem 在创建时注入，面板修改数据后系统自动把参数推给它
    [System.NonSerialized] public Material runtimeMaterial = null;

    // ---------- 标识：对应哪个材质模板（由 ShaderPlaybackSystem 填入）----------
    [System.NonSerialized] public string shaderEntryName = "";

    // ==========================================
    // 子类重写：把自己的参数写入 material
    // ShaderPlaybackSystem 每帧调用一次
    // ==========================================
    public virtual void ApplyToMaterial(Material mat, float alpha)
    {
        if (mat == null) return;
        // 基类只处理 _FullIntensity（所有 Full Screen Pass shader 都有）
        if (mat.HasProperty("_FullIntensity"))
            mat.SetFloat("_FullIntensity", alpha);
    }
}
