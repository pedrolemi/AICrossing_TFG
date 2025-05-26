using UnityEngine;

[CreateAssetMenu(fileName = "Base Item", menuName = "Items/Base Item")]
public class BaseItem : ScriptableObject
{
    public enum ITEMS { 
        NONE = -1, BEETROOT, CABBAGE, CARROT, CAULIFLOWER, EGG, KALE, MILK, PARSNIP, POTATO, PUMPKIN, RADISH, ROCK, SUNFLOWER, WHEAT, WOOD,
        AXE, HAMMER, PICKAXE, ROD, SHOVEL, SWORD, WATER_CAN
    };

    public ITEMS id;

    public string itemName = "";
    public string pluralItemName = "";
    public Sprite icon;

    public bool keyItem = false;
}
