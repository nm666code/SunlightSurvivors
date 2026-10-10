using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Needed to allow the player to move
using UnityEngine.SceneManagement; // Needed to load levels and restart current level

public class PlayerControls : MonoBehaviour
{
    public float playerSpeed;
    public InputAction controller;
    public Vector2 dir2D;
    private Rigidbody playerBox; // Will be used for collision detection for enemy entities
    public float jumpHeight = 10.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller.Enable();
        playerBox = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        dir2D = controller.ReadValue<Vector2>();
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed * dir2D.y);
        transform.Translate(Vector3.right * Time.deltaTime * playerSpeed * dir2D.x);

        if (transform.position.y <= -10)
        {
            Debug.Log("You died!");
            StartCoroutine(PauseRestartCoroutine());
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Print out the debug messages to test if player collides with any object.
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Collided with an enemy object");
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Collided with an obstacle object");
        }
        else if (collision.gameObject.name == "Dummy")
        {
            Debug.Log("Collided with a dummy object");
        }
        else if (collision.gameObject.CompareTag("Bouncy"))
        {
            Debug.Log("Bounced off a bouncy object!");
            playerBox.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.name == "Powerup")
        {
            Debug.Log("Picked up a powerup object");
        }
    }

    private IEnumerator PauseRestartCoroutine()
    {
        yield return new WaitForSeconds(10);
    }
}
