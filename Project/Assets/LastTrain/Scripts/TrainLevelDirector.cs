using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-400)]
public sealed class TrainLevelDirector : MonoBehaviour
{
    public PlayerController player;
    public TrainMotion[] trains;
    public TrainEquipment[] equipment;
    public TrainAudio sounds;
    public TrainUI ui;
    public SubwayVideoBackground background;
    public TrainIntro opening;
    public TrainOutro closing;
    public Transform checkpointSpawn;
    public SpriteRenderer checkpointLight;
    public EnvironmentSection[] environment;
    public int environmentStartTrain = 14;
    public float environmentLookAhead = 8f;
    public float signalLookAhead = 13.5f;
    public float[] startingGaps = { 6f, 9f, 4.5f, 13f, 5.5f, 6f, 5.3f };
    public float[] minimumGaps = { 3.2f, 8.2f, 4.2f, 5.5f, 4.6f, 4.8f, 5f };
    public float[] maximumGaps = { 6.8f, 10.5f, 8f, 15f, 6.2f, 8.5f, 5.6f };
    public Vector2 firstTrainPosition = new Vector2(0, -7.92f);
    public Transform spawnPoint;
    public Transform cameraAnchor;
    public CameraFollow cameraFollow;
    public float cameraLookAhead = 2.5f;
    public float cameraTrackCenterY;
    public float initialCameraSpeed = 7f;
    public float finalCameraSpeed = 9.5f;
    [Tooltip("Pressure pace for each car. Lower values create short recovery stretches.")]
    public float[] cameraPace = new float[0];
    [Min(.01f)] public float cameraAcceleration = 1.5f;
    [Min(.01f)] public float lookAheadSmoothTime = .4f;
    [Min(0f)] public float cameraHeightLimit = 1.5f;
    public float introductionDistance = 25f;
    public float pressureDistance = 20f;
    public float timedOpeningSeconds = 1.7f;
    public float timedClosingSeconds = 3.3f;
    public float timedOpeningSpeed = 8.2f;
    public float timedClosingSpeed = .8f;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private TrainRiderFrame rider;
    private Camera view;
    private TrainView framing;
    private bool[] introduced;
    private bool[] closingGaps;
    private bool ready, resetting, finished;
    private int lastTrain;
    private float cameraTargetX, cameraStartX, timedElapsed;
    private Vector3 spawnPosition;
    private Vector2[] initialPositions, checkpointPositions;
    private bool checkpointSaved;
    private int checkpointIndex;
    private float originalCameraSmoothTime;
    private float currentCameraSpeed, currentLookAhead, lookAheadVelocity;
    private int lastRespawnFrame=-1;
    private float respawnSafeUntil;
    private TrainPlayerPresentation presentation;
    private float ViewHalfWidth => framing!=null?framing.HalfWidth:view.orthographicSize*view.aspect;
    public bool HasCheckpoint => checkpointSaved;
    public int CheckpointIndex => checkpointSaved ? checkpointIndex : 0;
    public bool IsResetting => resetting;
    public bool IsFinished => finished;
    public bool IsIntroducing => opening!=null && opening.IsPlaying;
    public bool CanInteract => ready && !resetting && !finished && Time.frameCount!=lastRespawnFrame;
    public float CameraSpeed => currentCameraSpeed;
    private float TargetCameraSpeed => Mathf.Lerp(initialCameraSpeed, finalCameraSpeed,
        Mathf.Clamp01((float)lastTrain / Mathf.Max(1,trains.Length-2))) *
        (cameraPace!=null && lastTrain<cameraPace.Length ? Mathf.Max(.25f,cameraPace[lastTrain]) : 1f);
    public float CameraX => view != null ? view.transform.position.x : cameraStartX;
    private float LookAhead => lastTrain == 8 ? signalLookAhead : lastTrain >= environmentStartTrain ?
        (lastTrain >= trains.Length-2 ? environmentLookAhead+2f : environmentLookAhead) : cameraLookAhead;

    private void Awake(){player.respawnHandler=RespawnPlayer;}

