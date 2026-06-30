using UnityEngine;
using System;

public class example : MonoBehaviour
{


    public Func<int, int, bool> exampleFunc;
    void Start()
    {
       exampleFunc = (a, b) => a > b;
        bool result = exampleFunc(5, 3);
        Debug.Log($"Result of exampleFunc: {result}");
    }
}
