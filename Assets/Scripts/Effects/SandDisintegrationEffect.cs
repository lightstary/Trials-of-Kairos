using UnityEngine;

/// <summary>
/// Spawns a slow, dramatic sand disintegration effect — particles gently
/// peel away from the player's body over time, drifting upward and sideways
/// like the character is crumbling into sand grains.
/// Uses unscaled time so particles keep flowing through Time.timeScale = 0
/// (allowing them to animate into the death/lose screen).
/// Self-destructs after the effect finishes.
/// </summary>
public class SandDisintegrationEffect : MonoBehaviour
{
    /// <summary>Total emission window — particles stream out over this duration.</summary>
    private const float EMIT_DURATION = 1.8f;

    /// <summary>How long each particle lives after being emitted.</summary>
    private const float PARTICLE_LIFETIME = 2.5f;

    /// <summary>Total particles emitted across the full duration.</summary>
    private const int TOTAL_PARTICLES = 600;

    /// <summary>Spawns the disintegration effect at the given world position and bounds.</summary>
    public static SandDisintegrationEffect Spawn(Vector3 position, Vector3 size)
    {
        GameObject go = new GameObject("SandDisintegration");
        go.transform.position = position;
        SandDisintegrationEffect effect = go.AddComponent<SandDisintegrationEffect>();
        effect.Initialize(size);
        return effect;
    }

    /// <summary>Destroys all active disintegration effects in the scene.</summary>
    public static void DestroyAll()
    {
        SandDisintegrationEffect[] effects = FindObjectsOfType<SandDisintegrationEffect>();
        foreach (SandDisintegrationEffect effect in effects)
        {
            if (effect != null)
                Destroy(effect.gameObject);
        }
    }

    private void Initialize(Vector3 size)
    {
        // Create ParticleSystem and stop default playback before configuring
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main module ─────────────────────────────────────────────────
        var main = ps.main;
        main.duration = EMIT_DURATION;
        main.loop = false;
        main.playOnAwake = false;
        main.useUnscaledTime = true; // Keep animating through timeScale = 0
        main.startLifetime = new ParticleSystem.MinMaxCurve(PARTICLE_LIFETIME * 0.7f, PARTICLE_LIFETIME);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.055f);
        main.gravityModifier = -0.15f; // Slight upward float
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = TOTAL_PARTICLES + 50;

        // Sand colors: warm amber to pale gold
        Color sandDark  = new Color(0.76f, 0.60f, 0.32f, 1f);
        Color sandLight = new Color(0.95f, 0.85f, 0.55f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(sandDark, sandLight);

        // ── Emission: ramp up over time (slow start -> crescendo) ───────
        var emission = ps.emission;
        emission.enabled = true;

        AnimationCurve emitCurve = new AnimationCurve();
        emitCurve.AddKey(0f, 0.05f);
        emitCurve.AddKey(0.2f, 0.15f);
        emitCurve.AddKey(0.5f, 0.5f);
        emitCurve.AddKey(0.8f, 1.0f);
        emitCurve.AddKey(1.0f, 0.3f);

        float peakRate = TOTAL_PARTICLES / EMIT_DURATION;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(peakRate, emitCurve);

        // ── Shape: emit from the player's full volume ───────────────────
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;

        // ── Velocity: gentle drift to one side + upward ─────────────────
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;

        AnimationCurve driftCurve = new AnimationCurve();
        driftCurve.AddKey(0f, 0f);
        driftCurve.AddKey(0.3f, 0.4f);
        driftCurve.AddKey(1f, 1f);
        vel.x = new ParticleSystem.MinMaxCurve(1.2f, driftCurve);

        AnimationCurve riseCurve = new AnimationCurve();
        riseCurve.AddKey(0f, 0.2f);
        riseCurve.AddKey(0.5f, 0.6f);
        riseCurve.AddKey(1f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(0.8f, riseCurve);

        AnimationCurve swayCurve = new AnimationCurve();
        swayCurve.AddKey(0f, -0.3f);
        swayCurve.AddKey(0.5f, 0.3f);
        swayCurve.AddKey(1f, -0.1f);
        vel.z = new ParticleSystem.MinMaxCurve(0.5f, swayCurve);

        // ── Size over lifetime: hold then slowly shrink ─────────────────
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve shrinkCurve = new AnimationCurve();
        shrinkCurve.AddKey(0f, 0.8f);
        shrinkCurve.AddKey(0.4f, 1.0f);
        shrinkCurve.AddKey(0.8f, 0.5f);
        shrinkCurve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        // ── Color over lifetime: hold opaque then fade gently ───────────
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = new ParticleSystem.MinMaxGradient(fadeGradient);

        // ── Noise: slow organic turbulence for that drifting feel ────────
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 1.5f;
        noise.scrollSpeed = 0.8f;
        noise.damping = true;
        noise.octaveCount = 3;

        // ── Renderer ────────────────────────────────────────────────────
        ParticleSystemRenderer psr = GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", sandLight);
        psr.material = mat;

        ps.Play();

        Destroy(gameObject, EMIT_DURATION + PARTICLE_LIFETIME + 1f);
    }
}