    private IEnumerator Start()
    {
        yield return null; // Original player/camera Start methods initialise their singletons first.
        body = player.GetComponent<Rigidbody2D>();
        playerCollider = player.GetComponent<Collider2D>();
        rider = player.GetComponent<TrainRiderFrame>();
        presentation=player.GetComponent<TrainPlayerPresentation>();
        view = Camera.main;
        framing=view.GetComponent<TrainView>();
        originalCameraSmoothTime = cameraFollow.xSmoothTime;
        spawnPosition = spawnPoint.position;
        cameraStartX = spawnPosition.x + cameraLookAhead;
        ApplyStartingLayout();
        initialPositions = new Vector2[trains.Length];
        for(int i=0;i<trains.Length;i++) initialPositions[i]=trains[i].Body.position;
        body.position = spawnPosition;
        player.SetSpawnPoint(spawnPoint);
        ResetCamera();
        if(IsIntroducing)
        {
            resetting=true;
            foreach(var train in trains)train.SetPaused(true);
            foreach(var control in equipment)control.Freeze(true);
            yield return opening.Play();
            resetting=false;
        }
        StartTrains();
        body.simulated=true;
        player.enabled=true;player.EnablePlayerControl(true);
        if(presentation!=null && opening!=null)presentation.ResetPose(false);
        body.linearVelocity = new Vector2(trains[0].cruiseSpeed, 0);
        ready = true;
    }

    public void PrepareOpeningFinish()
    {
        ResetArrangement();
        player.ResetForRespawn(spawnPosition);player.EnablePlayerControl(false);
        body.simulated=false;rider.ResetSupport();
        if(presentation!=null){presentation.ResetPose(false);presentation.Freeze();}
    }

    public static float TrainWidth(TrainMotion train) => train.layoutWidth > 0f ? train.layoutWidth : train.GetComponentInChildren<SpriteRenderer>().bounds.size.x;

    public void ApplyStartingLayout()
    {
        float edge = 0;
        for (int i = 0; i < trains.Length; i++)
        {
            float width = TrainWidth(trains[i]);
            float x = i == 0 ? firstTrainPosition.x : edge + startingGaps[i - 1] + width * .5f;
            Vector2 position = new Vector2(x, firstTrainPosition.y);
            trains[i].Body.position = position;
            trains[i].transform.position = position;
            trains[i].CaptureStart();
            edge = x + width * .5f;
        }
        Physics2D.SyncTransforms();
    }

    private void StartTrains()
    {
        introduced = new bool[trains.Length];
        closingGaps = new bool[trains.Length];
        int first = checkpointSaved ? checkpointIndex : 0;
        for(int i=0;i<=Mathf.Min(first+1,trains.Length-1);i++) introduced[i]=true;
        for (int i = 0; i < trains.Length; i++)
        {
            if (i<first) { trains[i].SetPaused(true);continue; }
            if (i > first+1) trains[i].SetSpeedOverride(trains[i - 1].CurrentSpeed);
            trains[i].SetRunning(true);
            trains[i].SetPaused(false);
        }
        if(equipment!=null) foreach(TrainEquipment control in equipment) control.Freeze(false);
    }

