using System.Collections.Generic;
using UnityEngine;

public class JointChain : MonoBehaviour
{
    [Header("Chain Settings")]
    [Range(2, 12)] public int jointCount = 4;
    public float totalLength = 6f;
    [Range(10f, 179f)] public float maxAngle = 165f;
    [Range(1, 50)] public int iterations = 20;

    [Header("References")]
    public Transform rootAnchor;

    [Header("Colors")]
    public Color boneColor = new Color(0.70f, 0.78f, 0.90f);
    public Color jointColor = new Color(0.15f, 0.18f, 0.25f);
    public Color rootColor = new Color(1.00f, 0.55f, 0.10f);
    public Color tipColor = new Color(1.00f, 0.15f, 0.15f);

    private readonly List<Transform> joints = new List<Transform>();
    private Transform tip;
    private Transform chainRoot;
    private float[] angles;
    private Vector3 target;
    private Material boneMat, jointMat, rootMat, tipMat;

    public Transform Tip => tip;
    public float SegmentLength => totalLength / Mathf.Max(1, jointCount);

    void Awake()
    {
        boneMat = MakeMat(boneColor);
        jointMat = MakeMat(jointColor);
        rootMat = MakeMat(rootColor);
        tipMat = MakeMat(tipColor);
    }

    void Start()
    {
        Build(jointCount);
    }

    public void Rebuild(int count)
    {
        jointCount = Mathf.Clamp(count, 2, 12);
        Build(jointCount);
    }

    private void Build(int count)
    {
        if (chainRoot != null)
        {
            chainRoot.gameObject.SetActive(false);
            Destroy(chainRoot.gameObject);
        }
        joints.Clear();

        chainRoot = new GameObject("Chain").transform;
        chainRoot.SetParent(rootAnchor != null ? rootAnchor : transform, false);

        // 根节点底座
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "RootBase";
        baseObj.transform.SetParent(chainRoot, false);
        baseObj.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        baseObj.transform.localScale = new Vector3(1.3f, 0.05f, 1.3f);
        SetMat(baseObj, rootMat);
        RemoveCollider(baseObj);

        float L = totalLength / count;
        float boneRadius = Mathf.Clamp(L * 0.10f, 0.035f, 0.14f);
        float jointRadius = Mathf.Clamp(L * 0.13f, 0.07f, 0.18f);
        float tipRadius = Mathf.Clamp(L * 0.16f, 0.10f, 0.22f);

        Transform parent = chainRoot;

        for (int i = 0; i < count; i++)
        {
            GameObject j = new GameObject("Joint_" + i);
            j.transform.SetParent(parent, false);
            j.transform.localPosition = i == 0 ? Vector3.zero : new Vector3(0f, L, 0f);
            j.transform.localRotation = Quaternion.identity;

            // 关节球
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "JointVisual_" + i;
            sphere.transform.SetParent(j.transform, false);
            sphere.transform.localScale = Vector3.one * (jointRadius * 2f);
            SetMat(sphere, i == 0 ? rootMat : jointMat);
            RemoveCollider(sphere);

            // 骨骼
            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bone.name = "Bone_" + i;
            bone.transform.SetParent(j.transform, false);
            bone.transform.localPosition = new Vector3(0f, L * 0.5f, 0f);
            bone.transform.localScale = new Vector3(boneRadius * 2f, L * 0.5f, boneRadius * 2f);
            SetMat(bone, boneMat);
            RemoveCollider(bone);

            joints.Add(j.transform);
            parent = j.transform;
        }

        // 末端执行器（红色，可拖拽）
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        t.name = "Tip";
        t.transform.SetParent(parent, false);
        t.transform.localPosition = new Vector3(0f, L, 0f);
        t.transform.localScale = Vector3.one * (tipRadius * 2f);
        SetMat(t, tipMat);

        SphereCollider sc = t.GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.radius = 1.3f;
            sc.isTrigger = false;
        }

        tip = t.transform;
        angles = new float[count];
        target = tip.position;
        SolveIK(target);
    }

    void LateUpdate()
    {
        if (tip != null) SolveIK(target);
    }

    public void SetTarget(Vector3 p)
    {
        target = p;
    }

    public float[] GetAngles()
    {
        return angles;
    }

    private void SolveIK(Vector3 goal)
    {
        if (joints.Count == 0 || tip == null) return;

        Vector3 root = joints[0].position;
        float total = joints.Count * SegmentLength;

        Vector3 toGoal = goal - root;
        float dist = toGoal.magnitude;

        // 目标超出可达范围：夹到最远点；太近：避免完全折叠
        if (dist > total && dist > 0.0001f)
            goal = root + toGoal / dist * total;
        else if (dist < 0.05f)
            goal = root + Vector3.up * 0.05f;

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = joints.Count - 1; i >= 0; i--)
            {
                Vector3 jointPos = joints[i].position;
                Vector3 toEnd = tip.position - jointPos;
                Vector3 toTarget = goal - jointPos;

                if (toEnd.sqrMagnitude < 1e-8f || toTarget.sqrMagnitude < 1e-8f)
                    continue;

                float delta = Vector3.SignedAngle(toEnd, toTarget, Vector3.forward);
                float next = Mathf.Clamp(angles[i] + delta, -maxAngle, maxAngle);
                delta = next - angles[i];

                if (Mathf.Abs(delta) < 0.0001f) continue;

                angles[i] = next;
                joints[i].localRotation = Quaternion.Euler(0f, 0f, angles[i]);
            }

            if ((tip.position - goal).sqrMagnitude < 0.0004f)
                break;
        }
    }

    private Material MakeMat(Color c)
    {
        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Unlit/Color");

        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    private void SetMat(GameObject go, Material m)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = m;
    }

    private void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            c.enabled = false;
            Destroy(c);
        }
    }
}