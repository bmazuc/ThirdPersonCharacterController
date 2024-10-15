using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LayerDetectionSphere
{
    [Header("Data")]
    [SerializeField] private float detectionOffset = 0.0f;
    [SerializeField] private float detectionRadius = 0.28f;
    [SerializeField] private LayerMask layers;

    [Header("Draw")]
    [SerializeField] private bool draw = false;
    [SerializeField] private Color drawColor;

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
