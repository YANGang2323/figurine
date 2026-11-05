using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButton : Button
{
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
