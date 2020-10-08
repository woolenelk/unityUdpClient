using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkID : MonoBehaviour
{
    [SerializeField]
    public string Id;
    public Color color = new Color(0, 0, 0);
    public Vector3 cubePosition = new Vector3(0,0,0);
    public bool clientCube = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = color;
        if (!clientCube)
            transform.position = cubePosition;

    }
}
