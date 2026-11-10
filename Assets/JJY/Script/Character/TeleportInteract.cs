using UnityEngine;

public class TeleportInteract : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Rotation Settings")]
    [SerializeField] private bool useDestinationRotation = false;
    [SerializeField] private bool rotateY180 = false;

    private GameObject playerInRange;
    private CharacterController characterController;

    private void Update()
    {
        if (playerInRange == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        if (destinationPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Destination Point가 연결되어 있지 않습니다.");
            return;
        }

        GameObject player = playerInRange;

        if (characterController != null)
            characterController.enabled = false;

        player.transform.position = destinationPoint.position;

        if (useDestinationRotation)
        {
            player.transform.rotation = Quaternion.Euler(
                0f,
                destinationPoint.eulerAngles.y,
                0f
            );
        }

        if (rotateY180)
        {
            player.transform.rotation = Quaternion.Euler(
                0f,
                player.transform.eulerAngles.y + 180f,
                0f
            );
        }

        if (characterController != null)
            characterController.enabled = true;

        playerInRange = null;
        characterController = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = other.gameObject;
        characterController = other.GetComponent<CharacterController>();

        Debug.Log($"{gameObject.name} 범위 진입");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.gameObject == playerInRange)
        {
            playerInRange = null;
            characterController = null;

            Debug.Log($"{gameObject.name} 범위 이탈");
        }
    }
}