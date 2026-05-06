using UnityEngine;

// ==========================================
// 运镜摄像头数据 (满血完整版，不丢任何数据)
// ==========================================
[System.Serializable]
public class SmoothCameraClipData
{
    [System.Serializable]
    public class CamPoint
    {
        public float posX = 0f;
        public float posY = 1f;
        public float posZ = -5f;
        public float rotX = 0f;
        public float rotY = 0f;
        public float rotZ = 0f;

        public Vector3 Position => new Vector3(posX, posY, posZ);
        public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
    }

    public CamPoint point1 = new CamPoint();
    public CamPoint point2 = new CamPoint();
    public CamPoint midPoint = new CamPoint();

    public bool useMidPoint = false;

    // 【加回来的料】弧度控制（如果你打算做跳绳式弧线还会用到）
    public float curveAmount = 0f;

    // 【加回来的料】各种滑块的范围限制（保证数据持久化不丢失）
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 5f;

    // 默认计算一个中间点位置（用来在刚开启 Toggle 时初始化）
    public CamPoint ComputeDefaultMidPoint()
    {
        return new CamPoint
        {
            posX = (point1.posX + point2.posX) / 2f,
            posY = (point1.posY + point2.posY) / 2f,
            posZ = (point1.posZ + point2.posZ) / 2f,
            rotX = Mathf.LerpAngle(point1.rotX, point2.rotX, 0.5f),
            rotY = Mathf.LerpAngle(point1.rotY, point2.rotY, 0.5f),
            rotZ = Mathf.LerpAngle(point1.rotZ, point2.rotZ, 0.5f)
        };
    }
    // 根据索引获取当前点，方便面板直接调用
    public CamPoint GetCurrent(int index)
    {
        if (index == 0) return point1;
        if (index == 1) return point2;
        return midPoint;
    }
}