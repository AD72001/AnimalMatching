using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AnimalMatching/Item")]
/* 
Item in the tiles.
--Value refers to the score player got after popping the item.
--Type: Ordinary, FourTilesPiece, UniversalPiece, DoubleThreePiece.
--Sprite: Item sprite.
*/ 
public sealed class Item : ScriptableObject
{
    public int value;
    public string type;
    public Sprite sprite;
}
