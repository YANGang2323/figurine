using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButton : Button
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Material normalTextMaterial;
    [SerializeField] private Material hoverTextMaterial;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (label == null || normalTextMaterial == null || hoverTextMaterial == null) return;

        bool highlighted = state == SelectionState.Highlighted ||
                           state == SelectionState.Selected || state == SelectionState.Pressed;
        Material material = highlighted ? hoverTextMaterial : normalTextMaterial;
        if (label.fontSharedMaterial == material) return;
        label.fontSharedMaterial = material;
        label.UpdateMeshPadding();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        var eventSystem = EventSystem.current;
        if (eventData.button == PointerEventData.InputButton.Left &&
            eventSystem != null && eventSystem.currentSelectedGameObject == gameObject)
        {
            eventSystem.SetSelectedGameObject(null, eventData);
        }
    }
}
