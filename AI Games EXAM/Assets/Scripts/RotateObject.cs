using UnityEngine;
using UnityEngine.SceneManagement;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(30f, 45f, 60f); // degrees per second
    public string levelName = "Level2"; // Set this in the Inspector

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene(levelName);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
