  using System.Collections.Generic;
  using UnityEditor;
  using UnityEngine;

  public sealed class MapTemplateEditorWindow : EditorWindow
  {
      private sealed class TempNode
      {
          public string id;
          public Rect rect;

          public TempNode(string nodeId, Rect nodeRect)
          {
              id = nodeId;
              rect = nodeRect;
          }
      }

      private Vector2 canvasScroll;
      private readonly List<TempNode> nodes = new List<TempNode>();
      private int nextNodeIndex = 1;

      [MenuItem("Tools/地图/地图模板编辑器")]
      private static void Open()
      {
          MapTemplateEditorWindow window = GetWindow<MapTemplateEditorWindow>("地图模板编辑器");
          window.minSize = new Vector2(1000f, 700f);
          window.Show();
      }

      private void OnEnable()
      {
          if (nodes.Count == 0)
          {
              nodes.Add(new TempNode("A", new Rect(100f, 100f, 100f, 50f)));
              nodes.Add(new TempNode("B", new Rect(300f, 250f, 100f, 50f)));
              nextNodeIndex = 3;
          }
      }

      private void OnGUI()
      {
          DrawToolbar();
          DrawCanvas();
      }

      private void DrawToolbar()
      {
          using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
          {
              if (GUILayout.Button("新增节点", EditorStyles.toolbarButton, GUILayout.Width(80f)))
              {
                  nodes.Add(new TempNode("节点" + nextNodeIndex, new Rect(150f * nextNodeIndex, 100f, 100f, 50f)));
                  nextNodeIndex++;
                  Repaint();
              }

              GUILayout.FlexibleSpace();
          }
      }

      private void DrawCanvas()
      {
          canvasScroll = EditorGUILayout.BeginScrollView(canvasScroll);

          Rect canvasRect = GUILayoutUtility.GetRect(2000f, 1200f);
          EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.15f));

          Handles.BeginGUI();
          Handles.color = Color.white;
          for (int i = 0; i < nodes.Count - 1; i++)
          {
              Vector2 a = nodes[i].rect.center;
              Vector2 b = nodes[i + 1].rect.center;
              Handles.DrawLine(
                  new Vector3(canvasRect.x + a.x, canvasRect.y + a.y),
                  new Vector3(canvasRect.x + b.x, canvasRect.y + b.y));
          }
          Handles.EndGUI();

          for (int i = 0; i < nodes.Count; i++)
          {
              TempNode node = nodes[i];
              Rect drawRect = new Rect(canvasRect.x + node.rect.x, canvasRect.y + node.rect.y, node.rect.width,
  node.rect.height);
              GUI.Box(drawRect, node.id);
          }

          EditorGUILayout.EndScrollView();
      }
  }