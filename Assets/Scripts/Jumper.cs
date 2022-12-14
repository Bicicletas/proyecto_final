using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumper : MonoBehaviour
{
    [Header("Jumper Parameters\n")]
    public float force = 10f;
    public float delay = 0.2f;

    private bool canJump = true;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.name.Equals("Player") && canJump)
        {
            StartCoroutine(Delay(other));
        }
    }

    IEnumerator Delay(Collision other)
    {
        canJump = false;
        yield return new WaitForSeconds(delay);
        canJump = true;

        Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();

        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }
}
