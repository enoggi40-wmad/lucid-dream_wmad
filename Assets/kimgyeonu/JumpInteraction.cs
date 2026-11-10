using UnityEngine;

public class JumpInteraction : MonoBehaviour
{
    bool playerNear = false;

    void Update()
    {
        if (playerNear)
        {
            Debug.Log("F키를 누르시오");

            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("뜀틀 사용!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}