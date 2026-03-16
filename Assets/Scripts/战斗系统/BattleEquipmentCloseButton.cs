using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleEquipmentCloseButton : MonoBehaviour
{
    public void Close()
    {
        BattlePartyPortraitBinder.CloseEquipmentPanel();
    }
}
