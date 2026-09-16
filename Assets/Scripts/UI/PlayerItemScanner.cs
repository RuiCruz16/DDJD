using UnityEngine;

public class PlayerItemScanner : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;

    private ItemControlsUI currentTargetUI;

    void Update()
    {
        if (inventory == null)
            return;

        ItemControlsUI newTargetUI = inventory.CurrentLookAtUI;

        if (currentTargetUI != newTargetUI)
        {
            if (currentTargetUI != null)
                currentTargetUI.HideUI();

            currentTargetUI = newTargetUI;

            if (currentTargetUI != null)
                currentTargetUI.ShowUI();
        }

        if (currentTargetUI == null)
            ClearUI();
    }

    private void ClearUI()
    {
        if (currentTargetUI != null)
        {
            currentTargetUI.HideUI();
            currentTargetUI = null;
        }
    }
}
