using UnityEngine;
using System;
using System.Linq;
using System.Collections;


public class CorgiScript : MonoBehaviour
{
    public Animator animator;
    private bool isMoving;
    private bool isFacingCamera;
    private Vector3 targetPosition;
    private Action onTargetReached;

    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public Camera mainCamera;
    public LayerMask clickableLayer;

    public LayerMask foodLayer;

    private bool pendingSitCommand = false;

    private GameObject selectedFood;


    void Start()
    {
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not assigned");
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if we hit food first
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, foodLayer))
            {
                selectedFood = hit.collider.gameObject; // Assign selected food here
                Debug.Log("Selected food: " + selectedFood.name);
            }
            // Check if we hit the terrain
            else if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayer))
            {
                targetPosition = hit.point;
                selectedFood = null; // Clear selected food if clicking on terrain
                isMoving = true;
                isFacingCamera = false;
                onTargetReached = null;  // Reset the delegate

                animator.SetBool("IsWalking", true); // walking animation while moving

                // Handle modality fusion: if sit command is pending, move to target and then sit
                if (pendingSitCommand)
                {
                    pendingSitCommand = false;
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        MoveToTargetAndSit();
                    });
                }
            }
        }

        //Trigger Sitting with Keyboard
        if (Input.GetKeyDown(KeyCode.S))
        {
            TriggerSit();
        }
        // Trigger Eating with Keyboard REMOVE LATER
        if (Input.GetKeyDown(KeyCode.E))
        {
            animator.SetTrigger("Eat");
        }

        if (isMoving)
        {
            MoveToTarget();
        }
        else if (isFacingCamera)
        {
            FaceCamera();
        }
    }

    public void TriggerSit()
    {
        if (isMoving)
        {
            pendingSitCommand = true;
        }
        else
        {
            ExecuteSit();
        }
    }

    private void ExecuteSit()
    {
        Debug.Log("Sitting");


        // sit animation
        animator.SetTrigger("Sit");

        //Animator update to ensure the trigger is processed
        animator.Update(0f);

    }

    private void MoveToTargetAndSit()
    {   //sit if the object is close enough to target
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            ExecuteSit();
        }
        else
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                MoveToTarget();
                MoveToTargetAndSit();
            });
        }
    }


    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            isMoving = false;
            isFacingCamera = true;
            animator.SetBool("IsWalking", false);  // stop walking

            onTargetReached?.Invoke();
        }
    }

    void FaceCamera()
    {
        Vector3 directionToCamera = (mainCamera.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToCamera.x, 0, directionToCamera.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        if (Quaternion.Angle(transform.rotation, lookRotation) < 0.1f)
        {
            isFacingCamera = false;
        }
    }

    // method to handle the voice command
    public void VoiceCommandEat(string foodName)
    {
        if (selectedFood != null)
        {
            string sanitizedFoodName = CleanString(foodName);
            string selectedFoodName = CleanString(selectedFood.name);
            Debug.Log("Voice command food name: " + sanitizedFoodName);
            Debug.Log("Selected food name: " + selectedFoodName);
            if (selectedFoodName.Equals(sanitizedFoodName))
            {
                MoveToTargetAndEat(selectedFood.transform.position);
            }
            else
            {
                Debug.Log("Not possible, Voice Command and Selected food don't match!!");
            }
        }
        else
        {
            Debug.Log("No food selected.");
        }
    }


    // Method to move to the food and eat it
    private void MoveToTargetAndEat(Vector3 foodPosition)
    {
        targetPosition = foodPosition;
        isMoving = true;
        isFacingCamera = false;
        onTargetReached = () => StartCoroutine(FaceTargetAndEatCoroutine(foodPosition));
        animator.SetBool("IsWalking", true);
    }


    private void ExecuteEat()
    {
        Debug.Log("Eating...");

        // Start consuming animation on the food
        if (selectedFood != null)
        {
            Consumer consumer = selectedFood.GetComponent<Consumer>();
            if (consumer != null)
            {
                consumer.StartConsuming();
                StartCoroutine(CheckConsumption(consumer));
            }
        }

        animator.SetTrigger("Eat");
        animator.Update(0f);
    }


    private IEnumerator CheckConsumption(Consumer consumer)
    {
        // Wait until the consumption process is complete
        while (consumer.IsConsuming)
        {
            if (consumer.CurrentIndex == consumer.Portions.Length)
            {
                consumer.StopConsuming();
                break;
            }
            yield return null;
        }

        // Wait for 5 seconds before making the food reappear
        yield return new WaitForSeconds(5f);

        // Reactivate all portions
        foreach (GameObject portion in consumer.Portions)
        {
            portion.SetActive(true);
        }

        // Reset the currentIndex to ensure proper reactivation
        consumer.CurrentIndex = 0;
    }





    private IEnumerator FaceTargetAndEatCoroutine(Vector3 foodPosition)
    {
        // Rotate towards the food
        Vector3 direction = (foodPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float elapsedTime = 0f;
        float rotationTimeout = 2f;  // Timeout after 5 seconds to prevent getting stuck

        while (Quaternion.Angle(transform.rotation, lookRotation) > 0.1f && elapsedTime < rotationTimeout)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Execute the eat animation
        ExecuteEat();
    }



    // error handling for removing punctuation and upper case from voice commands
    private string CleanString(string input)
    {
        return new string(input.ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
    }
}
