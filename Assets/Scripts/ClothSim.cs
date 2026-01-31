using UnityEngine;
using System.Collections.Generic;

class ClothSim : MonoBehaviour
{
    public ConstraintMode constraintMode;
    public BendConstraintMode bendConstraintMode;
    public MeshFilter meshFilter;
    public Node nodePrefab;
    public float stiffness = 50f;
    public float torqueStiffness = 25f;
    public float maxForce = 50f;
    public float maxTorque = 50f;
    public float maxMoveDelta = .1f;
    public int iterations;

    Mesh oldMesh;
    Mesh runtimeMesh;

    List<Vector3> vertices;              // logical vertices (local)
    List<Vector3> verticesWorld;         // logical vertices (world)
    List<int[]> triangles;               // logical triangles (indices into vertices)
    Dictionary<int, List<int>> vertexMap; // logical -> mesh indices

    List<Node> nodes;

    List<EdgeConstraint> constraints;
    List<BendConstraint> bendConstraints;
    #region init
    void Start()
    {
        Init();
    }

    void Init()
    {
        oldMesh = meshFilter.sharedMesh;

        runtimeMesh = Instantiate(oldMesh);
        meshFilter.sharedMesh = runtimeMesh;

        MeshUtils.BuildConnectedMesh(
            runtimeMesh,
            out vertices,
            out triangles,
            out vertexMap
        );

        verticesWorld = new List<Vector3>();
        nodes = new List<Node>();
        constraints = new List<EdgeConstraint>();
        bendConstraints=new List<BendConstraint>();

        Transform t = meshFilter.transform;

        // Step 1: vertices → world
        foreach (var v in vertices)
            verticesWorld.Add(t.TransformPoint(v));

        // Step 2: spawn nodes
        foreach (var wp in verticesWorld)
        {
            Node n = Instantiate(nodePrefab, wp, Quaternion.identity);
            nodes.Add(n);
        }

        BuildConstraints();
        BuildBendConstraints();
    }
    void BuildBendConstraints()
    {
        // edge → adjacent triangles
        Dictionary<(int,int), List<int[]>> edgeTris = new();

        foreach (int[] tri in triangles)
        {
            AddEdge(edgeTris, tri[0], tri[1], tri);
            AddEdge(edgeTris, tri[1], tri[2], tri);
            AddEdge(edgeTris, tri[2], tri[0], tri);
        }

        foreach (var kv in edgeTris)
        {
            if (kv.Value.Count != 2) continue;

            int[] t0 = kv.Value[0];
            int[] t1 = kv.Value[1];

            int a = kv.Key.Item1;
            int b = kv.Key.Item2;

            int c = Third(t0, a, b);
            int d = Third(t1, a, b);

            float rest = ComputeDihedralAngle(a,b,c,d);

            bendConstraints.Add(new BendConstraint {
                a = nodes[a],
                b = nodes[b],
                c = nodes[c],
                d = nodes[d],
                restAngle = rest
            });
        }
    }
    float ComputeDihedralAngle(int a, int b, int c, int d)
    {
        Vector3 pa = nodes[a].rb.position;
        Vector3 pb = nodes[b].rb.position;
        Vector3 pc = nodes[c].rb.position;
        Vector3 pd = nodes[d].rb.position;

        Vector3 n0 = Vector3.Cross(pb - pa, pc - pa).normalized;
        Vector3 n1 = Vector3.Cross(pa - pb, pd - pb).normalized;

        return Mathf.Acos(Mathf.Clamp(Vector3.Dot(n0, n1), -1f, 1f));
    }
    void BuildConstraints()
    {
        HashSet<(int, int)> edges = new();

        void AddEdge(int a, int b)
        {
            if (a > b) (a, b) = (b, a);
            if (!edges.Add((a, b))) return;

            constraints.Add(new EdgeConstraint
            {
                a = nodes[a],
                b = nodes[b],
                restLength = Vector3.Distance(
                    nodes[a].transform.position,
                    nodes[b].transform.position)
            });
        }

        foreach (var tri in triangles)
        {
            AddEdge(tri[0], tri[1]);
            AddEdge(tri[1], tri[2]);
            AddEdge(tri[2], tri[0]);
        }
    }
#endregion
    void OnDestroy()
    {
        if (meshFilter)
            meshFilter.sharedMesh = oldMesh;
    }
    #region simulation
    void FixedUpdate()
    {
        for(int i = 0; i < iterations; ++i) {
            ResolveConstraints();
            ResolveBendConstraints();
        }
        UpdateMesh();
    }

