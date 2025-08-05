using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    [SerializeField] HoleColor holeColor;

    public HoleColor HoleColor { get => holeColor; set => holeColor = value; }
}