    private void FixedUpdate()
    {
        if (!ready || resetting || finished || body.bodyType != RigidbodyType2D.Dynamic) return;
        int support = CurrentTrainIndex();
        if (support >= 0) lastTrain = Mathf.Max(lastTrain, support);
        for (int i = 0; i < trains.Length; i++)
        {
            trains[i].ClearGapSpeedCap();
            trains[i].ClearGapSpeedFloor();
            if(trains[i].stationary)continue;
            if (i == 0) continue;
            float previousFront = trains[i - 1].Body.position.x + TrainWidth(trains[i - 1]) * .5f;
            if (!introduced[i] && body.position.x > previousFront - introductionDistance) introduced[i] = true;
            if (!introduced[i]) trains[i].SetSpeedOverride(trains[i - 1].CurrentSpeed);
            else trains[i].ClearSpeedOverride();
        }
        // A visibly opening gap becomes safe as the next train slows and closes it.
        if (trains.Length > 4 && introduced[4] && lastTrain < 4)
        {
            timedElapsed += Time.fixedDeltaTime;
            float phase = Mathf.Repeat(timedElapsed, timedOpeningSeconds + timedClosingSeconds);
            trains[4].SetSpeedOverride(phase < timedOpeningSeconds ? timedOpeningSpeed : timedClosingSpeed);
        }
        for (int i = 1; i < trains.Length; i++)
        {
            if (checkpointSaved && i <= checkpointIndex) continue;
            if(trains[i].stationary)continue;
            if (i <= lastTrain) continue;
            TrainMotion previous = trains[i - 1], next = trains[i];
            float gap = Gap(i - 1), difference = next.CurrentSpeed - previous.CurrentSpeed;
            float brake = difference * difference / (2f * Mathf.Max(1f, next.speedChangeRate)) + .15f;
            if (gap <= minimumGaps[i - 1] + (difference < 0 ? brake : .1f))
                next.SetGapSpeedFloor(previous.CurrentSpeed + Mathf.Max(0, minimumGaps[i - 1] - gap) * 2f);
            float range = maximumGaps[i - 1] - minimumGaps[i - 1];
            if (gap >= maximumGaps[i - 1] - (difference > 0 ? brake : .1f)) closingGaps[i] = true;
            if (difference <= 0f && gap <= minimumGaps[i - 1] + range * .45f) closingGaps[i] = false;
            // Close an oversized gap visibly, then allow the next coasting
            // cycle. Holding both cars at the same speed erased their rhythm.
            if (closingGaps[i])
                next.SetGapSpeedCap(Mathf.Max(0, previous.CurrentSpeed - Mathf.Clamp(range*.65f,.6f,2f)));
        }
        int pressureTarget = -1;
        if (support >= 0 && support < trains.Length - 1 && introduced[support + 1])
        {
            float front = trains[support].Body.position.x + TrainWidth(trains[support]) * .5f;
            bool hesitating = body.linearVelocity.x - trains[support].CurrentSpeed < 3f;
            if (support != 3 && front - body.position.x < pressureDistance && hesitating) pressureTarget = support + 1;
        }
        for (int i = 0; i < trains.Length; i++) trains[i].SetPressure(!trains[i].stationary && i == pressureTarget);
        if(equipment!=null)
        {
            foreach(TrainEquipment control in equipment)
            {
                if(!control.IsActive)continue;
                TrainMotion affected = control.kind==TrainEquipment.Kind.Signal ? control.targetTrain : control.sourceTrain;
                if(control.kind!=TrainEquipment.Kind.Signal && control.kind!=TrainEquipment.Kind.Brake)continue;
                affected.ClearGapSpeedFloor();
                affected.ClearGapSpeedCap();
                affected.SetPressure(false);
                affected.SetSpeedOverride(control.speed);
                affected.SetGapSpeedCap(control.speed);
                if(control.kind==TrainEquipment.Kind.Brake)
                {
                    for(int i=checkpointSaved?checkpointIndex:0;i<trains.Length && trains[i]!=affected;i++)
                        trains[i].SetGapSpeedCap(control.speed);
                    var escaping=control.targetTrain;
                    if(escaping!=null && control.escapeSpeed>0f && System.Array.IndexOf(trains,escaping)>lastTrain)
                    {
                        escaping.SetPressure(false);
                        escaping.SetSpeedOverride(control.escapeSpeed);
                        // Keep the gap's braking cap so the burst cannot carry
                        // the next landing beyond its authored catchable range.
                        escaping.LimitSpeed(control.escapeSpeed);
                        int gapIndex=System.Array.IndexOf(trains,affected);
                        if(gapIndex>=0 && gapIndex<maximumGaps.Length)
                        {
                            // Anticipate the source reaching its braking speed;
                            // its current speed understates the opening gap.
                            float relative=Mathf.Max(0f,escaping.CurrentSpeed-control.speed);
                            float stopping=relative*relative/(2f*Mathf.Max(1f,escaping.speedChangeRate))+.4f;
                            if(Gap(gapIndex)>=maximumGaps[gapIndex]-stopping)
                                closingGaps[gapIndex+1]=true;
                            if(closingGaps[gapIndex+1])escaping.LimitSpeed(control.speed);
                        }
                    }
                }
            }
        }
        if(lastTrain>0)
        {
            float left = CameraX-ViewHalfWidth;
            for(int i=0;i<lastTrain;i++)
                if(trains[i].Body.position.x+TrainWidth(trains[i])*.5f < left-2f) trains[i].SetPaused(true);
        }
    }

