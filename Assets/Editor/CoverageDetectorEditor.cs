using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoverageDetector))]
public class CoverageDetectorEditor : Editor
{
    CoverageDetector path;
    const float HANDLE_SIZE = 0.08f;

    void OnEnable()
    {
        path = (CoverageDetector)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(path, "Add Point");

            Vector2 newPoint = path.points.Count > 1
                ? path.points[^1] + .5f*(path.points[^1] -path.points[^2])
                : Vector2.zero;

            path.points.Add(newPoint);
            EditorUtility.SetDirty(path);
        }

        if (path.points.Count > 0)
        {
            if (GUILayout.Button("Remove Last Point"))
            {
                Undo.RecordObject(path, "Remove Point");
                path.points.RemoveAt(path.points.Count - 1);
                EditorUtility.SetDirty(path);
            }
        }
    }

    void OnSceneGUI()
    {
        if (path.points == null)
            return;

        Transform t = path.transform;

        for (int i = 0; i < path.points.Count; i++)
        {
            Vector3 worldPos = t.TransformPoint(path.points[i]);

            EditorGUI.BeginChangeCheck();

            var fmh_59_17_639055407098603167 = Quaternion.identity; Vector3 newWorldPos = Handles.FreeMoveHandle(
                worldPos,
                HANDLE_SIZE,
                Vector3.zero,
                Handles.DotHandleCap
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Point");
                path.points[i] = t.InverseTransformPoint(newWorldPos);
                EditorUtility.SetDirty(path);
            }

            // 点编号
            Handles.Label(newWorldPos + Vector3.up * 0.1f, i.ToString());
        }
    }
}