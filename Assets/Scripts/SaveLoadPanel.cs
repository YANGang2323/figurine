using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SaveLoadPanel : MonoBehaviour
{
    [SerializeField] private Button[] slots;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public void Bind(UnityAction<int> selectSlot, UnityAction close,
        UnityAction confirm = null, UnityAction cancel = null)
    {
        for (int index = 0; index < slots.Length; index++)
        {
            int slot = index;
            slots[index].onClick.AddListener(() => selectSlot(slot));
        }

        closeButton.onClick.AddListener(close);
        confirmation.SetActive(false);

        if (confirm != null)
        {
            confirmButton.onClick.AddListener(confirm);
            cancelButton.onClick.AddListener(cancel);
        }
    }
}
