using UnityEngine;

public class HealthBoost : MonoBehaviour
{
    public int healAmount;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        //Physics.IgnoreCollision(player.GetComponent<Collider>(), GetComponent<Collider>());
    }

    private void OnTriggerEnter(Collider collision)
    {
        // check if you hit an enemy
        if (collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            PlayerMovement playerScript = collision.gameObject.GetComponent<PlayerMovement>();

            if (playerScript.currentHealth != playerScript.MaxHealth)
            {
                playerScript.PlayerHealth(healAmount);

                // destroy projectile
                Destroy(gameObject);
            }
        }
    }
}
