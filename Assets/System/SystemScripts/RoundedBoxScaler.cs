using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(BoxCollider2D))]
public class RoundedBoxScaler : MonoBehaviour
{
    public float roundedCornderNormalValue = 0.15f;
    private BoxCollider2D boxCollider2D;

    void Update()
    {
        if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.edgeRadius = transform.localScale.x * roundedCornderNormalValue;
    }
}