    private void Update()
    {
        if (!ready || finished || Time.timeScale<=0f) return;
        if (!resetting && body.bodyType == RigidbodyType2D.Kinematic){RespawnPlayer();return;}
        if(resetting)return;
        currentCameraSpeed = Mathf.MoveTowards(currentCameraSpeed, TargetCameraSpeed, cameraAcceleration * Time.deltaTime);
        currentLookAhead = Mathf.SmoothDamp(currentLookAhead, LookAhead, ref lookAheadVelocity, lookAheadSmoothTime);
        cameraTargetX = Mathf.Max(cameraTargetX + CameraSpeed * Time.deltaTime, player.transform.position.x + currentLookAhead);
        // Normal jumps stay inside a broad vertical dead zone. Only unusually
        // high routes move the view, and the rail crop limits that movement.
        float halfHeight = framing != null ? framing.viewHeight * .5f : 11f;
        float height = Mathf.Clamp(playerCollider.bounds.max.y - (halfHeight - 2f), cameraTrackCenterY, cameraTrackCenterY + cameraHeightLimit);
        cameraAnchor.position = new Vector3(cameraTargetX, height, 0);
        cameraFollow.SetCameraTarget(cameraAnchor);
        float leftEdge = CameraX - ViewHalfWidth;
        if (playerCollider.bounds.max.x < leftEdge + .5f) RespawnPlayer();
    }

    public float Gap(int fromTrain) => trains[fromTrain + 1].Body.position.x - TrainWidth(trains[fromTrain + 1]) * .5f - trains[fromTrain].Body.position.x - TrainWidth(trains[fromTrain]) * .5f;

    private void ResetCamera()
    {
        cameraTargetX = cameraStartX;
        currentCameraSpeed = TargetCameraSpeed;
        currentLookAhead = checkpointSaved && checkpointIndex >= environmentStartTrain ? environmentLookAhead : cameraLookAhead;
        lookAheadVelocity = 0f;
        cameraAnchor.position = new Vector3(cameraStartX, cameraTrackCenterY, 0);
        cameraFollow.SnapTo(cameraAnchor);
    }

    public void RespawnPlayer()
    {
        if (!ready || resetting || finished || Time.timeScale<=0f || Time.frameCount==lastRespawnFrame || Time.time<respawnSafeUntil) return;
        resetting=true;lastRespawnFrame=Time.frameCount;
        cameraFollow.SetCameraTarget(null);
        player.EnablePlayerControl(false);
        body.linearVelocity=Vector2.zero;body.angularVelocity=0;
        body.bodyType=RigidbodyType2D.Kinematic;
        rider.ResetSupport();
        if(presentation!=null)presentation.Freeze();
        if(player.systemVariables.spritesParent!=null)player.systemVariables.spritesParent.SetActive(false);
        foreach(TrainMotion train in trains)
        {
            train.SetPaused(true);
            TrainGifAnimator animation=train.GetComponentInChildren<TrainGifAnimator>();if(animation!=null)animation.enabled=false;
        }
        if(equipment!=null)foreach(TrainEquipment control in equipment)control.Freeze(true);
        if(sounds!=null)sounds.Death();
        ResetArrangement();
        player.SetSpawnPoint(spawnPoint);
        player.ResetForRespawn(spawnPosition);
        StartTrains();
        body.linearVelocity=new Vector2(trains[checkpointSaved?checkpointIndex:0].CurrentSpeed,0);
        if(presentation!=null)presentation.ResetPose();
        lastRespawnFrame=Time.frameCount;respawnSafeUntil=Time.time+.08f;
        resetting=false;
        if(sounds!=null)sounds.Respawn();
        Physics2D.SyncTransforms();
    }

