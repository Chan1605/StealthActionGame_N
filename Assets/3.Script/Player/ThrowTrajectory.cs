using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ThrowTrajectory : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField] private int maxPoints = 60;
    [SerializeField] private float timeStep = 0.04f;
    [SerializeField] private float maxDistance = 30f;
    [SerializeField] private LayerMask blockMask = ~0;

    [Header("Line")]
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private Color lineColor = new Color(0.4f, 0.9f, 1f, 0.9f);

    [Header("Landing Circle")]
    [SerializeField] private bool isCircleEnabled = true;
    [SerializeField] private LineRenderer circleLine;
    [SerializeField] private float circleRadius = 0.35f;
    [SerializeField] private int circleSegments = 36;
    [SerializeField] private float circleWidth = 0.03f;
    [SerializeField] private Color circleColor = new Color(0.4f, 0.9f, 1f, 0.9f);
    [SerializeField] private float surfaceLift = 0.03f;

    [Header("Extra Marker")]
    [SerializeField] private Transform landingMarker;

    private LineRenderer _line;
    private readonly List<Vector3> _points = new List<Vector3>();

    public bool isShowing { get; private set; }

    public bool isLandingFound { get; private set; }

    public Vector3 landingPoint { get; private set; }

    public Vector3 landingNormal { get; private set; }

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        SetupLine(_line, lineWidth, lineColor, false);
        _line.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);

        if (circleLine == null && isCircleEnabled)
        {
            circleLine = CreateCircleLine();
        }

        if (circleLine != null)
        {
            SetupLine(circleLine, circleWidth, circleColor, true);
            circleLine.positionCount = Mathf.Max(8, circleSegments);
        }

        landingNormal = Vector3.up;

        Hide();
    }

    private LineRenderer CreateCircleLine()
    {
        GameObject holder = new GameObject("LandingCircle");
        holder.transform.SetParent(transform, false);

        return holder.AddComponent<LineRenderer>();
    }

    private void SetupLine(LineRenderer line, float width, Color color, bool isLoop)
    {
        line.useWorldSpace = true;
        line.loop = isLoop;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startColor = color;
        line.endColor = color;

        if (line.sharedMaterial == null)
        {
            line.material = CreateDefaultMaterial();
        }
    }

    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = "ThrowTrajectory (Runtime)";

        return material;
    }

    public void Show(Vector3 origin, Vector3 velocity)
    {
        Simulate(origin, velocity);

        _line.positionCount = _points.Count;
        for (int i = 0; i < _points.Count; i++)
        {
            _line.SetPosition(i, _points[i]);
        }

        _line.enabled = _points.Count > 1;

        DrawCircle();

        if (landingMarker != null)
        {
            landingMarker.gameObject.SetActive(isLandingFound);

            if (isLandingFound)
            {
                landingMarker.SetPositionAndRotation(
                    landingPoint + landingNormal * surfaceLift,
                    Quaternion.FromToRotation(Vector3.up, landingNormal));
            }
        }

        isShowing = true;
    }

    public void Hide()
    {
        if (_line != null)
        {
            _line.positionCount = 0;
            _line.enabled = false;
        }

        if (circleLine != null)
        {
            circleLine.enabled = false;
        }

        if (landingMarker != null)
        {
            landingMarker.gameObject.SetActive(false);
        }

        isShowing = false;
        isLandingFound = false;
    }

    private void DrawCircle()
    {
        if (circleLine == null)
        {
            return;
        }

        if (!isCircleEnabled || !isLandingFound)
        {
            circleLine.enabled = false;
            return;
        }

        int segments = Mathf.Max(8, circleSegments);

        if (circleLine.positionCount != segments)
        {
            circleLine.positionCount = segments;
        }

        Vector3 normal = landingNormal.sqrMagnitude > 0.0001f ? landingNormal.normalized : Vector3.up;
        Vector3 center = landingPoint + normal * surfaceLift;

        Vector3 axisA = Vector3.Cross(normal, Vector3.up);

        if (axisA.sqrMagnitude < 0.0001f)
        {
            axisA = Vector3.Cross(normal, Vector3.forward);
        }

        axisA.Normalize();

        Vector3 axisB = Vector3.Cross(normal, axisA).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector3 offset = (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * circleRadius;

            circleLine.SetPosition(i, center + offset);
        }

        circleLine.enabled = true;
    }

    private void Simulate(Vector3 origin, Vector3 velocity)
    {
        _points.Clear();
        isLandingFound = false;
        landingPoint = origin;
        landingNormal = Vector3.up;

        Vector3 current = origin;
        Vector3 gravity = Physics.gravity;
        float traveled = 0f;

        _points.Add(current);

        for (int i = 1; i < maxPoints; i++)
        {
            float t = timeStep * i;
            Vector3 next = origin + velocity * t + gravity * (0.5f * t * t);

            Vector3 segment = next - current;
            float length = segment.magnitude;

            if (length > 0.0001f)
            {
                if (Physics.Raycast(current, segment / length, out RaycastHit hit, length, blockMask, QueryTriggerInteraction.Ignore))
                {
                    _points.Add(hit.point);

                    landingPoint = hit.point;
                    landingNormal = hit.normal;
                    isLandingFound = true;

                    return;
                }
            }

            _points.Add(next);

            traveled += length;
            current = next;

            if (traveled >= maxDistance)
            {
                return;
            }
        }
    }
}
