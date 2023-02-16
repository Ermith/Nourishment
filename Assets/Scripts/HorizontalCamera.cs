using UnityEngine;

[ExecuteInEditMode]
public class HorizontalCamera : MonoBehaviour
{
    private Camera m_camera;
    private float lastAspect;

    [SerializeField]
    private float m_fieldOfView = 60f;
    public float FieldOfView
    {
        get { return m_fieldOfView; }
        set
        {
            if (m_fieldOfView != value)
            {
                m_fieldOfView = value;
                RefreshCamera();
            }
        }
    }

    [SerializeField]
    private float m_orthographicSize = 5f;
    public float OrthographicSize
    {
        get { return m_orthographicSize; }
        set
        {
            if (m_orthographicSize != value)
            {
                m_orthographicSize = value;
                RefreshCamera();
            }
        }
    }

    private void OnEnable()
    {
        RefreshCamera();
    }

    private void Update()
    {
        float aspect = m_camera.aspect;
        if (aspect != lastAspect)
            AdjustCamera(aspect);
    }

    public void RefreshCamera()
    {
        if (m_camera == null)
            m_camera = GetComponent<Camera>();

        AdjustCamera(m_camera.aspect);
    }

    private void AdjustCamera(float aspect)
    {
        lastAspect = aspect;

        // Credit: https://forum.unity.com/threads/how-to-calculate-horizontal-field-of-view.16114/#post-2961964
        float _1OverAspect = 1f / aspect;
        m_camera.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(m_fieldOfView * Mathf.Deg2Rad * 0.5f) * _1OverAspect) * Mathf.Rad2Deg;
        m_camera.orthographicSize = m_orthographicSize * _1OverAspect;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshCamera();
    }
#endif
}
