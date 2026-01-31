using System;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static Main Instance;
    public PlayerController PlayerController;

    private void Awake()
    {
        Instance = this;
    }
}
