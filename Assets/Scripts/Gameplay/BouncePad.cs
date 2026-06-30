using System.Collections.Generic;
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private float bounceVelocity = 9f;
    [SerializeField] private float cooldown = 0.18f;
    [SerializeField] private string[] affectedTags = { "Player", "Box" };

    private readonly Dictionary<Rigidbody, float> nextBounceTimes = new Dictionary<Rigidbody, float>();

    private void OnCollisionEnter(Collision collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryBounce(collision);
    }

    private void TryBounce(Collision collision)
    {
        Rigidbody targetRigidbody = collision.rigidbody;
        if (targetRigidbody == null || targetRigidbody.isKinematic)
        {
            return;
        }

        if (!CanAffect(collision.gameObject) || !HasTopContact(collision))
        {
            return;
        }

        if (nextBounceTimes.TryGetValue(targetRigidbody, out float nextTime) && Time.time < nextTime)
        {
            return;
        }

        Vector3 velocity = targetRigidbody.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, bounceVelocity);
        targetRigidbody.linearVelocity = velocity;
        targetRigidbody.WakeUp();
        nextBounceTimes[targetRigidbody] = Time.time + cooldown;
    }

    private bool CanAffect(GameObject target)
    {
        if (affectedTags == null || affectedTags.Length == 0)
        {
            return true;
        }

        foreach (string affectedTag in affectedTags)
        {
            if (!string.IsNullOrWhiteSpace(affectedTag) && target.CompareTag(affectedTag))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasTopContact(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.y) > 0.35f && contact.point.y >= transform.position.y)
            {
                return true;
            }
        }

        return false;
    }
}
