using Microsoft.Xna.Framework.Audio;

namespace ArenaStorm.Client.Audio;

/// <summary>
/// Procedural audio synthesizer generating dynamic retro sound effects with zero external asset files.
/// </summary>
public static class SoundManager
{
    private const int SampleRate = 22050;

    private static SoundEffect? _shootSfx;
    private static SoundEffect? _meleeSfx;
    private static SoundEffect? _hitSfx;
    private static SoundEffect? _killSfx;
    private static SoundEffect? _pickupSfx;
    private static SoundEffect? _deathSfx;

    private static bool _initialized;

    /// <summary>
    /// Generates audio buffers and compiles sound effects.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        try
        {
            _shootSfx = GenerateLaserSound();
            _meleeSfx = GenerateMeleeSlashSound();
            _hitSfx = GenerateImpactSound();
            _killSfx = GenerateChimeSound();
            _pickupSfx = GeneratePickupSound();
            _deathSfx = GenerateDeathSound();
            _initialized = true;
        }
        catch
        {
            // Audio hardware may not be available on headless servers/test environments
        }
    }

    /// <summary>Plays ranged projectile firing sound.</summary>
    public static void PlayShoot() => SafePlay(_shootSfx, 0.45f);

    /// <summary>Plays melee swing whoosh sound.</summary>
    public static void PlayMelee() => SafePlay(_meleeSfx, 0.5f);

    /// <summary>Plays entity damage impact sound.</summary>
    public static void PlayHit() => SafePlay(_hitSfx, 0.55f);

    /// <summary>Plays enemy elimination victory chime.</summary>
    public static void PlayKill() => SafePlay(_killSfx, 0.6f);

    /// <summary>Plays collectible pickup power chime.</summary>
    public static void PlayPickup() => SafePlay(_pickupSfx, 0.65f);

    /// <summary>Plays player defeat rumble sound.</summary>
    public static void PlayDeath() => SafePlay(_deathSfx, 0.7f);

    private static void SafePlay(SoundEffect? sfx, float volume)
    {
        if (!_initialized) Initialize();
        try
        {
            sfx?.Play(volume, 0f, 0f);
        }
        catch
        {
            // Silently ignore if audio device is unavailable
        }
    }

    private static SoundEffect GenerateLaserSound()
    {
        int samples = (int)(SampleRate * 0.12f);
        byte[] buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = MathHelper(880f, 220f, t);
            float sample = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            sample *= 1f - t; // Linear volume fadeout

            short val = (short)(sample * short.MaxValue * 0.4f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect GenerateMeleeSlashSound()
    {
        int samples = (int)(SampleRate * 0.15f);
        byte[] buffer = new byte[samples * 2];
        var rand = new Random(42);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float noise = ((float)rand.NextDouble() * 2f) - 1f;
            float freq = MathHelper(400f, 150f, t);
            float tone = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            float combined = (noise * 0.6f) + (tone * 0.4f);
            combined *= MathF.Sin(t * MathF.PI); // Arc envelope

            short val = (short)(combined * short.MaxValue * 0.5f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect GenerateImpactSound()
    {
        int samples = (int)(SampleRate * 0.08f);
        byte[] buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = 140f;
            float sample = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            sample *= MathF.Exp(-t * 8f); // Exponential punch decay

            short val = (short)(sample * short.MaxValue * 0.7f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect GenerateChimeSound()
    {
        int samples = (int)(SampleRate * 0.35f);
        byte[] buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = t switch
            {
                < 0.33f => 523.25f, // C5
                < 0.66f => 659.25f, // E5
                _ => 783.99f        // G5
            };

            float sample = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            sample *= 1f - (t * 0.8f);

            short val = (short)(sample * short.MaxValue * 0.4f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect GeneratePickupSound()
    {
        int samples = (int)(SampleRate * 0.22f);
        byte[] buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = t < 0.5f ? 587.33f : 880.00f; // D5 -> A5
            float sample = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            sample *= 1f - t;

            short val = (short)(sample * short.MaxValue * 0.45f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect GenerateDeathSound()
    {
        int samples = (int)(SampleRate * 0.45f);
        byte[] buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = MathHelper(220f, 45f, t);
            float sample = MathF.Sin(2f * MathF.PI * freq * (i / (float)SampleRate));
            sample *= 1f - t;

            short val = (short)(sample * short.MaxValue * 0.6f);
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[(i * 2) + 1] = (byte)((val >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static float MathHelper(float start, float end, float amount)
    {
        return start + ((end - start) * amount);
    }
}
