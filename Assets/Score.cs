using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Score : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<TMP_Text>().text = $"Depth reached {-Player.Lowest}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
