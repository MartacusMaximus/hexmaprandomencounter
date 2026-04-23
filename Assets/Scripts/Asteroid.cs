using UnityEngine;
 
public class Asteroid : MonoBehaviour
{
    [SerializeField] float minSpeed = 2f;
    [SerializeField] float maxSpeed = 10f;

    void Start()
    {
        Vector3 direction = new Vector3(
            Random.Range(-1f, 1f), 0f,Random.Range(-1f, 1f)).normalized;

        float speed = Random.Range(minSpeed, maxSpeed);

        GetComponent<Rigidbody>().linearVelocity = direction * speed;

    } 

}