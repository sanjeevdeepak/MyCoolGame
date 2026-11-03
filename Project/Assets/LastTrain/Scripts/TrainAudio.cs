using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>Music, train ambience and a fixed number of effect voices.</summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class TrainAudio : MonoBehaviour
{
    public TrainLevelDirector director;
    public AudioSource playerSource;
    public AudioMixerGroup effectsGroup;
    public Button[] menuButtons;
    [Header("Volume")]
    [Range(0f,1f)] public float masterVolume=.8f;
    [Range(0f,1f)] public float musicVolume=.2f;
    [Range(0f,2f)] public float gameplayMusicGain=1.18f;
    [Range(0f,1f)] public float ambienceVolume=.45f;
    [Range(0f,1f)] public float playerVolume=.6f;
    [Range(0f,1f)] public float machineryVolume=.5f;
    [Range(0f,1f)] public float warningVolume=.85f;
    [Range(0f,1f)] public float menuVolume=.4f;
    [Header("Sounds")]
    public AudioClip music, endingMusic;
    [Min(0f)] public float musicStartTime=0f;
    [Min(0f)] public float musicLoopEndTime;
    public AudioClip rumble, track, step, land, hardLand;
    public AudioClip switchClick, bridge, hatch, brake, warning, signal;
    public AudioClip checkpoint, death, respawn, finish, menu, replay;
    [Header("Movement")]
    [Min(.1f)] public float stepDistance=4f;
    [Min(0f)] public float hardLandingSpeed=16f;

    private AudioSource musicSource, nextMusicSource, rumbleSource, trackSource, footSource, warningSource, cueSource, menuSource;
    private double musicEnd;
    private int pausedMusicSample;
    private bool musicStarted, musicQueued, resumeMusic, started;
    private bool completionCuePlayed, replayCuePlayed;
    private readonly AudioSource[] machines=new AudioSource[4];
    private readonly float[] machineGains=new float[4];
    private readonly ContactPoint2D[] contacts=new ContactPoint2D[24];
    private bool[] active, spent;
    private float[] warningAt;
    private Rigidbody2D body;
    private TrainRiderFrame rider;
    private TrainEffects effects;
    private bool paused, dead, completed, grounded=true;
    private float stride, airborne, fallSpeed, nextFoot, nextWarning, ambienceFade, musicFade;
    private int voice, foot;
    private float footGain, warningGain, cueGain;
    public float DeathDuration => death!=null ? death.length : 0f;

    private void Awake()
    {
        body=director.player.GetComponent<Rigidbody2D>();rider=director.player.GetComponent<TrainRiderFrame>();
        effects=GetComponent<TrainEffects>();
        if(playerSource!=null)playerSource.volume=masterVolume*playerVolume;
        paused=Time.timeScale<=0f;
        musicSource=Source("Music");nextMusicSource=Source("Music Loop");
        foreach(var source in new[]{musicSource,nextMusicSource}){source.outputAudioMixerGroup=null;source.priority=220;}
        rumbleSource=Source("Rumble",true);trackSource=Source("Track",true);
        rumbleSource.clip=rumble;trackSource.clip=track;
        footSource=Source("Footsteps");warningSource=Source("Warnings");cueSource=Source("Cues");menuSource=Source("Menu");
        menuSource.outputAudioMixerGroup=null;menuSource.ignoreListenerPause=true;
        for(int i=0;i<machines.Length;i++)machines[i]=Source("Machinery "+(i+1));
        int count=director.equipment.Length;
        active=new bool[count];spent=new bool[count];warningAt=new float[count];
    }

    private void OnEnable()
    {
        if(menuButtons!=null)foreach(Button button in menuButtons)if(button!=null)button.onClick.AddListener(PlayMenu);
        if(started && rumbleSource!=null && !dead && !completed)StartLoops();
    }

    private AudioSource Source(string name,bool loop=false)
    {
        GameObject obj=new GameObject(name);obj.transform.SetParent(transform,false);
        AudioSource source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
        source.outputAudioMixerGroup=effectsGroup;source.spatialBlend=0;source.dopplerLevel=0;source.volume=0;
        source.priority=loop?200:128;return source;
    }

    private void Start(){started=true;if(!director.IsIntroducing)BeginGameplay();}

    public void BeginGameplay(){if(musicStarted)return;StartLoops();PlayCue(respawn,.65f);}

    private void LateUpdate()
    {
        if(director.IsIntroducing){ApplyVolumes();return;}
        bool pause=Time.timeScale<=0f;
        if(paused!=pause){SetPaused(pause);PlayMenu();}
        if(paused){ApplyVolumes();return;}
        UpdateMusic();
        bool warningNow=false;
        if(!dead && !completed && !director.IsResetting)
        {
            warningNow=Equipment();
            Movement();
        }
        float target=dead || completed?0f:warningNow?.38f:1f;
        ambienceFade=Mathf.MoveTowards(ambienceFade,target,Time.unscaledDeltaTime*5f);
        bool effectPlaying=(playerSource!=null && playerSource.isPlaying) || footSource.isPlaying || cueSource.isPlaying;
        foreach(AudioSource source in machines)effectPlaying|=source.isPlaying;
        float musicTarget=dead?0f:completed?1f:warningNow?.3f:effectPlaying?.6f:1f;
        musicFade=Mathf.MoveTowards(musicFade,musicTarget,Time.unscaledDeltaTime*(musicTarget<musicFade?10f:2f));
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        musicSource.volume=nextMusicSource.volume=MusicVolume*musicFade;
        rumbleSource.volume=masterVolume*ambienceVolume*.55f*ambienceFade;
        trackSource.volume=masterVolume*ambienceVolume*.22f*ambienceFade;
        if(playerSource!=null)playerSource.volume=masterVolume*playerVolume;
        footSource.volume=masterVolume*playerVolume*footGain;
        warningSource.volume=masterVolume*warningVolume*warningGain;
        cueSource.volume=masterVolume*playerVolume*cueGain;
        menuSource.volume=masterVolume*menuVolume;
        for(int i=0;i<machines.Length;i++)machines[i].volume=masterVolume*machineryVolume*machineGains[i];
    }

    private bool Equipment()
    {
        bool warningNow=false;
        for(int i=0;i<director.equipment.Length;i++)
        {
            TrainEquipment control=director.equipment[i];
            Vector2 position=Position(control);
            float gain=DistanceGain(position);
            if(control.IsActive && !active[i])
            {
                switch(control.kind)
                {
                    case TrainEquipment.Kind.Bridge:Machine(bridge,.7f,position,1f);break;
                    case TrainEquipment.Kind.Signal:Machine(signal,.65f,position,1f);break;
                    case TrainEquipment.Kind.Hatch:Machine(hatch,.8f,position,.94f);break;
                    case TrainEquipment.Kind.Brake:Machine(brake,.8f,position,.85f);break;
                }
            }
            if(control.IsSpent && !spent[i])
            {
                if(control.kind==TrainEquipment.Kind.Bridge)Machine(hatch,.7f,position,.85f);
                else if(control.kind==TrainEquipment.Kind.Signal || control.kind==TrainEquipment.Kind.Brake)Machine(signal,.4f,position,1.08f);
            }
            bool warn=control.IsWarning || (control.kind==TrainEquipment.Kind.Bridge && control.IsActive && control.Remaining<=control.retractWarning);
            if(warn && gain>.03f)
            {
                warningNow=true;
                if(Time.time>=warningAt[i] && Time.time>=nextWarning)
                {
                    warningAt[i]=Time.time+.22f;nextWarning=Time.time+.12f;
                    warningSource.clip=warning;warningSource.pitch=control.kind==TrainEquipment.Kind.Bridge?1.1f:1f;
                    warningGain=gain;warningSource.volume=masterVolume*warningVolume*gain;warningSource.panStereo=Pan(position);warningSource.Play();
                }
            }
            active[i]=control.IsActive;spent[i]=control.IsSpent;
        }
        return warningNow;
    }

    private static Vector2 Position(TrainEquipment control)
    {
        if(control.kind==TrainEquipment.Kind.Bridge && control.bridgeStart!=null && control.bridgeEnd!=null)return (control.bridgeStart.position+control.bridgeEnd.position)*.5f;
        if(control.hatchVisual!=null)return control.hatchVisual.transform.position;
        return control.transform.position;
    }

    private void Movement()
    {
        bool supported=false;
        ContactPoint2D ground=default;
        if(body.bodyType==RigidbodyType2D.Dynamic && body.linearVelocity.y<=.2f)
        {
            int count=body.GetContacts(contacts);
            for(int i=0;i<count;i++)if(contacts[i].normal.y>=.7f){supported=true;ground=contacts[i];break;}
        }
        if(!supported)
        {
            airborne+=Time.deltaTime;fallSpeed=Mathf.Max(fallSpeed,-body.linearVelocity.y);stride=0;
        }
        else
        {
            if(!grounded && airborne>=.075f && fallSpeed>1.5f && Time.time>=nextFoot)
            {
                footSource.Stop();footSource.clip=fallSpeed>=hardLandingSpeed?hardLand:land;
                footSource.pitch=fallSpeed>=hardLandingSpeed?.92f:1f;
                footGain=Mathf.Lerp(.32f,.75f,Mathf.InverseLerp(2f,25f,fallSpeed));footSource.volume=masterVolume*playerVolume*footGain;footSource.Play();nextFoot=Time.time+.14f;
                TrainMotion surface=ground.collider.GetComponentInParent<TrainMotion>();
                if(surface==null)surface=ground.otherCollider.GetComponentInParent<TrainMotion>();
                if(effects!=null)effects.Land(new Vector2(body.position.x,ground.point.y),fallSpeed,surface);
            }
            airborne=fallSpeed=0;
            float speed=Mathf.Abs(body.linearVelocity.x-(rider.Support!=null?rider.Support.CurrentSpeed:0f));
            if(speed>2f)
            {
                stride+=speed*Time.deltaTime;
                if(stride>=stepDistance && Time.time>=nextFoot)
                {
                    stride%=stepDistance;footSource.clip=step;footSource.pitch=(foot++&1)==0?.97f:1.03f;
                    footGain=.16f;footSource.volume=masterVolume*playerVolume*footGain;footSource.Play();nextFoot=Time.time+.12f;
                }
            }
            else stride=0;
        }
        grounded=supported;
    }

    private float DistanceGain(Vector2 position)=>Mathf.InverseLerp(40f,8f,Mathf.Abs(position.x-body.position.x));
    private float Pan(Vector2 position)=>Mathf.Clamp((position.x-body.position.x)/25f,-1f,1f)*.3f;

    private void Machine(AudioClip clip,float gain,Vector2 position,float pitch)
    {
        gain*=DistanceGain(position);if(clip==null || gain<=.02f)return;
        int index=voice++%machines.Length;
        for(int i=0;i<machines.Length;i++)if(!machines[i].isPlaying){index=i;break;}
        AudioSource source=machines[index];source.Stop();source.clip=clip;source.pitch=pitch;source.panStereo=Pan(position);
        machineGains[index]=gain;source.volume=masterVolume*machineryVolume*gain;source.Play();
    }

    private void StartLoops()
    {
        if(!musicStarted)StartMusic(music,false);
        if(!rumbleSource.isPlaying){rumbleSource.time=0;rumbleSource.Play();}
        if(!trackSource.isPlaying){trackSource.time=0;trackSource.Play();}
        if(paused){rumbleSource.Pause();trackSource.Pause();}
    }

    private int MusicStartSample => music==null?0:Mathf.Clamp(Mathf.RoundToInt(musicStartTime*music.frequency),0,Mathf.Max(0,music.samples-1));
    private int MusicEndSample => music==null?0:musicLoopEndTime>0f?Mathf.Clamp(Mathf.RoundToInt(musicLoopEndTime*music.frequency),MusicStartSample+1,music.samples):music.samples;
    private double MusicDuration => music==null?0:(double)(MusicEndSample-MusicStartSample)/music.frequency;
    private float MusicVolume => Mathf.Clamp01(masterVolume*musicVolume*gameplayMusicGain);

    private void StartMusic(AudioClip clip,bool ending)
    {
        musicSource.Stop();nextMusicSource.Stop();musicEnd=0;musicQueued=false;resumeMusic=false;
        musicSource.clip=clip;musicStarted=true;
        if(clip==null)return;
        musicFade=1f;musicSource.volume=nextMusicSource.volume=MusicVolume;
        pausedMusicSample=ending?0:MusicStartSample;
        if(paused){resumeMusic=true;return;}
        PlayMusic(pausedMusicSample);
    }

    private void PlayMusic(int sample)
    {
        musicSource.timeSamples=sample;
        if(completed){musicSource.Play();return;}
        // Explicit ends join the next loop at its start sample without a gap
        // or overlapping copies of the music.
        double now=AudioSettings.dspTime;
        musicEnd=now+(double)(MusicEndSample-sample)/music.frequency;
        musicSource.PlayScheduled(now);musicSource.SetScheduledEndTime(musicEnd);
        QueueMusic();
    }

    private void QueueMusic()
    {
        nextMusicSource.Stop();nextMusicSource.clip=music;nextMusicSource.timeSamples=MusicStartSample;
        nextMusicSource.PlayScheduled(musicEnd);nextMusicSource.SetScheduledEndTime(musicEnd+MusicDuration);musicQueued=true;
    }

    private void UpdateMusic()
    {
        if(dead || completed || !musicQueued || AudioSettings.dspTime<musicEnd)return;
        var previous=musicSource;musicSource=nextMusicSource;nextMusicSource=previous;
        musicEnd+=MusicDuration;
        if(AudioSettings.dspTime>=musicEnd){StartMusic(music,false);return;}
        QueueMusic();
    }

    private void PlayCue(AudioClip clip,float gain=1f)
    {
        cueGain=gain;cueSource.Stop();cueSource.clip=clip;cueSource.pitch=1;cueSource.volume=masterVolume*playerVolume*gain;
        if(clip!=null){cueSource.Play();if(paused)cueSource.Pause();}
    }

    public void Checkpoint(){if(!completed)PlayCue(checkpoint,.85f);}

    public void PlaySwitch(Vector2 position)
    {
        if(!dead && !completed && !director.IsResetting && Time.timeScale>0f)Machine(switchClick,1f,position,1f);
    }

    public void Death()
    {
        if(completed)return;
        StopGameplay();dead=true;completed=false;ResetRecords();PlayCue(death);
    }

    public void Respawn()
    {
        if(completed)return;
        bool wasDead=dead;
        StopGameplay();dead=completed=false;ResetRecords();StartLoops();
        PlayCue(respawn,.65f);
        // Instant retries keep the death cue audible on the existing player voice.
        if(wasDead && playerSource!=null && death!=null)playerSource.PlayOneShot(death,.75f);
    }

    public void Complete()
    {
        if(completed)return;
        StopGameplay();cueSource.Stop();completed=true;completionCuePlayed=false;StartMusic(endingMusic,true);
    }

    public void EndingStep()
    {
        if(!completed || completionCuePlayed || Time.timeScale<=0f)return;
        footSource.Stop();footSource.clip=step;footSource.pitch=(foot++&1)==0?.98f:1.02f;
        footGain=.13f;footSource.volume=masterVolume*playerVolume*footGain;footSource.Play();
    }
    public void StopEndingSteps(){if(footSource!=null)footSource.Stop();footGain=0;}
    public void ShowCompletion()
    {
        if(!completed || completionCuePlayed)return;
        completionCuePlayed=true;StopEndingSteps();PlayCue(finish);
    }

    public void StopForReplay()
    {
        if(replayCuePlayed)return;
        StopGameplay();cueSource.Stop();menuSource.Stop();
        if(completed && replay!=null)
        {
            replayCuePlayed=true;menuSource.clip=replay;menuSource.pitch=1;
            menuSource.volume=masterVolume*menuVolume;menuSource.Play();
        }
    }

    public void PlayMenu()
    {
        if(menu==null || replayCuePlayed)return;menuSource.Stop();menuSource.clip=menu;menuSource.volume=masterVolume*menuVolume;menuSource.Play();
    }

    private void ResetRecords()
    {
        System.Array.Clear(active,0,active.Length);System.Array.Clear(spent,0,spent.Length);System.Array.Clear(warningAt,0,warningAt.Length);
        stride=airborne=fallSpeed=nextWarning=ambienceFade=musicFade=0;nextFoot=Time.time+.15f;grounded=true;
        musicSource.volume=rumbleSource.volume=trackSource.volume=0;
    }

    private void SetPaused(bool pause)
    {
        if(pause)
        {
            UpdateMusic();resumeMusic=musicSource.isPlaying;
            pausedMusicSample=musicSource.timeSamples;musicSource.Stop();nextMusicSource.Stop();musicQueued=false;musicEnd=0;
        }
        paused=pause;
        if(!pause && resumeMusic){resumeMusic=false;PlayMusic(pausedMusicSample);}
        foreach(AudioSource source in new[]{rumbleSource,trackSource,footSource,warningSource,cueSource,playerSource})
            if(source!=null){if(pause)source.Pause();else source.UnPause();}
        foreach(AudioSource source in machines){if(pause)source.Pause();else source.UnPause();}
        stride=0;
    }

    private void StopGameplay()
    {
        if(effects!=null)effects.Clear();
        musicSource.Stop();nextMusicSource.Stop();musicEnd=0;musicQueued=false;musicStarted=false;resumeMusic=false;
        rumbleSource.Stop();trackSource.Stop();footSource.Stop();warningSource.Stop();
        foreach(AudioSource source in machines)source.Stop();
        if(playerSource!=null)playerSource.Stop();ambienceFade=musicFade=0;
    }

    private void OnDisable()
    {
        if(rumbleSource!=null){StopGameplay();cueSource.Stop();menuSource.Stop();}
        if(menuButtons!=null)foreach(Button button in menuButtons)if(button!=null)button.onClick.RemoveListener(PlayMenu);
    }
}
