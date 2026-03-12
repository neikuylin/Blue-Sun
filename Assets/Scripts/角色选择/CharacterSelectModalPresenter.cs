using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectModalPresenter
{
    private readonly List<ModalCanvasPatch> modalCanvasPatches = new List<ModalCanvasPatch>();
    private GameObject modalOverlay;

    private struct ModalCanvasPatch
    {
        public Transform target;
        public Canvas canvas;
        public bool canvasAdded;
        public bool previousOverrideSorting;
        public int previousSortingOrder;
        public GraphicRaycaster raycaster;
        public bool raycasterAdded;
    }

    public void Enter(OpenCharacterSelect opener, CharacterSlotView currentSlot)
    {
        if (opener == null || opener.characterPanel == null)
        {
            return;
        }

        Exit(false, null, null);
        EnsureModalOverlay(opener.characterPanel);
        ElevateAllowedForModal(opener, currentSlot);
    }

    public void Exit(bool closePanel, GameObject currentCharacterPanel, OpenCharacterSelect opener)
    {
        if (closePanel && currentCharacterPanel != null)
        {
            currentCharacterPanel.SetActive(false);
            if (opener != null)
            {
                opener.MarkClosedFromOutside();
            }
        }

        ClearModalCanvasPatches();

        if (modalOverlay != null)
        {
            modalOverlay.SetActive(false);
        }
    }

    public void Dispose()
    {
        Exit(false, null, null);
    }

    private void EnsureModalOverlay(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Canvas rootCanvas = panel.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            return;
        }

        if (modalOverlay == null)
        {
            modalOverlay = new GameObject("CharacterSelectModalOverlay", typeof(RectTransform), typeof(Image));
        }

        RectTransform rt = modalOverlay.GetComponent<RectTransform>();
        rt.SetParent(rootCanvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = modalOverlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);
        image.raycastTarget = true;

        modalOverlay.SetActive(true);
        modalOverlay.transform.SetAsLastSibling();
    }

    private void ElevateAllowedForModal(OpenCharacterSelect opener, CharacterSlotView currentSlot)
    {
        ClearModalCanvasPatches();

        if (modalOverlay == null)
        {
            return;
        }

        ElevateTransformWithCanvas(opener.characterPanel.transform, 5001);

        if (currentSlot != null)
        {
            ElevateTransformWithCanvas(currentSlot.transform, 5002);

            for (int i = 0; i < currentSlot.selectButtons.Count; i++)
            {
                if (currentSlot.selectButtons[i] != null)
                {
                    ElevateTransformWithCanvas(currentSlot.selectButtons[i].transform, 5003);
                }
            }
        }

        ElevateTransformWithCanvas(opener.transform, 5004);
    }

    private void ElevateTransformWithCanvas(Transform tr, int sortingOrder)
    {
        if (tr == null)
        {
            return;
        }

        for (int i = 0; i < modalCanvasPatches.Count; i++)
        {
            if (modalCanvasPatches[i].target != tr)
            {
                continue;
            }

            ModalCanvasPatch existing = modalCanvasPatches[i];
            if (existing.canvas != null)
            {
                existing.canvas.overrideSorting = true;
                existing.canvas.sortingOrder = Mathf.Max(existing.canvas.sortingOrder, sortingOrder);
            }

            modalCanvasPatches[i] = existing;
            return;
        }

        Canvas canvas = tr.GetComponent<Canvas>();
        bool canvasAdded = false;
        if (canvas == null)
        {
            canvas = tr.gameObject.AddComponent<Canvas>();
            canvasAdded = true;
        }

        GraphicRaycaster raycaster = tr.GetComponent<GraphicRaycaster>();
        bool raycasterAdded = false;
        if (raycaster == null)
        {
            raycaster = tr.gameObject.AddComponent<GraphicRaycaster>();
            raycasterAdded = true;
        }

        ModalCanvasPatch patch = new ModalCanvasPatch
        {
            target = tr,
            canvas = canvas,
            canvasAdded = canvasAdded,
            previousOverrideSorting = canvas.overrideSorting,
            previousSortingOrder = canvas.sortingOrder,
            raycaster = raycaster,
            raycasterAdded = raycasterAdded
        };

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        modalCanvasPatches.Add(patch);
    }

    private void ClearModalCanvasPatches()
    {
        for (int i = modalCanvasPatches.Count - 1; i >= 0; i--)
        {
            ModalCanvasPatch patch = modalCanvasPatches[i];
            if (patch.target == null)
            {
                continue;
            }

            if (patch.raycasterAdded && patch.raycaster != null)
            {
                Object.Destroy(patch.raycaster);
            }

            if (patch.canvas != null)
            {
                if (patch.canvasAdded)
                {
                    Object.Destroy(patch.canvas);
                }
                else
                {
                    patch.canvas.overrideSorting = patch.previousOverrideSorting;
                    patch.canvas.sortingOrder = patch.previousSortingOrder;
                }
            }
        }

        modalCanvasPatches.Clear();
    }
}
