using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class DontDestroyOnLoad : MonoBehaviour
{
    [HideInInspector] public string objectID;
    private DontDestroyOnLoad[] dontDestroyObjects;
    private void Awake()
    {
        objectID = name + transform.position.ToString() + transform.eulerAngles.ToString();
        dontDestroyObjects = FindObjectsOfType<DontDestroyOnLoad>();
    }

    private void Start()
    {
        for (int i = 0; i < dontDestroyObjects.Length; i++)
        {
            if (dontDestroyObjects[i] != this)
            {
                if (dontDestroyObjects[i].objectID == objectID)
                {
                    Destroy(gameObject);
                }
            }
        }
        DontDestroyOnLoad(gameObject);
    }
}
