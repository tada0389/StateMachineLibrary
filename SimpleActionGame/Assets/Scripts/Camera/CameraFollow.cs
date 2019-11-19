using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ごみすくりぷと

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    private void LateUpdate()
    {
        transform.position = new Vector3(target.transform.position.x, target.transform.position.y, -10f);

        if (transform.position.y < -20f || Input.GetKeyDown(KeyCode.A)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
