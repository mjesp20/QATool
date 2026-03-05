using System.Collections;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public float cooldownDuration = 3f;

    public Material readyMaterial;
    public Material cooldownMaterial;

    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        _renderer.material = cooldownMaterial;

        yield return new WaitForSeconds(cooldownDuration);

        _renderer.material = readyMaterial;
    }
}