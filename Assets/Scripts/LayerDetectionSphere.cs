using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LayerDetectionSphere
{
    [Header("Data")]
    public float detectionOffset;
    public float detectionRadius;
    public LayerMask layers;

    [Header("Draw")]
    public bool draw;
    public Color drawColor;

    public LayerDetectionSphere(float inDetectionOffset, float inDetectionRadius)
    {
        detectionOffset = inDetectionOffset;
        detectionRadius = inDetectionRadius;
        layers = new LayerMask();

        draw = false;
        drawColor = Color.white;
    }

    public bool CheckSphere(Vector3 spherePosition)
    {
        spherePosition.y += detectionOffset;
        return Physics.CheckSphere(spherePosition, detectionRadius, layers);
    }

    public void Draw(Vector3 spherePosition)
    {
        if (draw)
        {
            Gizmos.color = drawColor;
            spherePosition.y += detectionOffset;
            Gizmos.DrawSphere(spherePosition, detectionRadius);
        }
    }
}
