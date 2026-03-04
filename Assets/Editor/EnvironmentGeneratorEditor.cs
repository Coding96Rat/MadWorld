using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnvironmentGenerator))]
public class EnvironmentGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 변수들 그리기
        DrawDefaultInspector();

        EnvironmentGenerator generator = (EnvironmentGenerator)target;

        GUILayout.Space(20);

        // 구워내기 버튼
        if (GUILayout.Button("Bake Environment (주변 타일 굽기)", GUILayout.Height(40)))
        {
            generator.BakeEnvironment();
        }

        GUILayout.Space(5);

        // 지우기 버튼 (빨간색으로 강조)
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Environment (주변 타일 지우기)", GUILayout.Height(30)))
        {
            generator.ClearEnvironment();
        }
        GUI.backgroundColor = Color.white; // 색상 초기화
    }
}