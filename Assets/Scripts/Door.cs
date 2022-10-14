using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isOpen = false;
    public bool isClose = true;
    [SerializeField] public float doorTimer = 10f; // changed from private to public
    public float speedScale = 1;

    void Update()
    {
        if (isOpen)
        {
            OpenDoor();
            isClose = false;
        }
        if (doorTimer <= 5)
        {
            isOpen = false;
        }
        if (!isClose && !isOpen)
        {
            CloseDoor();
        }
        if (doorTimer <= 0)
        {
            isClose = true;
            doorTimer = 10f;
        }

        // This should only countdown for as long as the player is touching the switch
        // or alternatively, until the door reaches the bottom.
        if(doorTimer>=0)
        {
            doorTimer -= Time.deltaTime;
        }
    }

    public void OpenDoor()
    {
        transform.Translate(Vector2.up * Time.deltaTime * speedScale);
    }

    public void CloseDoor()
    {
        transform.Translate(Vector2.down * Time.deltaTime * speedScale);
    }
}
