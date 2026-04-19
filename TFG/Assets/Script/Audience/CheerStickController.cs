using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

// ==========================================
// 应援棒控制器（简洁版）
//
// 操作：左键单击 → 播放一次"诶！"动画 + 音效
//   音效必须播完才能触发下一次（防叠声）
//
// 群众感：每次触发时随机微调音高和音量，
//   模拟现场一群人每人声线略有不同的效果
// ==========================================
public class CheerStickController : MonoBehaviour
{
    [Header("=== 模型 ===")]
    [Tooltip("应援棒模型的根 GameObject，切换模式时整体隐藏/显示")]
    public GameObject stickModel;

    [Header("=== 动画 ===")]
    [Tooltip("应援棒上的 Animator 组件")]
    public Animator stickAnimator;
    [Tooltip("Animator 里诶！动画对应的 Trigger 参数名")]
    public string eiTriggerName = "Ei";

    [Header("=== 音效 ===")]
    [Tooltip("挂在场景里的 AudioSource，Loop 设 false，PlayOnAwake 设 false")]
    public AudioSource audioSource;
    [Tooltip("诶！音频片段")]
    public AudioClip eiClip;

    [Header("=== 音量 ===")]
    [Tooltip("基础音量（0~1）")]
    [Range(0f, 1f)]
    public float baseVolume = 0.85f;

    [Tooltip("音量随机偏差（±值），模拟群众远近不同带来的音量差异")]
    [Range(0f, 0.3f)]
    public float volumeVariation = 0.1f;

    [Header("=== 音高（大众感）===")]
    [Tooltip("基础音高倍率，1 = 原始音高")]
    [Range(0.5f, 2f)]
    public float basePitch = 1f;

    [Tooltip("音高随机偏差（±值），模拟不同人音色差异\n推荐 0.05~0.15，越大越像一群人在喊")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.08f;

    // ── 内部 ──
    private bool isPlaying = false;   // 音效锁，播完前不能再触发

    // ==========================================
    void Update()
    {
        if (!enabled) return;
        if (stickModel == null || !stickModel.activeSelf) return;

        // 左键单击
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlayEi();
    }

    // ==========================================
    private void TryPlayEi()
    {
        if (isPlaying) return;   // 上一声还没完，忽略本次点击
        StartCoroutine(PlayEiCoroutine());
    }

    private IEnumerator PlayEiCoroutine()
    {
        isPlaying = true;

        // 触发动画
        if (stickAnimator != null)
            stickAnimator.SetTrigger(eiTriggerName);

        // 随机化音高和音量，制造群众感
        if (audioSource != null && eiClip != null)
        {
            float pitch  = Mathf.Clamp(basePitch  + Random.Range(-pitchVariation,  pitchVariation),  0.1f, 4f);
            float volume = Mathf.Clamp01(baseVolume + Random.Range(-volumeVariation, volumeVariation));

            audioSource.pitch  = pitch;
            audioSource.volume = volume;
            audioSource.clip   = eiClip;
            audioSource.loop   = false;
            audioSource.Play();

            // 等实际播放时长（音高变化会影响速度）
            float actualDuration = eiClip.length / pitch;
            yield return new WaitForSeconds(actualDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        isPlaying = false;
    }

    // ==========================================
    // 供 AudienceModeSystem 调用
    // ==========================================
    public void SetActive(bool active)
    {
        if (stickModel != null) stickModel.SetActive(active);

        if (!active)
        {
            StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();
            isPlaying = false;
        }
    }
}
