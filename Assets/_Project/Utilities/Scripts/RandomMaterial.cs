using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMaterial : MonoBehaviour
{
    public Material[] randomMaterials;
    void Start ()  
    {
        
            gameObject.GetComponent<Renderer>().material = randomMaterials[Random.Range(0, randomMaterials.Length)];
    }
}