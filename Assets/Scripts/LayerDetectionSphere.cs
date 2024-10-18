using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A class to manage the environment detection.
 */
[Serializable]
public class LayerDetectionSphere
{
    [Header("Data")]
    [Tooltip("Check is done at transform location. Allow for a y offset.")]
    [SerializeField] private float detectionOffset = 0.0f;
    [Tooltip("Sphere radius.")]
    [SerializeField] private float detectionRadius = 0.28f;
    [Tooltip("The layers the sphere should check.")]
    [SerializeField] private LayerMask layers;

#if UNITY_EDITOR
    [Header("Draw")]
    [Tooltip("Display the sphere used to detection. Useful for debug and setup. Will be display in scene tabs.")]
    [SerializeField] private bool draw = false;
    [SerializeField] private Color drawColor;
#endif

    // Does an object with an associated layer is present into the sphere ?
    public bool CheckSphere(Vector3 spherePosition)
    {
        spherePosition.y += detectionOffset;
        return Physics.CheckSphere(spherePosition, detectionRadius, layers);
    }

#if UNITY_EDITOR
    // Draw the sphere. Useful for debug and setup.
    public void Draw(Vector3 spherePosition)
    {
        if (draw)
        {
            Gizmos.color = drawColor;
            spherePosition.y += detectionOffset;
            Gizmos.DrawSphere(spherePosition, detectionRadius);
        }
    }
#endif
}