    private void ResetArrangement()
    {
        resetting = true;
        finished = false;
        timedElapsed = 0;
        lastTrain = checkpointSaved ? checkpointIndex : 0;
        for(int i=0;i<trains.Length;i++)
        {
            TrainMotion train = trains[i];
            train.ResetAt(checkpointSaved ? checkpointPositions[i] : initialPositions[i]);
            train.SetPaused(true);
            TrainGifAnimator animation = train.GetComponentInChildren<TrainGifAnimator>();
            if(animation!=null){animation.enabled=true;animation.ResetAnimation();}
        }
        if(equipment!=null) foreach(TrainEquipment control in equipment){control.ResetEquipment();control.Freeze(true);}
        if(environment!=null)foreach(EnvironmentSection section in environment)section.ResetSection();
        introduced = new bool[trains.Length];
        rider.ResetSupport();
        body.linearVelocity = Vector2.zero;
        spawnPoint.position = spawnPosition;
        player.SetSpawnPoint(spawnPoint);
        body.position = spawnPosition;
        cameraFollow.xSmoothTime = originalCameraSmoothTime;
        ResetCamera();
        Physics2D.SyncTransforms();
    }

    public void SetCheckpoint(int index, Transform newSpawn = null, SpriteRenderer newLight = null)
    {
        if(!ready || finished || resetting || (checkpointSaved && index<=checkpointIndex))return;
        if(newSpawn!=null)checkpointSpawn=newSpawn;
        if(newLight!=null)checkpointLight=newLight;
        checkpointSaved = true;
        checkpointIndex = index;
        checkpointPositions = new Vector2[trains.Length];
        for(int i=0;i<trains.Length;i++) checkpointPositions[i]=trains[i].Body.position;
        spawnPosition = (Vector3)(trains[index].Body.position +
            (Vector2)(checkpointSpawn.position - trains[index].transform.position));
        cameraStartX = spawnPosition.x + cameraLookAhead;
        spawnPoint.position = spawnPosition;
        player.SetSpawnPoint(spawnPoint);
        if(checkpointLight!=null) checkpointLight.color = new Color32(60,222,234,255);
        if(sounds!=null)sounds.Checkpoint();
    }

    public void CompleteLevel()
    {
        if (!ready || resetting || finished) return;
        finished = true;
        if(background!=null)background.Freeze();
        if(sounds!=null)sounds.Complete();
        cameraFollow.SetCameraTarget(null);
        foreach (TrainMotion train in trains)
        {
            train.SetPaused(true);
            TrainGifAnimator animation=train.GetComponentInChildren<TrainGifAnimator>();if(animation!=null)animation.enabled=false;
        }
        if(equipment!=null)foreach(TrainEquipment control in equipment)control.Freeze(true);
        rider.ResetSupport();
        body.linearVelocity = Vector2.zero;
        body.bodyType=RigidbodyType2D.Kinematic;
        body.simulated=false;
        player.EnablePlayerControl(false);
        player.enabled=false;
        if(presentation!=null)presentation.Freeze();
        if(closing!=null && closing.enabled)StartCoroutine(closing.Play());
        else {if(ui!=null)ui.ShowPassed();if(sounds!=null)sounds.ShowCompletion();}
    }

    public void RestartLevel()
    {
        if(!ready || resetting)return;
        Time.timeScale=1f;
        #if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path,
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        #else
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
        #endif
    }

    public int CurrentTrainIndex()
    {
        if (rider != null && rider.Support != null)
            for (int i = 0; i < trains.Length; i++) if (trains[i] == rider.Support) return i;
        return -1;
    }

    private void OnDestroy(){if(player!=null)player.respawnHandler=null;}
}

