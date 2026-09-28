using UnityEngine;

public sealed class GroundPlatformEffect : MonoBehaviour
{
    public enum EffectType
    {
        Jump,
        Slow,
        Fast
    }

    [SerializeField] private EffectType effectType;
    [Min(0f)]
    [SerializeField] private float strength = 1f;

    public EffectType Type => effectType;
    public float Strength => strength;

    public void ApplyTo(PlayerMovement player)
    {
        if (player == null) {
            return;
        }

        switch (effectType) {
            case EffectType.Jump:
                player.LaunchUpward(strength);
                break;
            case EffectType.Slow:
            case EffectType.Fast:
                player.ApplyGroundSpeedMultiplier(strength);
                break;
        }
    }

    private void OnValidate()
    {
        strength = Mathf.Max(0f, strength);
    }
}