    void ResolveConstraints()
    {
        switch (constraintMode)
        {
            case ConstraintMode.Force:
                foreach (EdgeConstraint c in constraints)
                {
                    Vector3 delta = c.b.transform.position - c.a.transform.position;
                    float dist = delta.magnitude;
                    if (dist < 1e-6f) {
                        continue;
                    }

                    float error = dist - c.restLength;
                    Vector3 dir = delta / dist;

                    Vector3 force = dir * error * stiffness; // stiffness
                    force = Vector3.ClampMagnitude(force, maxForce);

                    c.a.rb.AddForce( force, ForceMode.Force);
                    c.b.rb.AddForce(-force, ForceMode.Force);
                }
                break;
            case ConstraintMode.Direct:
                foreach (var c in constraints)
                {
                    Vector3 pa = c.a.rb.position;
                    Vector3 pb = c.b.rb.position;

                    Vector3 delta = pb - pa;
                    float dist = delta.magnitude;
                    if (dist < 1e-6f) continue;

                    // 误差
                    float diff = dist - c.restLength;

                    // 方向
                    Vector3 dir = delta / dist;

                    // 计算移动量（两边各移动一半）
                    Vector3 correction = dir * (diff * 0.5f);

                    // 限制最大位移
                    if (correction.magnitude > 10f)
                        correction = correction.normalized * 10f;

                    // 更新位置
                    c.a.rb.position += correction;
                    c.b.rb.position -= correction;
                }
                break;
        }
    }
    void ResolveBendConstraints()
    {
        switch (bendConstraintMode)
        {
            case BendConstraintMode.Force:
                foreach (var c in bendConstraints)
                {
                    Vector3 pa = c.a.rb.position;
                    Vector3 pb = c.b.rb.position;
                    Vector3 pc = c.c.rb.position;
                    Vector3 pd = c.d.rb.position;

                    // 两个三角形法线
                    Vector3 n0 = Vector3.Cross(pb - pa, pc - pa).normalized;
                    Vector3 n1 = Vector3.Cross(pb - pa, pd - pa).normalized;

                    float dot = Mathf.Clamp(Vector3.Dot(n0, n1), -1f, 1f);
                    float angle = Mathf.Acos(dot);

                    float error = angle - c.restAngle;
                    if (Mathf.Abs(error) < 1e-5f) continue;

                    // dihedral axis
                    Vector3 axis = Vector3.Cross(n0, n1).normalized;
                    if (axis.sqrMagnitude < 1e-6f) continue;

                    // 计算对顶点施加的力
                    // XPBD / PBD simplification:
                    // 力的方向沿法线旋转梯度近似
                    Vector3 f_c = axis * error * torqueStiffness;
                    Vector3 f_d = -f_c;

                    // 限制最大力
                    f_c = Vector3.ClampMagnitude(f_c, maxTorque);
                    f_d = Vector3.ClampMagnitude(f_d, maxTorque);

                    // 施加力
                    c.c.rb.AddForce(f_c, ForceMode.Force);
                    c.d.rb.AddForce(f_d, ForceMode.Force);

                    // 共享边的两个节点也可以施加一半的反作用力，让系统更稳定
                    Vector3 f_ab = -(f_c + f_d) * 0.5f;
                    c.a.rb.AddForce(f_ab, ForceMode.Force);
                    c.b.rb.AddForce(f_ab, ForceMode.Force);
                }
                break;
            case BendConstraintMode.Direct:
                foreach (var c in bendConstraints)
                {
                    Vector3 pa = c.a.rb.position;
                    Vector3 pb = c.b.rb.position;
                    Vector3 pc = c.c.rb.position;
                    Vector3 pd = c.d.rb.position;

                    // 两个三角形法线
                    Vector3 n0 = Vector3.Cross(pb - pa, pc - pa).normalized;
                    Vector3 n1 = Vector3.Cross(pb - pa, pd - pa).normalized;

                    float dot = Mathf.Clamp(Vector3.Dot(n0, n1), -1f, 1f);
                    float angle = Mathf.Acos(dot);

                    float deltaAngle = angle - c.restAngle;
                    if (Mathf.Abs(deltaAngle) < 1e-5f) continue;

                    // dihedral axis
                    Vector3 axis = Vector3.Cross(n0, n1).normalized;
                    if (axis.sqrMagnitude < 1e-6f) continue;

                    // 简化梯度：只对 c 和 d 节点修改位置
                    // 位移大小 = deltaAngle * stiffness * avgEdgeLength
                    float moveMag = deltaAngle * stiffness;
                    Vector3 moveC = axis * moveMag;
                    Vector3 moveD = -axis * moveMag;

                    // 限制最大位移
                    if (moveC.magnitude > maxMoveDelta)
                        moveC = moveC.normalized * maxMoveDelta;
                    if (moveD.magnitude > maxMoveDelta)
                        moveD = moveD.normalized * maxMoveDelta;

                    // 更新节点位置
                    c.c.rb.position += moveC;
                    c.d.rb.position += moveD;

                    // 共享边节点加半量反作用，增加稳定性
                    Vector3 moveAB = -(moveC + moveD) * 0.5f;
                    c.a.rb.position += moveAB;
                    c.b.rb.position += moveAB;
                }
                break;
        }
    }
    #endregion
    #region update mesh
    void UpdateMesh()
    {
        Vector3[] meshVerts = runtimeMesh.vertices;

        // 1️⃣ 先计算所有节点位置的几何中心（world space）
        Vector3 center = Vector3.zero;
        foreach (var node in nodes)
        {
            center += node.transform.position;
        }
        center /= nodes.Count;

        // 2️⃣ 把 meshFilter.transform 移到中心点
        meshFilter.transform.position = center;

        // 3️⃣ 将节点位置转换到 meshFilter.localSpace
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 local = meshFilter.transform.InverseTransformPoint(
                nodes[i].transform.position);

            foreach (int meshIdx in vertexMap[i])
                meshVerts[meshIdx] = local;
        }

