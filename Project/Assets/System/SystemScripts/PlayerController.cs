using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[SelectionBase]
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Space]
    [Header("Movement")]
    [Tooltip("The player's movement physics when on the ground.")]
    public PlayerMovementValues groundMovementValues;
    [Tooltip("The player's movement physics when in the air.")]
    public PlayerMovementValues airMovementValues;
    [Tooltip("The player's movement physics when sliding on a wall.")]
    public PlayerMovementValues wallMovementValues;
    [Tooltip("Whether or not the player can 'swim', i.e. jump while in water.")]
    public bool canSwim = true;
    [Tooltip("What level of slope is no longer considered \"ground\", on a scale of 0-1, with 1 being flat ground, 0 being a vertical wall, and 0.5 being a 45° slope.")]
    public float groundSlopeThreshold = 0.3f;
    [Tooltip("What level of slope is considered a \"wall\", on a scale of 0-1, with 1 being flat ground, 0 being a vertical wall, and 0.5 being a 45° slope.")]
    public float wallSlopeThreshold = 0.9f;

    [Space]
    [Header("Jumping")]
    [Tooltip("The default stats for performing a jump.")]
    public PlayerJumpValues jumpValues;
    [Tooltip("How many seconds the player can be in the air after running off a ledge, and still jump.")]
    public float coyoteTime = 0.5f;
    [Tooltip("How many seconds the player can press the jump button before they touch the ground, and have it still count as a jump.")]
    public float jumpQueueTime = 0.1f;
    [Tooltip("The maximum upwards speed a player can achieve.")]
    public float maxJumpSpeed = 100f;

    [Space]
    [Header("Double Jumping")]
    [Tooltip("The number of jumps the player can perform while already in the air.")]
    public int doubleJumps = 1;
    [Tooltip("The physics of each subsequent double jump the player does. If the player can perform more double jumps than this list has, the player will continuously use the last value in the list.")]
    public PlayerJumpValues[] doubleJumpValues = new PlayerJumpValues[1];

    [Space]
    [Header("Wall Jumping")]
    [Tooltip("Whether or not the player can jump while sliding down walls.")]
    public bool allowWallJump = true;
    [Tooltip("Whether or not wall jumping shoudl replenish the player's wall jumps.")]
    public bool resetDoubleJumpsOnWall = true;
    [Tooltip("The stats for performing a jump while sliding on a wall.")]
    public PlayerWallJumpValues wallJumpValues;

    [Space]
    [Header("Crouching")]
    [Tooltip("Whether or not the player can crouch.")]
    public bool allowCrouch = true;
    [Tooltip("How high the player's collider is when crouching. This is used to slide under objects.")]
    public float crouchColliderHeight = 0.2f;

    [Space]
    [Header("Miscellaneous")]
    [Tooltip("The global y coordinates that will cause the player to respawn if they fall below.")]
    public float deathHeight = -301f;
    [Tooltip("The sound that plays when the player jumps.")]
    public AudioClip jumpSound;
    [Tooltip("The sound that plays when the player hits a wall.")]
    public AudioClip bumpSound;
    [Tooltip("The sound that plays when the player dies.")]
    public AudioClip deathSound;

    // System Variables
    [Space]
    public PlayerSystemVariables systemVariables;
    Vector2 moveDirection = Vector2.zero;
    private bool controlEnabled = true;
    private bool isOnGround = false;
    private bool jumpedOffGround = false;
    private float lastOnGroundTime = 0;
    private float lastLandTime = 0;
    private bool onWallLeft = false;
    private bool onWallRight = false;
    private bool onGroundLastFrame = false;
    private bool wasOnWallLeft = false;
    private bool wasOnWallRight = false;
    private bool isFacingLeft = false;
    private Animator[] animators;

    // Movement
    private PlayerMovementValues currentMovementState;
    private bool isInWater = false;

    // Jump
    private PlayerJumpValues currentJumpValues;
    private bool isJumping = false;
    private bool jumpQueued = false;
    private float lastJumpQueueTime = 0;
    private float lastJumpTime = -1;
    private float lastJumpDuration = -1;
    private float jumpStartedThreshold = 0.1f;
    private int jumpsSinceGroundTouch = 0;
    private Transform currentSpawnPoint;
    private Transform startSpawnPoint;

    // Crouching
    private bool isCrouching = false;
    private Vector2 originalColliderSize = Vector2.one;
    private Vector2 originalColliderOffset = Vector2.zero;

    // Sound
    private float bumpSoundVolume = 0.2f;
    private float bumpMinimumImpulse = 7f;

    // Component References
    private Rigidbody2D rigidbody2D;
    private BoxCollider2D collider2D;
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance != null)
        {
            Debug.Log("WARNING: More than one player detected!");
        }
        instance = this;

        rigidbody2D = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<BoxCollider2D>();
        audioSource = GetComponentInChildren<AudioSource>();

        currentMovementState = groundMovementValues;

        originalColliderSize = collider2D.size;
        originalColliderOffset = collider2D.offset;

        startSpawnPoint = new GameObject().transform;
        startSpawnPoint.position = transform.position;
        startSpawnPoint.gameObject.name = "InitialSpawnPoint";
        currentSpawnPoint = startSpawnPoint;
        animators = GetComponentsInChildren<Animator>(true);
    }

    void Update()
    {
        moveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if(Input.GetButtonDown("Jump"))
        {
            JumpButtonPressed();
        }
        else if(Input.GetButtonUp("Jump"))
        {
            JumpButtonReleased();
        }

        CheckIfCrouching();
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        CheckGroundAndWallState();
        PerformWallEffects(moveDirection);
        DetermineMovementState();

        if (controlEnabled && (isCrouching && !isInWater))
        {
            SetFriction(currentMovementState.crouchFriction);
        }
        else if (controlEnabled && !IsTryingToStop(moveDirection))
        {
            SetFriction(currentMovementState.moveFriction);
            rigidbody2D.AddForce(new Vector3(moveDirection.x * currentMovementState.moveAcceleration, 0, 0));
            if (moveDirection.x != 0) isFacingLeft = moveDirection.x < 0;
        }
        else
        {
            SetFriction(currentMovementState.stopFriction);
        }

        if ((IsOnGround() || (resetDoubleJumpsOnWall && allowWallJump && IsOnWall())) && (Time.time - lastJumpTime) > jumpStartedThreshold)
        {
            jumpsSinceGroundTouch = 0;
        }

        if (controlEnabled && jumpQueued)
        {
            if (CanJump() && (Time.time - lastJumpQueueTime) < jumpQueueTime)
            {
                jumpQueued = false;
                DetermineJumpValues();
                PerformJump();
            }
        }

        DetermineGravityScale();

        rigidbody2D.linearVelocity = new Vector2(
            Mathf.Clamp(rigidbody2D.linearVelocity.x, -currentMovementState.maxSpeed, currentMovementState.maxSpeed),
            Mathf.Clamp(rigidbody2D.linearVelocity.y, -currentMovementState.maxFallSpeed, maxJumpSpeed)
        );

        SetAnimatorStates();

        if (transform.position.y < deathHeight) Respawn();
    }

    private void SetFriction(float aFriction)
    {
        if (IsOnGround())
        {
            PhysicsMaterial2D material = rigidbody2D.sharedMaterial;
            material.friction = aFriction;
            rigidbody2D.sharedMaterial = material;
            rigidbody2D.linearDamping = currentMovementState.airDrag;
        }
        else
        {
            rigidbody2D.linearDamping = aFriction;
        }
    }


    // --------- Movement States ---------

    private void DetermineMovementState()
    {
        if (IsOnGround() && (Time.time - lastJumpTime) > jumpStartedThreshold)
        {
            currentMovementState = groundMovementValues;
            isJumping = false;
        }
        else if (IsOnWall())
        {
            currentMovementState = wallMovementValues;
            isJumping = false;
        }
        else
        {
            currentMovementState = airMovementValues;
        }
    }

    private void CheckGroundAndWallState()
    {
        onGroundLastFrame = isOnGround;
        wasOnWallLeft = onWallLeft;
        wasOnWallRight = onWallRight;

        isOnGround = false;
        isInWater = false;
        onWallLeft = false;
        onWallRight = false;

        systemVariables.groundFilter.useTriggers = false;
        List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        rigidbody2D.GetContacts(systemVariables.groundFilter, contacts);

        foreach (ContactPoint2D eachContact in contacts)
        {
            // Debug.Log(eachContact.normal);
            if(eachContact.normal.y > groundSlopeThreshold)
            {
                isOnGround = true;
            }
            if(eachContact.normal.x < -wallSlopeThreshold)
            {
                onWallRight = true;
            }
            if(eachContact.normal.x > wallSlopeThreshold)
            {
                onWallLeft = true;
            }
        }

        Physics2D.queriesHitTriggers = true;
        systemVariables.waterFilter.useTriggers = true;
        Collider2D[] triggerColliders = new Collider2D[1];
        if(collider2D.Overlap(systemVariables.waterFilter, triggerColliders) > 0)
        {
            isOnGround = canSwim;
            isInWater = true;
        }

        if (onGroundLastFrame && !isOnGround)
        {
            lastOnGroundTime = Time.time;
        }
    }

    private bool IsOnGround()
    {
        return isOnGround;
    }

    private bool IsOnGroundWithCoyoteTime()
    {
        return IsOnGround() || ((jumpsSinceGroundTouch <= 0) && (Time.time - lastOnGroundTime) < coyoteTime);
    }

    private void PerformWallEffects(Vector2 moveDirection)
    {
        if (!onGroundLastFrame && isOnGround)
        {
            lastLandTime = Time.time;
            if (systemVariables.landParticle != null) systemVariables.landParticle.Play();
            jumpedOffGround = false;
        }
        
        if (!wasOnWallLeft && onWallLeft)
        {
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.leftWallHitParticle != null) systemVariables.leftWallHitParticle.Play();
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.leftWallSlideParticle != null && !systemVariables.leftWallSlideParticle.isPlaying)
            {
                systemVariables.leftWallSlideParticle.Play();
            }
        }
        if (!IsOnGround() && moveDirection != Vector2.zero && !onWallLeft && systemVariables.leftWallSlideParticle != null && systemVariables.leftWallSlideParticle.isPlaying)
        {
            systemVariables.leftWallSlideParticle.Stop();
        }

        if (!wasOnWallRight && onWallRight)
        {
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.rightWallHitParticle != null) systemVariables.rightWallHitParticle.Play();
            if (!IsOnGround() && moveDirection != Vector2.zero && systemVariables.rightWallSlideParticle != null && !systemVariables.rightWallSlideParticle.isPlaying)
            {
                systemVariables.rightWallSlideParticle.Play();
            }
        }
        if (!IsOnGround() && moveDirection != Vector2.zero && !onWallRight && systemVariables.rightWallSlideParticle != null && systemVariables.rightWallSlideParticle.isPlaying)
        {
            systemVariables.rightWallSlideParticle.Stop();
        }
    }

    private bool IsOnWall()
    {
        return onWallLeft || onWallRight;
    }

    private bool IsTryingToStop(Vector2 moveDirection)
    {
        if (!IsOnWall() && moveDirection == Vector2.zero) return true;
        if (!IsOnGround() && onWallLeft && moveDirection.x < -0.1f) return true;
        if (!IsOnGround() && onWallRight && moveDirection.x > 0.1f) return true;
        return false;
    }

    private void DetermineGravityScale()
    {
        if (isCrouching)
        {
            rigidbody2D.gravityScale = currentMovementState.crouchGravity;
        }
        else if (isJumping && Mathf.Abs(rigidbody2D.linearVelocity.y) < currentJumpValues.airHangThreshold && (Time.time - lastJumpTime) > jumpStartedThreshold)
        {
            rigidbody2D.gravityScale = currentJumpValues.airHangGravity;
        }
        else if (isJumping && rigidbody2D.linearVelocity.y > 0)
        {
            rigidbody2D.gravityScale = currentJumpValues.jumpGravity;
        }
        else
        {
            rigidbody2D.gravityScale = currentMovementState.fallGravity;
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        CheckCollisionImpulse(other);
    }
    void OnCollisionStay2D(Collision2D other)
    {
        CheckCollisionImpulse(other);
    }

    private void CheckCollisionImpulse(Collision2D other)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[other.contactCount];
		other.GetContacts(contacts);
		float totalImpulse = 0;
		foreach (ContactPoint2D contact in contacts) {
			totalImpulse += contact.normalImpulse;
		}
        if(totalImpulse > bumpMinimumImpulse)
        {
            // Debug.Log(totalImpulse);
            PlayBumpSound();
        }
    }


    // --------- Jumping ---------

    private bool CanJump()
    {
        return IsOnGroundWithCoyoteTime()
            || (IsOnWall() && allowWallJump)
            || (jumpsSinceGroundTouch <= GetAvailableJumps() && !isInWater);
    }

    private int GetAvailableJumps()
    {
        if (jumpedOffGround)
        {
            return doubleJumps;
        }
        else
        {
            return doubleJumps - 1;
        }
    }

    private void DetermineJumpValues()
    {
        if (!IsOnGround() && IsOnWall())
        {
            currentJumpValues = wallJumpValues;
        }
        else if (IsOnGroundWithCoyoteTime()) 
        {
            currentJumpValues = jumpValues;
        }
        else // Air Multi Jumps
        {
            currentJumpValues = doubleJumpValues[Mathf.Clamp(jumpsSinceGroundTouch - 1, 0, doubleJumpValues.Length - 1)];
        }
    }

    private void PerformJump()
    {
        rigidbody2D.gravityScale = currentJumpValues.jumpGravity;
        rigidbody2D.linearVelocityY = Mathf.Max(0, rigidbody2D.linearVelocityY);
        rigidbody2D.AddForce(new Vector3(0, currentJumpValues.jumpForce, 0));
        if (currentJumpValues is PlayerWallJumpValues)
        {
            PlayerWallJumpValues wallJumpValues = currentJumpValues as PlayerWallJumpValues;
            if (onWallRight)
            {
                rigidbody2D.AddForce(new Vector3(-wallJumpValues.horizontalJumpForce, 0, 0));
            }
            else if (onWallLeft)
            {
                rigidbody2D.AddForce(new Vector3(wallJumpValues.horizontalJumpForce, 0, 0));
            }
        }
        if (IsOnGroundWithCoyoteTime()) jumpedOffGround = true;
        isJumping = true;
        lastJumpTime = Time.time;
        jumpsSinceGroundTouch++;
        SetAnimatorTrigger("jump");
        PlaySfx(jumpSound,
            Random.Range(1f - systemVariables.randomJumpSoundPitchFluctuation, 1f + systemVariables.randomJumpSoundPitchFluctuation)
                 + (jumpsSinceGroundTouch * systemVariables.jumpSoundSequencePitchIncrease)
            );
    }

    private void QueueJump()
    {
        jumpQueued = true;
        lastJumpQueueTime = Time.time;
    }


    // --------- Input ---------
    public void JumpButtonPressed()
    {
        if (controlEnabled)
        {
            QueueJump();
        }
    }

    public void JumpButtonReleased()
    {
        isJumping = false;
        rigidbody2D.gravityScale = currentMovementState.fallGravity;
        lastJumpDuration = Time.time - lastJumpTime;
        ResetAnimatorTrigger("jump");
    }

    public void CheckIfCrouching()
    {
        if (controlEnabled && allowCrouch && !isCrouching && moveDirection.y < 0)
        {
            isCrouching = true;
            collider2D.size = new Vector2(originalColliderSize.x, crouchColliderHeight);
            collider2D.offset = new Vector2(originalColliderOffset.x, -(crouchColliderHeight / 2));
            if (systemVariables.slideParticle != null && !systemVariables.slideParticle.isPlaying)
            {
                systemVariables.slideParticle.Play();
            }
        }
        else if (isCrouching && (moveDirection.y >= 0 || !controlEnabled || !allowCrouch))
        {
            isCrouching = false;
            collider2D.size = originalColliderSize;
            collider2D.offset = originalColliderOffset;
            if (systemVariables.slideParticle != null && systemVariables.slideParticle.isPlaying)
            {
                systemVariables.slideParticle.Stop();
            }
        }
    }

    // --------- Spawning ---------

    public void Respawn()
    {
        if (systemVariables.deathParticlePrefab != null)
        {
            GameObject.Instantiate(systemVariables.deathParticlePrefab, transform.position, Quaternion.identity);
        }
        StartCoroutine((ResetCameraCoroutine()));
        PlaySfx(deathSound, 1);

        if (currentSpawnPoint != null)
        {
            transform.position = currentSpawnPoint.position;
        }
        else if (startSpawnPoint != null)
        {
            transform.position = startSpawnPoint.position;
        }
    }

    private IEnumerator ResetCameraCoroutine()
    {
        CameraFollow.instance.SetCameraTarget(null);
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        EnablePlayerControl(false);
        if (systemVariables.spritesParent != null) systemVariables.spritesParent.SetActive(false);
        yield return new WaitForSeconds(systemVariables.waitToResetCameraAfterDeath);
        CameraFollow.instance.FocusCameraToPlayer();
        if (systemVariables.spritesParent != null) systemVariables.spritesParent.SetActive(true);
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        EnablePlayerControl(true);
    }

    public void SetSpawnPoint(Transform aPoint)
    {
        currentSpawnPoint = aPoint;
    }


    // --------- Animation ---------

    private void SetAnimatorStates()
    {
        SetAnimatorBool("onGround", IsOnGround());
        SetAnimatorBool("falling", !IsOnGround() && IsFalling());
        SetAnimatorBool("crouching", isCrouching);
        SetAnimatorBool("wallLeft", onWallLeft);
        SetAnimatorBool("wallRight", onWallRight);
        SetAnimatorFloat("VerticalMovement", moveDirection.y != 0 ? (rigidbody2D.linearVelocity.y/currentMovementState.maxSpeed) : 0);
        SetAnimatorFloat("HorizontalMovement", moveDirection.x != 0 ? (rigidbody2D.linearVelocity.x/currentMovementState.maxSpeed) : 0);
        SetAnimatorFloat("VerticalSpeed", rigidbody2D.linearVelocity.y);
        SetAnimatorFloat("HorizontalSpeed", rigidbody2D.linearVelocity.x);
        SetAnimatorInt("AirJumps", jumpsSinceGroundTouch);
        foreach (SpriteRenderer each in systemVariables.flipSprites)
        {
            if (each != null) each.flipX = isFacingLeft;
        }
    }

    private void SetAnimatorTrigger(string aTrigger)
    {
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetTrigger(aTrigger);
        }
    }

    private void ResetAnimatorTrigger(string aTrigger)
    {
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.ResetTrigger(aTrigger);
        }
    }

    private void SetAnimatorBool(string aBoolName, bool aBoolValue)
    {
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetBool(aBoolName, aBoolValue);
        }
    }

    private void SetAnimatorFloat(string aName, float aValue)
    {
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetFloat(aName, aValue);
        }
    }

    private void SetAnimatorInt(string aName, int aValue)
    {
        foreach (Animator each in animators)
        {
            if (each != null && each.gameObject.activeInHierarchy) each.SetInteger(aName, aValue);
        }
    }

    public bool IsFalling()
    {
        return rigidbody2D.linearVelocityY < 0;
    }

    public bool IsFacingLeft()
    {
        return isFacingLeft;
    }

    // --------- Audio ---------

    private void PlaySfx(AudioClip clip, float pitch)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, systemVariables.sfxVolume);
        }
    }

    private void PlayBumpSound()
    {
        if(audioSource != null && bumpSound != null)
        {
            audioSource.pitch = 1;
            audioSource.PlayOneShot(bumpSound, systemVariables.sfxVolume * bumpSoundVolume);
        }
    }

    // --------- External Manipulation ---------

    public void EnablePlayerControl(bool enable)
    {
        controlEnabled = enable;
        if (!controlEnabled)
        {
            isCrouching = false;
            isJumping = false;
            jumpQueued = false;
        }
    }

    public void SetDoubleJumpCount(int amount)
    {
        doubleJumps = amount;
    }

    public void AdjustDoubleJumpCount(int amount)
    {
        doubleJumps += amount;
    }

    public void EnableCrouch(bool enable)
    {
        allowCrouch = enable;
    }

    public void EnableWallJump(bool enable)
    {
        allowWallJump = enable;
    }

    public void EnableWallsResetDoubleJumps(bool enable)
    {
        resetDoubleJumpsOnWall = enable;
    }

    public void EnableSwim(bool enable)
    {
        canSwim = enable;
    }

    public void RefreshDoubleJumps()
    {
        jumpsSinceGroundTouch = 0;
    }

    void OnDrawGizmos()
    {
        // Draw line at deathHeight
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawLine(new Vector3(-5000, deathHeight, 0), new Vector3(5000, deathHeight, 0));
    }
}

