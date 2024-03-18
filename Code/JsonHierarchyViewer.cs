using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;

public class JsonHierarchyViewer : EditorWindow
{
    private string jsonFilePath = "";
    private bool rootFoldoutState = false;
    private Vector2 scrollPosition;
    private JToken jsonData; // JToken�� ����Ͽ� �پ��� JSON ������ ó��
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>(); // �� JSON ����� fold ���� ����

    [MenuItem("Tools/JSON Hierarchy Viewer")]
    public static void ShowWindow()
    {
        GetWindow<JsonHierarchyViewer>("JSON Hierarchy Viewer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ver. 1.0.3", EditorStyles.boldLabel);
        DrawLine();

        GUILayout.BeginVertical();
        var dropArea = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Excel file here or Click 'Open JSON File'");
        GUILayout.EndVertical();

        DragAndDropVisual(dropArea);
        if (GUILayout.Button("Open JSON File"))
        {
            OpenJsonFile();
        }

        EditorGUILayout.LabelField("JSON File Path:", jsonFilePath);
        DrawLine();

        if (!string.IsNullOrEmpty(jsonFilePath))
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFilePath);
            rootFoldoutState = EditorGUILayout.Foldout(rootFoldoutState, fileNameWithoutExtension, true);

            if (rootFoldoutState)
            {
                DisplayJsonData(jsonData, ""); // ������ DisplayJsonData ȣ��
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DragAndDropVisual(Rect dropArea)
    {
        var currentEvent = Event.current;
        switch (currentEvent.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(currentEvent.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        var path = AssetDatabase.GetAssetPath(draggedObject);
                        if (Path.GetExtension(path).Equals(".json"))
                        {
                            jsonFilePath = path;
                            ParseJsonFile(path);
                            break;
                        }
                    }
                }
                break;
        }
    }

    private void OpenJsonFile()
    {
        string path = EditorUtility.OpenFilePanel("Select JSON File", Application.persistentDataPath, "json");
        if (!string.IsNullOrEmpty(path) && Path.GetExtension(path).Equals(".json"))
        {
            jsonFilePath = path;
            ParseJsonFile(path);
        }
    }

    private void DrawLine()
    {
        var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, Color.gray);
    }

    private void ParseJsonFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);
        jsonData = JToken.Parse(jsonContent); // JToken.Parse�� ����Ͽ� JSON ������ �Ľ�
        foldoutStates.Clear();
        Repaint();
    }

    private void DisplayJsonData(JToken data, string path)
    {
        if (data is JObject) // ��ü Ÿ���� �׸�
        {
            foreach (var prop in (JObject)data)
            {
                string newPath = $"{path}/{prop.Key}";
                bool foldout = GetFoldoutState(newPath);
                EditorGUI.indentLevel++;
                if (prop.Value is JValue) // �� Ÿ���� �׸� ó��
                {
                    EditorGUILayout.LabelField($"{prop.Key}: {prop.Value}");
                    EditorGUI.indentLevel--; 
                }
                else
                {
                    // ��ü �Ǵ� �迭 Ÿ���� �׸� ���� foldout ó��
                    foldoutStates[newPath] = EditorGUILayout.Foldout(foldout, $"{prop.Key}", true);
                    if (foldoutStates[newPath])
                    {
                        DisplayJsonData(prop.Value, newPath);
                    }
                    EditorGUI.indentLevel--; // ��� ��쿡 ���Ұ� �߻��ϵ��� ����
                }
            }
        }
        else if (data is JArray) // �迭 Ÿ���� �׸�
        {
            if (!data.Any()) // �迭�� ����ִ��� Ȯ�� -> ex)�迭 = []
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("empty array");
                EditorGUI.indentLevel--;
            }
            else
            {
                for (int i = 0; i < data.Count(); i++)
                {
                    string itemPath = $"{path}[{i}]";
                    bool foldout = GetFoldoutState(itemPath);
                    EditorGUI.indentLevel++;

                    foldoutStates[itemPath] = EditorGUILayout.Foldout(foldout, $"[{i}]", true);
                    if (foldoutStates[itemPath])
                    {
                        if (data[i] is JValue) // �迭�� ��Ұ� �� Ÿ���� ���
                        {
                            EditorGUI.indentLevel++;
                            var value = data[i] as JValue;

                            // �� Ÿ���� ��Ұ� null�̰ų� �� ���ڿ��� ��� "null" ���
                            EditorGUILayout.LabelField(value == null || value.Value == null ? "null" : value.ToString());
                            EditorGUI.indentLevel--;
                        }
                        else if (!data[i].HasValues) // �迭�� ��Ұ� ��ü�� �ٸ� �迭�� ��� -> ex)�迭�� ��� = {} or []
                        {
                            EditorGUI.indentLevel++;

                            // JObject {} 
                            if (data[i].Type == JTokenType.Object)
                            {
                                EditorGUILayout.LabelField("empty object");
                            }

                            // JArray[]
                            else if (data[i].Type == JTokenType.Array)
                            {
                                EditorGUILayout.LabelField("empty array");
                            }
                            EditorGUI.indentLevel--;
                        }
                        else
                        {
                            DisplayJsonData(data[i], itemPath);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }




    private bool GetFoldoutState(string path)
    {
        if (!foldoutStates.ContainsKey(path))
        {
            foldoutStates[path] = false;
        }
        return foldoutStates[path];
    }
}
