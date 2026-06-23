using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpotLightClipInspectorPanel : ClipInspectorPanel
{
    private static readonly int ID_GlobalAlpha = Shader.PropertyToID("_Global_Alpha");
    private static readonly int ID_BreathSpeed = Shader.PropertyToID("_Breath_Speed");
    private static readonly int ID_ColorTop = Shader.PropertyToID("_Color_Top");
    private static readonly int ID_ColorBottom = Shader.PropertyToID("_Color_Bottom");

    [Header("=== Position ===")]
    public Slider sliderPosX, sliderPosY, sliderPosZ;
    public TextMeshProUGUI posXText, posYText, posZText;

    [Header("=== Rotation ===")]
    public Slider sliderRotX, sliderRotY, sliderRotZ;
    public TextMeshProUGUI rotXText, rotYText, rotZText;

    [Header("=== Scale ===")]
    public Slider sliderScaleX, sliderScaleY, sliderScaleZ;
    public TextMeshProUGUI scaleXText, scaleYText, scaleZText;

    [Header("=== Circle Rotation Animation ===")]
    public Toggle toggleRotation;   // ��/����ת

    public GameObject rotSpeedRow;
    public Slider sliderRotSpeed;
    public TextMeshProUGUI rotSpeedText;

    // circleRadius Slider������ʱ��ʾ����ֵ���ر�ʱ Slider �ص� 0������ֵ������
    public Slider sliderCircleRadius;
    public TextMeshProUGUI circleRadiusText;

    [Header("=== Shader ===")]
    public Slider sliderAlpha;
    public TextMeshProUGUI alphaText;
    public Slider sliderBreathSpeed;
    public TextMeshProUGUI breathSpeedText;

    [Header("=== Colors ===")]
    public ColorPickerPanel colorPickerTop;
    public ColorPickerPanel colorPickerBottom;

    [Header("=== 替身 (3D Dummy) ===")]
    public GameObject spotDummyPrefab;
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f);
    private GameObject myDummyInstance;
    private MeshRenderer[] dummyRenderers;
    private Color[] originalColors;

    private SpotLightClipData spotData;
    private bool isReady = false;
    private float savedCircleRadius = 0f; // �ر�ʱ�ݴ� circleRadius

    protected override void Awake() => base.Awake();

    private void OnEnable()
    {
        if (dummyRenderers != null)
            foreach (var r in dummyRenderers) r.material.color = selectedColor;
    }

    private void OnDisable()
    {
        if (dummyRenderers != null)
            for (int i = 0; i < dummyRenderers.Length; i++)
                dummyRenderers[i].material.color = originalColors[i];
    }

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        isReady = false;
        base.BindClip(clip, mgr);

        spotData = clip.customData is SpotLightClipData ex ? ex : new SpotLightClipData();
        clip.customData = spotData;

        SetupSlider(sliderPosX, spotData.posXMin, spotData.posXMax, spotData.posX);
        SetupSlider(sliderPosY, spotData.posYMin, spotData.posYMax, spotData.posY);
        SetupSlider(sliderPosZ, spotData.posZMin, spotData.posZMax, spotData.posZ);
        SetupSlider(sliderRotX, spotData.rotMin, spotData.rotMax, spotData.rotX);
        SetupSlider(sliderRotY, spotData.rotMin, spotData.rotMax, spotData.rotY);
        SetupSlider(sliderRotZ, spotData.rotMin, spotData.rotMax, spotData.rotZ);
        SetupSlider(sliderScaleX, spotData.scaleMin, spotData.scaleMax, spotData.scaleX);
        SetupSlider(sliderScaleY, spotData.scaleMin, spotData.scaleMax, spotData.scaleY);
        SetupSlider(sliderScaleZ, spotData.scaleMin, spotData.scaleMax, spotData.scaleZ);
        SetupSlider(sliderRotSpeed, spotData.rotSpeedMin, spotData.rotSpeedMax, spotData.rotationSpeed);
        SetupSlider(sliderAlpha, spotData.alphaMin, spotData.alphaMax, spotData.alpha);
        SetupSlider(sliderBreathSpeed, spotData.breathSpeedMin, spotData.breathSpeedMax, spotData.breathSpeed);

        // circleRadius������ʱ��ʾ����ֵ���ر�ʱ��ʾ 0
        float radiusDisplay = spotData.isRotating ? spotData.circleRadius : 0f;
        SetupSlider(sliderCircleRadius, spotData.circleRadiusMin, spotData.circleRadiusMax, radiusDisplay);

        // Toggle
        if (toggleRotation != null)
        {
            toggleRotation.onValueChanged.RemoveAllListeners();
            toggleRotation.isOn = spotData.isRotating;
            toggleRotation.onValueChanged.AddListener(OnToggleChanged);
        }

        if (colorPickerTop != null)
        {
            colorPickerTop.onColorChanged.RemoveAllListeners();
            colorPickerTop.SetColor(spotData.colorTop, notify: false);
            colorPickerTop.onColorChanged.AddListener(c => { spotData.colorTop = c; Refresh(); });
        }
        if (colorPickerBottom != null)
        {
            colorPickerBottom.onColorChanged.RemoveAllListeners();

            spotData.colorBottom = Color.red;

            colorPickerBottom.SetColor(spotData.colorBottom, notify: false);
            colorPickerBottom.onColorChanged.AddListener(c => { spotData.colorBottom = c; Refresh(); });
        }

        // 克隆 3D 替身
        if (myDummyInstance == null && spotDummyPrefab != null)
        {
            myDummyInstance = Instantiate(spotDummyPrefab);
            dummyRenderers = myDummyInstance.GetComponentsInChildren<MeshRenderer>();
            originalColors = new Color[dummyRenderers.Length];
            for (int i = 0; i < dummyRenderers.Length; i++)
                originalColors[i] = dummyRenderers[i].material.color;
            SetLayerRecursively(myDummyInstance, LayerMask.NameToLayer("EditorOnly"));
        }

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
    }

    // ==========================================
    // Toggle �ص�
    // ==========================================
    private void OnToggleChanged(bool isOn)
    {
        if (!isReady || spotData == null) return;
        spotData.isRotating = isOn;

        if (isOn)
        {
            // �������� savedCircleRadius �ָ�ֵ
            spotData.circleRadius = savedCircleRadius;
            SetupSlider(sliderCircleRadius, spotData.circleRadiusMin, spotData.circleRadiusMax, spotData.circleRadius);
            if (sliderCircleRadius != null)
                sliderCircleRadius.onValueChanged.AddListener(v => { spotData.circleRadius = v; Refresh(); });
        }
        else
        {
            // �رգ��ѵ�ǰֵ�浽 savedCircleRadius��Ȼ��� circleRadius �� Slider ������
            savedCircleRadius = sliderCircleRadius != null ? sliderCircleRadius.value : spotData.circleRadius;
            spotData.circleRadius = 0f;

            if (sliderCircleRadius != null)
            {
                sliderCircleRadius.onValueChanged.RemoveAllListeners();
                sliderCircleRadius.value = 0f;
                sliderCircleRadius.interactable = false;
            }
        }

        SetRotationRowsVisible(isOn);
    }

    private void SetRotationRowsVisible(bool v)
    {
        if (rotSpeedRow != null) rotSpeedRow.SetActive(v);
        // circleRadius �и���������������е�����������������������ƣ�û�о�ɾ���У�
        if (sliderCircleRadius != null) sliderCircleRadius.interactable = v;
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || spotData == null) return;
        if (toggleRotation != null) toggleRotation.isOn = spotData.isRotating;
        SetRotationRowsVisible(spotData.isRotating);
        UpdateLabels();
        UpdatePreview();
        UpdateVisuals();
    }

    private void SetupSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.interactable = true;
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(val, min, max);
    }

    private void RegisterListeners()
    {
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { spotData.posX = v; Refresh(); });
        if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(v => { spotData.posY = v; Refresh(); });
        if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(v => { spotData.posZ = v; Refresh(); });
        if (sliderRotX != null) sliderRotX.onValueChanged.AddListener(v => { spotData.rotX = v; Refresh(); });
        if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(v => { spotData.rotY = v; Refresh(); });
        if (sliderRotZ != null) sliderRotZ.onValueChanged.AddListener(v => { spotData.rotZ = v; Refresh(); });
        if (sliderScaleX != null) sliderScaleX.onValueChanged.AddListener(v => { spotData.scaleX = v; Refresh(); });
        if (sliderScaleY != null) sliderScaleY.onValueChanged.AddListener(v => { spotData.scaleY = v; Refresh(); });
        if (sliderScaleZ != null) sliderScaleZ.onValueChanged.AddListener(v => { spotData.scaleZ = v; Refresh(); });
        if (sliderRotSpeed != null) sliderRotSpeed.onValueChanged.AddListener(v => { spotData.rotationSpeed = v; Refresh(); });
        if (sliderAlpha != null) sliderAlpha.onValueChanged.AddListener(v => { spotData.alpha = v; Refresh(); });
        if (sliderBreathSpeed != null) sliderBreathSpeed.onValueChanged.AddListener(v => { spotData.breathSpeed = v; Refresh(); });

        // circleRadius ֻ�ڿ���ʱע�ᣨ�ر�ʱ�� OnToggleChanged ������
        if (sliderCircleRadius != null && spotData.isRotating)
            sliderCircleRadius.onValueChanged.AddListener(v => { spotData.circleRadius = v; Refresh(); });
    }

    private void Refresh()
    {
        UpdateLabels();
        UpdatePreview();
        UpdateVisuals();
    }

    private void UpdateLabels()
    {
        if (posXText) posXText.text = $"{spotData.posX:F1}";
        if (posYText) posYText.text = $"{spotData.posY:F1}";
        if (posZText) posZText.text = $"{spotData.posZ:F1}";
        if (rotXText) rotXText.text = $"{spotData.rotX:F0}��";
        if (rotYText) rotYText.text = $"{spotData.rotY:F0}��";
        if (rotZText) rotZText.text = $"{spotData.rotZ:F0}��";
        if (scaleXText) scaleXText.text = $"{spotData.scaleX:F2}";
        if (scaleYText) scaleYText.text = $"{spotData.scaleY:F2}";
        if (scaleZText) scaleZText.text = $"{spotData.scaleZ:F2}";
        if (rotSpeedText) rotSpeedText.text = $"{spotData.rotationSpeed:F0}��/s";
        if (alphaText) alphaText.text = $"{spotData.alpha:F2}";
        if (breathSpeedText) breathSpeedText.text = $"{spotData.breathSpeed:F2}";
        // �ر�ʱ��ʾ 0������ʱ��ʾʵ�ʱ���ֵ
        if (circleRadiusText) circleRadiusText.text = spotData.isRotating ? $"{spotData.circleRadius:F1}��" : "0��";
    }

    private void UpdatePreview()
    {
        if (spotData?.runtimeInstance == null) return;

        spotData.runtimeInstance.transform.position = spotData.Position;
        spotData.runtimeInstance.transform.localScale = spotData.Scale;
        spotData.runtimeInstance.transform.rotation = spotData.Rotation;

        if (spotData.runtimeMaterial != null)
        {
            spotData.runtimeMaterial.SetFloat(ID_GlobalAlpha, spotData.alpha);
            spotData.runtimeMaterial.SetFloat(ID_BreathSpeed, spotData.breathSpeed);
            spotData.runtimeMaterial.SetColor(ID_ColorTop, spotData.colorTop);
            spotData.runtimeMaterial.SetColor(ID_ColorBottom, spotData.colorBottom);
        }

        Light lt = spotData.runtimeInstance.GetComponentInChildren<Light>();
        if (lt != null)
        {
            lt.range = spotData.range;
            lt.color = spotData.colorTop;
            lt.intensity = spotData.alpha * 5f;
        }
    }

    // 把 Dummy 同步到当前 spotData 的 Position / Rotation / Scale
    private void UpdateVisuals()
    {
        if (myDummyInstance != null && spotData != null)
        {
            myDummyInstance.transform.position = spotData.Position + new Vector3(0, 0.5f, 0f);
            myDummyInstance.transform.rotation = spotData.Rotation;
        }
    }

    private void OnDestroy()
    {
        if (myDummyInstance != null) Destroy(myDummyInstance);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}