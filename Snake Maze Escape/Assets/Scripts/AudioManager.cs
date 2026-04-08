using UnityEngine;

// AudioManager synthesises all sounds procedurally — no audio files required.
// Clips are generated once in Awake() using Unity's AudioClip PCM API and
// reused for the lifetime of the scene.
// Auto-created by GameManager if not present in the scene.

public class AudioManager : MonoBehaviour
{
    private AudioSource source;

    private AudioClip moveClip;
    private AudioClip escapeClip;
    private AudioClip winClip;

    private const int SampleRate = 44100;

    void Awake()
    {
        source             = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        // Short soft blip — confirms each cell of movement
        moveClip = GenerateTone(660f, 0.07f, 0.30f);

        // Rising sweep — satisfying escape confirmation
        escapeClip = GenerateSweep(420f, 1050f, 0.30f, 0.55f);

        // Arpeggiated C-major chord — level complete fanfare
        winClip = GenerateChord(0.75f);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void PlayMove()   => source.PlayOneShot(moveClip,   0.45f);
    public void PlayEscape() => source.PlayOneShot(escapeClip, 0.70f);
    public void PlayWin()    => source.PlayOneShot(winClip,    0.80f);

    // -------------------------------------------------------------------------
    // Synthesis helpers
    // -------------------------------------------------------------------------

    // Pure sine tone with exponential decay
    AudioClip GenerateTone(float hz, float duration, float volume)
    {
        int      n    = Mathf.CeilToInt(SampleRate * duration);
        float[] data  = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = volume
                * Mathf.Exp(-t * 14f)                        // fast exponential decay
                * Mathf.Sin(2f * Mathf.PI * hz * t);
        }

        return MakeClip("Move", data);
    }

    // Frequency sweep from startHz to endHz
    AudioClip GenerateSweep(float startHz, float endHz,
        float duration, float volume)
    {
        int      n    = Mathf.CeilToInt(SampleRate * duration);
        float[] data  = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / SampleRate;
            float freq = Mathf.Lerp(startHz, endHz, t / duration);
            float fade = Mathf.Exp(-t * 3.5f);
            data[i]    = volume * fade * Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        return MakeClip("Escape", data);
    }

    // C-major arpeggio: C5, E5, G5 — each note staggered by 60 ms
    AudioClip GenerateChord(float duration)
    {
        int      n    = Mathf.CeilToInt(SampleRate * duration);
        float[] data  = new float[n];

        float[] notes   = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
        float[] offsets = { 0f,      0.06f,   0.12f   }; // stagger

        for (int i = 0; i < n; i++)
        {
            float t      = (float)i / SampleRate;
            float sample = 0f;

            for (int k = 0; k < notes.Length; k++)
            {
                float tLocal = t - offsets[k];
                if (tLocal < 0f) continue;

                float attack = Mathf.Clamp01(tLocal / 0.02f); // 20 ms attack
                float decay  = Mathf.Exp(-tLocal * 3f);
                sample += attack * decay
                    * Mathf.Sin(2f * Mathf.PI * notes[k] * tLocal);
            }

            data[i] = 0.22f * sample;
        }

        return MakeClip("Win", data);
    }

    AudioClip MakeClip(string clipName, float[] data)
    {
        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
