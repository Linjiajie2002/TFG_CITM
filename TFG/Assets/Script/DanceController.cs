using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DanceController : MonoBehaviour
{
    [Header("设置")]
    public Animator animator;
    public string[] danceStateNames;

    [Header("过渡设置")]
    [Tooltip("平滑过渡的时间（单位：秒）")]
    public float transitionDuration = 0.5f;

    private int currentIndex = 0;
    private Coroutine transitionCoroutine;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // 游戏一开始，强制暂停
        animator.speed = 0f;
    }

    void Start()
    {
        if (danceStateNames.Length > 0)
        {
            // 摆好第一个Pose
            animator.Play(danceStateNames[0], 0, 0f);
        }
        animator.speed = 0f;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // --- S键：正式开始跳舞 ---
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            // 如果正在自动刹车，打断它，让它继续跳
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

            animator.speed = 1f;
            Debug.Log($"S键: 开始播放 {danceStateNames[currentIndex]}");
        }

        // --- N键：瞬间切 (硬切) ---
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            SwitchDance(true);
        }

        // --- T键：慢慢切 (软切) ---
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwitchDance(false);
        }
    }

    void SwitchDance(bool isInstant)
    {
        // 1. 切换到下一个索引
        currentIndex++;
        if (currentIndex >= danceStateNames.Length) currentIndex = 0;

        string nextDance = danceStateNames[currentIndex];

        // 停止之前的协程，防止冲突
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        if (isInstant)
        {
            // 【N键】瞬间切过去
            animator.Play(nextDance, 0, 0f);
            animator.speed = 0f;
            Debug.Log($"N键: 已切至 {nextDance} (暂停)");
        }
        else
        {
            // 【T键】开启过渡协程
            transitionCoroutine = StartCoroutine(SmoothTransitionRoutine(nextDance));
        }
    }

    IEnumerator SmoothTransitionRoutine(string targetState)
    {
        Debug.Log($"T键: 开始用 {transitionDuration} 秒过渡到 {targetState}...");

        // 🔴 关键修复：使用 FixedTime (固定时间)
        // 这样 0.5f 就代表 0.5秒，而不是动画进度的 50%
        animator.CrossFadeInFixedTime(targetState, transitionDuration);

        // 必须让时间流动，过渡才能计算
        animator.speed = 1f;

        // 等待过渡完成
        yield return new WaitForSeconds(transitionDuration);

        // ⏰ 时间到！
        // 此时应该已经变成了下一个动作的姿势，立刻暂停
        animator.speed = 0f;

        // 强制修正：如果你觉得过渡完动作有点歪，可以取消下面这行的注释，强制修正到完美的第0帧
        // animator.Play(targetState, 0, 0f);

        Debug.Log("过渡完成，自动暂停。等待S键。");
        transitionCoroutine = null;
    }
}