        // 4️⃣ 更新 Mesh
        runtimeMesh.vertices = meshVerts;
        runtimeMesh.RecalculateNormals();
    }
    #endregion
    #region Utilities
    /// <summary>
    /// 返回三角形 tri 中除了 a 和 b 的那个顶点
    /// </summary>
    static int Third(int[] tri, int a, int b)
    {
        for (int i = 0; i < 3; i++)
        {
            int v = tri[i];
            if (v != a && v != b)
                return v;
        }

        throw new System.Exception($"Triangle does not contain edge ({a},{b})");
    }
    static void AddEdge(
        Dictionary<(int, int), List<int[]>> edgeTris,
        int i0,
        int i1,
        int[] tri
    )
    {
        // 保证无向边唯一
        if (i0 > i1)
            (i0, i1) = (i1, i0);

        var key = (i0, i1);

        if (!edgeTris.TryGetValue(key, out var list))
        {
            list = new List<int[]>();
            edgeTris[key] = list;
        }

        list.Add(tri);
    }
    #endregion
    public enum ConstraintMode
    {
        None,
        Force,
        Direct
    }
    public enum BendConstraintMode
    {
        None,
        Force,
        Direct
    }
}

public class EdgeConstraint
{
    public Node a;
    public Node b;
    public float restLength;
}

class BendConstraint
{
    public Node a, b, c, d;   // 两个三角形： (a,b,c) & (b,a,d)
    public float restAngle;
}