using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AnimalMatching/Item")]
public sealed class Item : ScriptableObject
{
    public int value;
    public string type;
    public Sprite sprite;
}
