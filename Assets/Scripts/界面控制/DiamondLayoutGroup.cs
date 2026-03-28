  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.UI;

  [AddComponentMenu("Layout/Diamond Layout Group")]
  public class DiamondLayoutGroup : LayoutGroup
  {
      public Vector2 cellSize = new Vector2(80f, 60f);
      public Vector2 spacing = new Vector2(12f, 12f);

      public override void CalculateLayoutInputHorizontal()
      {
          base.CalculateLayoutInputHorizontal();
      }

      public override void CalculateLayoutInputVertical()
      {
      }

      public override void SetLayoutHorizontal()
      {
          LayoutChildren();
      }

      public override void SetLayoutVertical()
      {
          LayoutChildren();
      }

      private void LayoutChildren()
      {
          List<RectTransform> children = new List<RectTransform>();
          for (int i = 0; i < rectChildren.Count; i++)
          {
              if (rectChildren[i] != null)
              {
                  children.Add(rectChildren[i]);
              }
          }

          int count = children.Count;
          if (count == 0)
          {
              return;
          }

          List<int> rows = BuildDiamondRows(count);

          float totalHeight = rows.Count * cellSize.y + (rows.Count - 1) * spacing.y;
          float startY = padding.top + (rectTransform.rect.height - padding.vertical - totalHeight) * 0.5f;

          int childIndex = 0;
          for (int row = 0; row < rows.Count; row++)
          {
              int rowCount = rows[row];
              float rowWidth = rowCount * cellSize.x + (rowCount - 1) * spacing.x;
              float startX = padding.left + (rectTransform.rect.width - padding.horizontal - rowWidth) * 0.5f;

              for (int col = 0; col < rowCount; col++)
              {
                  if (childIndex >= count)
                  {
                      return;
                  }

                  RectTransform child = children[childIndex];
                  float x = startX + col * (cellSize.x + spacing.x);
                  float y = startY + row * (cellSize.y + spacing.y);

                  SetChildAlongAxis(child, 0, x, cellSize.x);
                  SetChildAlongAxis(child, 1, y, cellSize.y);

                  childIndex++;
              }
          }
      }

      private static List<int> BuildDiamondRows(int count)
      {
          List<int> rows = new List<int>();
          int placed = 0;
          int width = 1;

          while (placed < count)
          {
              int take = Mathf.Min(width, count - placed);
              rows.Add(take);
              placed += take;
              width++;
          }

          int left = rows.Count - 2;
          while (left >= 0)
          {
              rows.Add(rows[left]);
              left--;
          }

          int total = 0;
          for (int i = 0; i < rows.Count; i++)
          {
              total += rows[i];
          }

          while (total > count && rows.Count > 0)
          {
              int last = rows.Count - 1;
              if (rows[last] > 1)
              {
                  rows[last]--;
                  total--;
              }
              else
              {
                  rows.RemoveAt(last);
                  total--;
              }
          }

          return rows;
      }
  }