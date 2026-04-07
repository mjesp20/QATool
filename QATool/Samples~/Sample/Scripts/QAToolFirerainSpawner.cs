using UnityEngine;
using System.Collections;

namespace QATool.Sample
{
    public class QAToolFirerainSpawner : MonoBehaviour
    {
        public Vector2 areaSize = new Vector2(10,10);
        public float spawnHeight = 12f;
        public float spawnRate = 1f;
        public float fireballFallSpeed = 5f;

        void Start()
        {
            StartCoroutine(RainRoutine());
        }

        IEnumerator RainRoutine()
        {
            while(true)
            {
                SpawnFireballWithShadow();
                yield return new WaitForSeconds(spawnRate);
            }
        }

        void SpawnFireballWithShadow()
        {
            // Pick random position in area
            Vector3 randomOffset = new Vector3(
                Random.Range(-areaSize.x/2, areaSize.x/2),
                0,
                Random.Range(-areaSize.y/2, areaSize.y/2)
            );

            Vector3 groundPos = transform.position + randomOffset;

            // Create fireball and shadow
            GameObject fireball = CreateFireball(groundPos);
            GameObject shadow = CreateShadow(groundPos);

            // Link them
            fireball.AddComponent<LinkedShadow>().shadow = shadow;
        }

        GameObject CreateShadow(Vector3 pos)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.transform.position = pos;
            shadow.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            shadow.GetComponent<Collider>().enabled = false;

            Renderer r = shadow.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0,0,0,0.4f);

            return shadow;
        }

        GameObject CreateFireball(Vector3 groundPos)
        {
            GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.transform.position = groundPos + Vector3.up * spawnHeight;
            fireball.transform.localScale = Vector3.one * 0.7f;

            Renderer r = fireball.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = Color.red;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", Color.red * 2);

            Rigidbody rb = fireball.AddComponent<Rigidbody>();
            rb.useGravity = false;

            QAToolFireball script = fireball.AddComponent<QAToolFireball>();
            script.fallSpeed = fireballFallSpeed;
            script.destroyY = -10;

            return fireball;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x,0.1f,areaSize.y));
        }
    }

    // This small helper links the shadow to the fireball
    public class LinkedShadow : MonoBehaviour
    {
        public GameObject shadow;

        void Update()
        {
            if(shadow)
            {
                // Keep shadow on ground beneath fireball
                Vector3 shadowPos = shadow.transform.position;
                shadowPos.x = transform.position.x;
                shadowPos.z = transform.position.z;
                shadow.transform.position = shadowPos;
            }
        }

        void OnDestroy()
        {
            if(shadow)
                Destroy(shadow);
        }
    }
}