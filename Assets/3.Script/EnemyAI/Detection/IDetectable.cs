using UnityEngine;

public interface IDetectable
{
    Vector3 Position { get; }
    float StealthWeight { get; } 
    bool IsCrouching { get; }
    float SoundIntensity { get; }
}