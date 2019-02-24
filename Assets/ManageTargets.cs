using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageTargets : MonoBehaviour
{
    public Slicing player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Slicing>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!player.targetList.Contains(other.gameObject))
        {
            player.targetList.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (player.targetList.Contains(other.gameObject))
        {
            player.targetList.Remove(other.gameObject);
        }
    }
}
