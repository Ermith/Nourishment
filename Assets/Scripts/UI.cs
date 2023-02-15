using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ui : MonoBehaviour
{
    float _marker1;
    Camera _camera;

    public Text Text1;
    public Image Image1;
    public Canvas Canvas;

    // Start is called before the first frame update
    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        float d = 10;

        //float startingPosition = -_camera.transform.position.y + (_camera.transform.position.y % d) + float.Epsilon;
        float distance = -_camera.transform.position.y - _marker1 + float.Epsilon;

        if (distance > d) _marker1 += d * 2;
        if (distance < -d) _marker1 -= d * 2;

        distance = -_camera.transform.position.y - _marker1 + float.Epsilon;

        float perc = distance / _camera.orthographicSize / 2;

        Vector2 p = Text1.rectTransform.anchoredPosition;
        p.y = Canvas.renderingDisplaySize.y * perc;
        Text1.rectTransform.anchoredPosition = p;

        p = Image1.rectTransform.anchoredPosition;
        p.y = Canvas.renderingDisplaySize.y * perc;
        Image1.rectTransform.anchoredPosition = p;

        Text1.text = $"{_marker1}";
    }
}
