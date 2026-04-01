using UnityEngine;
using Slime;

[RequireComponent(typeof(Collider))]
public class TemperatureTrigger : MonoBehaviour
{
    [Header("Temperature Source Settings")]
    [Tooltip("If checked, this block heats up the slime (Ice -> Slime -> Air). If unchecked, this block cools down the slime (Air -> Slime -> Ice).")]
    public bool isHotCube = true;

    private void Start()
    {
        // Automatically make sure this block can be walked through without colliding hard
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        // Dynamically find the Slime physics script on whatever touched us!
        Slime_PBF slime = other.GetComponentInParent<Slime_PBF>();
        
        if (slime != null)
        {
            if (isHotCube) 
            {
                slime.HeatUp();
            }
            else 
            {
                slime.CoolDown();
            }
        }
    }
}