[System.Serializable]
public class PlayerMovementValues
{
    [Space]
    [Header("Moving")]
    [Tooltip("How quickly the player accelerates.")]
    public float moveAcceleration = 40f;
    [Tooltip("How much friction the player experiences when actively moving left/right.")]
    public float moveFriction = 0f;
    [Tooltip("How much friction the player experiences when not actively moving.")]
    public float stopFriction = 3f;
    [Tooltip("How much air drag the player experiences.")]
    public float airDrag = 0.4f;
    [Tooltip("The maximum left/right speed the player can achieve.")]
    public float maxSpeed = 16f;

    [Space]
    [Header("Falling")]
    [Tooltip("How much gravity the player experiences.")]
    public float fallGravity = 3f;
    [Tooltip("The maximum downwards speed the player can achieve.")]
    public float maxFallSpeed = 50f;

    [Space]
    [Header("Crouching")]
    [Tooltip("The friction the player experiences when crouching. Use a low number to slide.")]
    public float crouchFriction = 0.01f;
    [Tooltip("The gravity the player experiences when crouching. Use a high number to fast fall while holding down.")]
    public float crouchGravity = 12f;
}

[System.Serializable]
public class PlayerJumpValues
{
    [Tooltip("The amount of upwards force the player experiences when jumping.")]
    public float jumpForce = 600f;
    [Tooltip("The amount of gravity the player experiences when jumping.")]
    public float jumpGravity = 1.5f;
    [Tooltip("A number to quantify how long the player will 'hang' at the apex of their jump (not specifically seconds).")]
    public float airHangThreshold = 0.35f;
    [Tooltip("The amount of gravity the player experiences when at the apex of their jump. A low number here will allow the player to 'hang' in the air.")]
    public float airHangGravity = 1.7f;
}

[System.Serializable]
public class PlayerWallJumpValues : PlayerJumpValues
{
    [Tooltip("The amount of outwards force the player experiences when jumping while sliding on a wall. Use a high number to encourage left/right wall jumping, or a low number to encourage wall climbing.")]
    public float horizontalJumpForce = 500f;
}

[System.Serializable]
public class PlayerSystemVariables
{
    public ContactFilter2D groundFilter;
    public ContactFilter2D waterFilter;
    public GameObject spritesParent;
    public SpriteRenderer[] flipSprites;
    [HideInInspector]
    public float waitToResetCameraAfterDeath = 2;
    [HideInInspector]
    public float sfxVolume = 1;
    [HideInInspector]
    public float randomJumpSoundPitchFluctuation = 0.05f;
    [HideInInspector]
    public float jumpSoundSequencePitchIncrease = 0.1f;

    [Space]
    [Header("Particles")]
    public ParticleSystem landParticle;
    public ParticleSystem leftWallHitParticle;
    public ParticleSystem rightWallHitParticle;
    public ParticleSystem slideParticle;
    public ParticleSystem leftWallSlideParticle;
    public ParticleSystem rightWallSlideParticle;
    public GameObject deathParticlePrefab;
}