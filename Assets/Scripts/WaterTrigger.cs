using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController controller = other.gameObject.GetComponent<ThirdPersonController>();
        if (controller)
        {

        }
    }

    private void OnTriggerExit(Collider other)
    {
        ThirdPersonController controller = other.gameObject.GetComponent<ThirdPersonController>();
        if (controller)
        {
  
        }
    }
}
