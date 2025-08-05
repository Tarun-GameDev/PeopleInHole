using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    private int slotCount = 0;
    private HoleColor slotColor = HoleColor.None;

    public int SlotCount { get => slotCount; set => slotCount = value; }
    public HoleColor SlotColor { get => slotColor; set => slotColor = value; }
}
