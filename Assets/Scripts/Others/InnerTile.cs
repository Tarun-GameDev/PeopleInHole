using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerTile : MonoBehaviour
{
    public float fadeDuration = 1f;
    public MeshRenderer _meshRenderer;
    private Coroutine _fadeCoroutine;
    
    public void FadeFromHalfToZero()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        Color baseColor = Color.cyan;
        
        Material mat = _meshRenderer.material;
        baseColor.a = 0.5f;
        mat.color = baseColor;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0.5f, 0f, elapsed / fadeDuration);
            Color c = mat.color;
            c.a = a;
            mat.color = c;
            yield return null;
        }

        Color final = mat.color;
        final.a = 0f;
        mat.color = final;

        _fadeCoroutine = null;
    }
}
