using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleEffect : MonoBehaviour
{
    [SerializeField, Range(1, 5)] private float speed;
    [SerializeField] private float rippleSize;

    private void Start()
    {
        Material shader = GetComponent<SpriteRenderer>().material;
        StartCoroutine(RippleEffectCoroutine(shader));
    }

    IEnumerator RippleEffectCoroutine(Material mat)
    {
        transform.eulerAngles = new Vector3(75, 0, 0);
        float elapsed = 0f;

        while (elapsed < speed)
        {
            mat.SetFloat("_Radius", Mathf.Lerp(0, rippleSize, elapsed / speed));
            elapsed += Time.deltaTime;
            yield return null;
        }
        mat.SetFloat("_Radius", rippleSize);
        Destroy(gameObject);
    }
}

