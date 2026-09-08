using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
        public bool loop;

        [HideInInspector] public AudioSource source;
    }

    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool soundEnabled = true;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Effects")]
    [SerializeField] private Sound[] sounds;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private int currentTrackIndex = -1;

    private readonly Dictionary<string, Sound> soundDict = new Dictionary<string, Sound>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.playOnAwake = false;
            soundDict[sound.name] = sound;
        }
    }

    private void Start()
    {
        PlayMusic();
    }

    public void PlaySound(string soundName)
    {
        if (!soundEnabled) return;

        if (soundDict.TryGetValue(soundName, out Sound sound))
        {
            sound.source.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Sound '{soundName}' not found!");
        }
    }

    public void PlayMusic()
    {
        if (!musicEnabled || musicTracks.Length == 0 || musicSource == null) return;

        currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Length;
        musicSource.clip = musicTracks[currentTrackIndex];
        musicSource.Play();
    }

    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        if (musicSource != null) musicSource.mute = !musicEnabled;
        PersistSettings();
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        PersistSettings();
    }

    public bool IsSoundEnabled() => soundEnabled;
    public bool IsMusicEnabled() => musicEnabled;

    private void LoadSettings()
    {
        SaveSystem.SaveData data = SaveSystem.LoadGameData();
        musicEnabled = data.musicEnabled;
        soundEnabled = data.soundEnabled;

        if (musicSource != null)
            musicSource.mute = !musicEnabled;
    }

    private void PersistSettings()
    {
        SaveSystem.SaveData data = SaveSystem.LoadGameData();
        SaveSystem.SetAudioSettings(soundEnabled, musicEnabled, data.hapticsEnabled);
    }

    public void PlayJumpSound() => PlaySound("jump");
    public void PlayScoreSound() => PlaySound("score");
    public void PlayGameOverSound() => PlaySound("gameover");
    public void PlayNewBestSound() => PlaySound("newbest");
    public void PlayButtonSound() => PlaySound("button");

    private void OnApplicationFocus(bool hasFocus)
    {
        if (musicSource == null) return;

        if (!hasFocus && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
        else if (hasFocus && !musicSource.isPlaying && musicEnabled)
        {
            musicSource.UnPause();
        }
    }
}
