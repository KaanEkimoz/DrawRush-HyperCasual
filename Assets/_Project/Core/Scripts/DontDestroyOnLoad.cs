using UnityEngine;

//Script for attach to the DontDestroyOnLoad Objects
public class DontDestroyOnLoad : MonoBehaviour
{
    [HideInInspector] public string objectID;
    private DontDestroyOnLoad[] _dontDestroyObjects;
    private void Awake()
    {
        objectID = name + transform.position.ToString() + transform.eulerAngles.ToString();
        _dontDestroyObjects = FindObjectsOfType<DontDestroyOnLoad>();
    }
    private void Start()
    {
        for (int i = 0; i < _dontDestroyObjects.Length; i++)
        {
            if (_dontDestroyObjects[i] != this)
            {
                if (_dontDestroyObjects[i].objectID == objectID)
                {
                    Destroy(gameObject);
                }
            }
        }
        DontDestroyOnLoad(gameObject);
    }
}
