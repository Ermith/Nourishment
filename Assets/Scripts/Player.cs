using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int X = World.MAP_WIDTH / 2;
    public int Y = 0;
    public Camera camera;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = new Vector3(X - World.MAP_WIDTH / 2 - 0.5f, Y - 0.5f, 0);
        camera.transform.position = new Vector3(0, Y - 0.5f, -10);

        if (Input.GetKeyDown(KeyCode.W))
            Y++;
        if (Input.GetKeyDown(KeyCode.S))
            Y--;
        if (Input.GetKeyDown(KeyCode.A))
            X--;
        if (Input.GetKeyDown(KeyCode.D))
            X++;
    }
}
