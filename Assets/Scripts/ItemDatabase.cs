using UnityEngine;

public static class ItemDatabase
{
    public static Item[] Items {get; set;}
    public static Item[] FourPieceItems { get; set;}
    public static Item[] DoubleThreeItems {get; set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]private static void Initialize() {
        Items = Resources.LoadAll<Item>(path:"Items/Ordinary/");
        FourPieceItems = Resources.LoadAll<Item>(path:"Items/FourPiece/");
        DoubleThreeItems = Resources.LoadAll<Item>(path:"Items/DoubleThree/");
    } 
}
