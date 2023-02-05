using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    float marker1;
    Camera _camera;

    public Text text1;
    public Image image1;
    public Canvas _canvas;

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
        float distance = -_camera.transform.position.y - marker1 + float.Epsilon;

        if (distance > d) marker1 += d * 2;
        if (distance < -d) marker1 -= d * 2;

        distance = -_camera.transform.position.y - marker1 + float.Epsilon;

        float perc = distance / _camera.orthographicSize / 2;

        Vector2 p = text1.rectTransform.anchoredPosition;
        p.y = _canvas.renderingDisplaySize.y * perc;
        text1.rectTransform.anchoredPosition = p;

        p = image1.rectTransform.anchoredPosition;
        p.y = _canvas.renderingDisplaySize.y * perc;
        image1.rectTransform.anchoredPosition = p;

        text1.text = $"{marker1}";
    }
}
