using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 【精髓所在】：定义每个模块的专属数据
[System.Serializable]
public class ModuleData
{
    public string moduleName = "New Module"; // 模块的名字 (如 Light, Camera)
    public Button addButton;                 // 这个模块专属的 Add 按钮
    public GameObject startPanel;            // 包含该按钮的初始面板 (点击后关闭)
    public GameObject targetInspector;       // 点击后，要在右侧显示的专属控制台
    public float defaultDuration = 60f;      // 默认生成的蓝色方块长度
}

public class DynamicModuleSystem : MonoBehaviour
{
    [Header("=== 全局核心引用 (只需拖一次) ===")]
    public TimelineManager timeline;         // 时间轴管理器
    public Transform headerArea;             // 左侧垂直排版的轨道头父物体
    public GameObject headerPrefab;          // 轨道头的预制体
    public GameObject defaultInspector;      // 默认的空控制台 (可选)

    [Header("=== 你的所有模块列表 ===")]
    // 在这里配置你的 4 个系统，未来可以随时随意增删！
    public List<ModuleData> allModules = new List<ModuleData>();

    void Start()
    {
        // 自动遍历你配置的所有模块，为每一个按钮绑定事件
        foreach (var module in allModules)
        {
            if (module.addButton != null)
            {
                // C# 委托防坑处理：必须用一个局部变量存起来
                ModuleData currentModule = module;
                currentModule.addButton.onClick.AddListener(() => OnModuleAddClicked(currentModule));
            }
        }
    }

    // 任何一个模块的按钮被点击时，都会执行这个核心逻辑
    void OnModuleAddClicked(ModuleData module)
    {
        // 1. 关闭它自己的初始询问面板
        if (module.startPanel != null) module.startPanel.SetActive(false);

        // 2. 左侧生成轨道头 (自动排列)
        if (headerArea != null && headerPrefab != null)
        {
            GameObject newHeader = Instantiate(headerPrefab, headerArea);
            TextMeshProUGUI txt = newHeader.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = module.moduleName;
        }

        // 3. 右侧时间轴生成蓝色方块 (通知 Timeline 全自动扩容)
        if (timeline != null)
        {
            timeline.AddDynamicTrack(module.moduleName, module.defaultDuration);
        }

        // 4. 切换右侧的 Inspector 面板
        // 先关掉默认控制台和所有其他的控制台
        if (defaultInspector != null) defaultInspector.SetActive(false);
        foreach (var m in allModules)
        {
            if (m.targetInspector != null) m.targetInspector.SetActive(false);
        }

        // 单独打开属于这一个模块的控制台！
        if (module.targetInspector != null) module.targetInspector.SetActive(true);
    }
}