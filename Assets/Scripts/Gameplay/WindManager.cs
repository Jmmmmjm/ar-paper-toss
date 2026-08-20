using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the wind direction and speed.
/// Changes wind periodically and applies horizontal crosswinds to flying paper balls.
/// </summary>
public class WindManager : MonoBehaviour
{
    public static WindManager Instance { get; private set; }

    [Header("Wind Settings")]
    [Tooltip("Min and Max wind speed in MPH.")]
    [SerializeField] private float _minSpeed = 0f;
    [SerializeField] private float _maxSpeed = 8f;

    [Tooltip("How often wind changes (seconds).")]
    [SerializeField] private float _changeInterval = 6f;

    [Tooltip("Multiplier converting speed (MPH) to Unity physics force.")]
    [SerializeField] private float _forceMultiplier = 0.08f;

    public float CurrentSpeedMPH { get; private set; }
    public Vector3 CurrentWindVector { get; private set; } // World space force
    public float CurrentDirectionSign { get; private set; } // -1 for Left, +1 for Right

    public static event Action<float, float> OnWindChanged; // speedMPH, directionSign (-1 to 1)

    private Camera _cam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _cam = Camera.main;
        StartCoroutine(WindCycleRoutine());
    }

    private IEnumerator WindCycleRoutine()
    {
        while (true)
        {
            GenerateNewWind();
            yield return new WaitForSeconds(_changeInterval);
        }
    }

    private void GenerateNewWind()
    {
        if (_cam == null) _cam = Camera.main;

        // Choose random speed and direction (-1 = left, +1 = right)
        CurrentSpeedMPH = Mathf.Round(UnityEngine.Random.Range(_minSpeed, _maxSpeed) * 10f) / 10f;
        CurrentDirectionSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        // Wind blows perpendicular to camera view (crosswind)
        Vector3 windDir = _cam != null ? _cam.transform.right * CurrentDirectionSign : Vector3.right * CurrentDirectionSign;
        CurrentWindVector = windDir * (CurrentSpeedMPH * _forceMultiplier);

        OnWindChanged?.Invoke(CurrentSpeedMPH, CurrentDirectionSign);
    }
}
