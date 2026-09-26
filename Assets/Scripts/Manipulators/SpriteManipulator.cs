using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteManipulator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private Sprite originalSprite;
    [Tooltip("Sprites that this component can change a sprite renderer's sprite to.  When choosing via an event, choose the index of the sprite, starting with 0.")]
    public Sprite[] presetSprites;

    private Color originalColor = Color.white;
    [Tooltip("Colors that this component can change a sprite renderer's color to.  When choosing via an event, choose the index of the color, starting with 0.")]
    public Color[] presetColors;

    [Space]
    [Header("Other Object Values")]
    [Tooltip("When changing a triggered object's hue, saturation, lightness, or alpha, the trigger'd objects values will change to this amount.")]
    public float changeAmount = 0;

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalSprite = spriteRenderer.sprite;
    }

    public void ChangeSpritePreset(int preset)
    {
        spriteRenderer.sprite = presetSprites[preset];
    }

    public void ResetSprite()
    {
        spriteRenderer.sprite = originalSprite;
    }

    public void ChangeColorPreset(int color)
    {
        spriteRenderer.color = presetColors[color];
    }

    public void ResetColor()
    {
        spriteRenderer.color = originalColor;
    }

    public void AdjustColorHue(float amount)
    {
        Color.RGBToHSV(spriteRenderer.color, out float hue, out float saturation, out float value);
        spriteRenderer.color = Color.HSVToRGB(hue + amount, saturation, value);
    }

    public void AdjustColorSaturation(float amount)
    {
        Color.RGBToHSV(spriteRenderer.color, out float hue, out float saturation, out float value);
        spriteRenderer.color = Color.HSVToRGB(hue, saturation + amount, value);
    }

    public void AdjustColorValue(float amount)
    {
        Color.RGBToHSV(spriteRenderer.color, out float hue, out float saturation, out float value);
        spriteRenderer.color = Color.HSVToRGB(hue, saturation, value + amount);
    }

    public void SetAlpha(float amount)
    {
        Color color = spriteRenderer.color;
        color.a = amount;
        spriteRenderer.color = color;
    }

    public void AdjustAlpha(float amount)
    {
        Color color = spriteRenderer.color;
        color.a += amount;
        spriteRenderer.color = color;
    }

    public void RandomColor()
    {
        spriteRenderer.color = new Color(Random.Range(0F, 1F), Random.Range(0, 1F), Random.Range(0, 1F));
    }






    // ------- Other Object Methods ------

    private SpriteRenderer GetSpriteRenderer(GameObject aObject)
    {
        SpriteRenderer renderer = aObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = aObject.GetComponentInChildren<SpriteRenderer>();
        }
        return renderer;
    }

    public void OtherChangeSpritePreset(GameObject aObject)
    {
        GetSpriteRenderer(aObject).sprite = presetSprites[0];
    }

    public void OtherChangeColorPreset(GameObject aObject)
    {
        GetSpriteRenderer(aObject).color = presetColors[0];
    }

    public void OtherAdjustColorHue(GameObject aObject)
    {
        Color.RGBToHSV(GetSpriteRenderer(aObject).color, out float hue, out float saturation, out float value);
        GetSpriteRenderer(aObject).color = Color.HSVToRGB(hue + changeAmount, saturation, value);
    }

    public void OtherAdjustColorSaturation(GameObject aObject)
    {
        Color.RGBToHSV(GetSpriteRenderer(aObject).color, out float hue, out float saturation, out float value);
        GetSpriteRenderer(aObject).color = Color.HSVToRGB(hue, saturation + changeAmount, value);
    }

    public void OtherAdjustColorValue(GameObject aObject)
    {
        Color.RGBToHSV(GetSpriteRenderer(aObject).color, out float hue, out float saturation, out float value);
        GetSpriteRenderer(aObject).color = Color.HSVToRGB(hue, saturation, value + changeAmount);
    }

    public void OtherSetAlpha(GameObject aObject)
    {
        Color color = GetSpriteRenderer(aObject).color;
        color.a = changeAmount;
        GetSpriteRenderer(aObject).color = color;
    }

    public void OtherAdjustAlpha(GameObject aObject)
    {
        Color color = GetSpriteRenderer(aObject).color;
        color.a += changeAmount;
        GetSpriteRenderer(aObject).color = color;
    }

    public void OtherRandomColor(GameObject aObject)
    {
        GetSpriteRenderer(aObject).color = new Color(Random.Range(0F, 1F), Random.Range(0, 1F), Random.Range(0, 1F));
    }
}
