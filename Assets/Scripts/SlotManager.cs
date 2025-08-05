using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotManager : MonoBehaviour
{
    HashSet<Slot> slots = new HashSet<Slot>();

    private void Awake()
    {
        slots = new HashSet<Slot>(GetComponentsInChildren<Slot>());
    }
}
