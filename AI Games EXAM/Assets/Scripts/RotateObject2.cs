using UnityEngine;
using UnityEngine.SceneManagement;

public class RotateObject2 : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(30f, 45f, 60f); // degrees per second

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene("Level2");
             Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; // Ensure it's unlocked
        }
    }
}
