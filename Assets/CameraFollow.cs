using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    public GameObject vehicle;
    public Vector3 offset;

    public bool mine = true;

    void Start()
    {
        if (mine) {
            this.gameObject.SetActive(true);
        } else if (mine == false) {
            this.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        Vector3 desiredPosition = vehicle.transform.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 9 * Time.deltaTime);

        Quaternion desiredRotation = vehicle.transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, 2 * Time.deltaTime);
    }
}