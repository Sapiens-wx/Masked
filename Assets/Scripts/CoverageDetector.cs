using System;
using System.Collections.Generic;
using UnityEngine;

public class CoverageDetector : Singleton<CoverageDetector>
{
    public LayerMask maskLayer;
    [Header("Shape")]
    public List<Vector2> points = new List<Vector2>();
    public bool loop = true;

    [Header("Spawn")]
    public GameObject prefab;
    public float spacing = 1f;

    [Header("Runtime")]
    public List<Collider2D> spawnedObjects = new List<Collider2D>();

    // ---- length cache ----
    float totalLength;
    List<float> segmentLengths = new List<float>();

    void Start()
    {
        ClearSpawned();
        SpawnAlongPath();
    }
    #region Scoring
    public float GetCoverage()
    {
        if (spawnedObjects == null || spawnedObjects.Count == 0)
            return 0f;

        int coveredCount = 0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(maskLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;   // trigger 也算

        Collider2D[] results = new Collider2D[8];

        foreach (var col in spawnedObjects)
        {
            if (col == null || !col.enabled)
                continue;

            int hitCount = col.Overlap(filter, results);

            if (hitCount > 0)
                coveredCount++;
        }

        return (float)coveredCount / spawnedObjects.Count;
    }
    #endregion

    #region Spawn

    void SpawnAlongPath()
    {
        if (prefab == null || points.Count < 2 || spacing <= 0f)
            return;

        RebuildLengthCache();

        int count = Mathf.FloorToInt(totalLength / spacing);

        for (int i = 0; i <= count; i++)
        {
            float d = i * spacing;
            Vector2 localPos = GetPointAtDistance(d);
            Vector3 worldPos = transform.TransformPoint(localPos);

            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            spawnedObjects.Add(go.GetComponent<Collider2D>());
        }
    }

    void RebuildLengthCache()
    {
        segmentLengths.Clear();
        totalLength = 0f;

        int segCount = loop ? points.Count : points.Count - 1;

        for (int i = 0; i < segCount; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];

            float len = Vector2.Distance(a, b);
            segmentLengths.Add(len);
            totalLength += len;
        }
    }

    Vector2 GetPointAtDistance(float d)
    {
        d = Mathf.Clamp(d, 0, totalLength);

        float accumulated = 0f;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segLen = segmentLengths[i];

            if (accumulated + segLen >= d)
            {
                float t = (d - accumulated) / segLen;
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                return Vector2.Lerp(a, b, t);
            }

            accumulated += segLen;
        }

        // fallback（理论上不会走到）
        return points[^1];
    }

    #endregion

    #region Utils

    void ClearSpawned()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                DestroyImmediate(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (points == null || points.Count < 2)
            return;

        Gizmos.color = Color.green;

        int count = loop ? points.Count : points.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 a = transform.TransformPoint(points[i]);
            Vector3 b = transform.TransformPoint(points[(i + 1) % points.Count]);
            Gizmos.DrawLine(a, b);
        }
    }

    #endregion
}