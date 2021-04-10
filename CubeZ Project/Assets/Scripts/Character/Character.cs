﻿using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterStatsController))]
public class Character : MonoBehaviour, IAnimatiomStateController, ICheckerStats, IInvokerMono, IGeterHit, ICharacter
{
    private Animator animator;
    private CharacterAnimatorObserver animatorObserver;
    private Rigidbody _rb;
    private CharacterData characterData;

    BoxCollider boxCollider;

    private WeaponItem currentWeapon = null;


    private const string VERTICAL_INPUT_NAME = "Vertical";
    private const string HORIZONTAL_INPUT_NAME = "Horizontal";
    private const string PATH_CHARACTER_SETTINGS = "Character/CharacterSettings";
    private const string ATTACK_NAME_ANIM = "AttackGeneric";
    private const string DEAD_PLAYER_TAG = "DeadPlayer";
    private const string DEAD_ANIM_NAME = "Death";
    private const string KEY_CODE_OFF_SLEEP_NAME = "offSleep";


    private const string TAG_TREE = "Tree";
    private const string TAG_INTERACTION_OBJECT = "InteractionObject";
    private const string TAG_DOOR = "Door";
    private const string ZOMBIE_TAG = "Zombie";
    private const string ZOMBIE_AREA_TAG = "ZombieArea";
    private const string FIRE_TAG = "FireArea";



    private TypeAnimation animationState = TypeAnimation.Idle2;

    [SerializeField, ReadOnlyField] CharacterDataSettings characterDataSettings;
    [Header("Тело персонажа")]
    [SerializeField] Transform skinCharacter;
    [Header("Триггер персонажа")]
    [SerializeField] CharacterTrigger characterTrigger;

    public CharacterData CharacterStats { get => characterData; }

    private CharacterStatsDataNeed healthStats;
    private CharacterStatsDataNeed runStats;

    private int baseDamage = 6;
    private int currentDamage;

    private bool characterActive = true;

    public event Action<ItemBaseData> onWeaponChanged;

    public event Action onDead;

    public event Action<bool> onSleep;


    private bool isDead = false;

    private bool isFrezzed = false;

    private bool isSleeping = false;

    private bool isFatigue = false;

    private bool collisizedInteractionObject = false;

    private bool inFireArea = false;

    private Vector3 lastPosition;
    private Quaternion lastQuaternion;

    private Quaternion startQuuaterion;

    public float Speed { get => characterData.speed; }

    public int Health { get => healthStats.value; }
    public bool IsDead { get => isDead; }
    public WeaponItem CurrentWeapon { get => currentWeapon; }
    public bool IsSleeping { get => isSleeping; }
    public bool CollisizedInteractionObject { get => collisizedInteractionObject; }
    public bool InFireArea { get => inFireArea; }




    // Start is called before the first frame update

    private void Awake()
    {
        if (skinCharacter == null)
        {
            throw new CharacterException("Character skin not seted");
        }

        startQuuaterion = skinCharacter.transform.localRotation;
        characterDataSettings = Resources.Load<CharacterDataSettings>(PATH_CHARACTER_SETTINGS);
        characterData = new CharacterData(characterDataSettings.GetData());
        healthStats = characterData.GetDictonaryNeeds()[NeedCharacterType.Health];
        runStats = characterData.GetDictonaryNeeds()[NeedCharacterType.Run];


        if (characterDataSettings == null)
        {
            throw new CharacterException("character settings is null");
        }
        if (characterData.speed <= 0)
        {
            throw new CharacterException("Speed <= 0");
        }

        if (!TryGetComponent(out boxCollider))
        {
            throw new CharacterException("Box Colider component not found on Character");
        }



        baseDamage = characterData.damage;
        ReturnToBaseDamage();

        characterTrigger.onEnter += CharacterEnterOnTrigger;
        characterTrigger.onExit += CharacterExitOnTrigger;


    }

    private void CharacterEnterOnTrigger(string tag)
    {
        if (tag == this.tag)
        {
            return;
        }
        if (tag == FIRE_TAG)
        {
            inFireArea  = true;
        }
        
    }

    private void CharacterExitOnTrigger(string tag)
    {
        if (tag == this.tag)
        {
            return;
        }
        if (tag == FIRE_TAG)
        {
            inFireArea = false;
        }
    }

    void Start()
    {

#if UNITY_EDITOR
        CheckValidStats();

#endif
        if (ControlManagerObject.Manager == null)
        {
            throw new CharacterException("Control manager object not found");
        }

        if (ControlManagerObject.Manager.ControlManager == null)
        {
            throw new CharacterException("Control manager not found");
        }


        animator = transform.GetChild(0).GetComponent<Animator>();

        animatorObserver = transform.GetChild(0).GetComponent<CharacterAnimatorObserver>();
        _rb = GetComponent<Rigidbody>();

        animatorObserver.onAttackEvent += Damage;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
        {
            return;
        }


        ActionsCharactersCheck();

        if (isSleeping)
        {
            if (ControlManagerObject.Manager != null && ControlManagerObject.Manager.ControlManager != null)
            {
            if (Input.GetKeyDown(ControlManagerObject.Manager.ControlManager.GetKeyCodeByFragment(KEY_CODE_OFF_SLEEP_NAME)))
            {
                Awakening();
            }
            }

        }

    }

    

    private void ActionsCharactersCheck()
    {
        StandLockInput();
    }

    private void StandLockInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            characterActive = !characterActive;
            if (characterActive == false)
            {
                SetAnimationState(TypeAnimation.SitandLook);
            }

        }
    }

    private void LookAtDirection()
    {
        Vector3 dir = new Vector3(Input.GetAxis(HORIZONTAL_INPUT_NAME), 0.0f, Input.GetAxis(VERTICAL_INPUT_NAME));

        if (dir != Vector3.zero)
        {
            _rb.MoveRotation(Quaternion.LookRotation(dir * characterData.rotateSpeed * Time.deltaTime));
        }

    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        else
        {
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }
        if (characterActive && !isFrezzed)
        {
            LookAtDirection();
            Control();
        }

    }

    private void Control()
    {


        float speed = 0;

        if (!CharacterIsStand())
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                speed = Walk();
            }

            else
            {
                if (runStats.value > 0)
                {
                    speed = Run();
                }

                else
                {

                    SetAnimationState(TypeAnimation.Idle2);
                }

            }
        }



        else
        {

            SetAnimationState(TypeAnimation.Idle2);

            if (Input.GetMouseButtonDown(0))
            {
                int varintsAttack = Random.Range(1, 4);

                string animName = $"{ATTACK_NAME_ANIM}{varintsAttack}";
                SetAnimationState(animName.ToEnum<TypeAnimation>());
            }
        }



        Vector3 tempVect = transform.forward;
        tempVect = tempVect.normalized * speed * Time.deltaTime;
        _rb.MovePosition(transform.position + tempVect);
    }

    private float Walk()
    {
        float speed =  isFatigue == false ? characterData.speed : characterData.speed - 1;
        SetAnimationState(TypeAnimation.Walk);
        return speed;
    }

    private float Run()
    {
        
        float speed = isFatigue == false ?  characterData.speed + 1 : characterData.speed - 1;
        SetAnimationState(isFatigue == false ? TypeAnimation.Run : TypeAnimation.Walk);
        return speed;
    }

    public void SetAnimationState(TypeAnimation type)
    {
        if (type != animationState)
        {
            for (int i = 0; i < animator.runtimeAnimatorController.animationClips.Length; i++)
            {
                animator.SetBool(animator.runtimeAnimatorController.animationClips[i].name, false);
            }
            animator.SetBool(type.ToString(), true);
            animationState = type;
        }
    }

    public void IncrementDamage(int value)
    {
        ReturnToBaseDamage();
        if (value < 0)
        {
            throw new CharacterException("invalid value damage");
        }
        currentDamage += value;
    }

    public void ReturnToBaseDamage()
    {
        currentDamage = baseDamage;
    }

    public bool CharacterUseTheWeapon(WeaponItem weapon)
    {
        CheckWeaponisNull(weapon);
        if (!currentWeapon)
        {
            return false;
        }
        return currentWeapon.data.id == weapon.data.id;
    }

    private static void CheckWeaponisNull(WeaponItem weapon)
    {
        if (!weapon)
        {
            throw new CharacterException("Weapon argument is null");
        }
    }

    public bool CharacterUseWeapon()
    {
        return currentWeapon != null;
    }

    public void SetWeapon(WeaponItem weapon)
    {  
        currentWeapon = weapon;
        onWeaponChanged?.Invoke(currentWeapon == null ? null : currentWeapon.data);
    }

    public void CheckValidStats()
    {
        foreach (var prop in characterData.GetType().GetFields())
        {
            try
            {
                ValidatorData.CheckValidFieldStats(prop, characterData);
            }
            catch (ValidatorDataException)
            {

                throw;
            }
        }

        foreach (var prop in characterData.GetType().GetProperties())
        {
            try
            {
                ValidatorData.CheckValidPropertyStats(prop, characterData);
            }
            catch (ValidatorDataException)
            {

                throw;
            }
        }
    }
#region Attack System


    public void Damage()
    {

        RaycastHit raycastHit;


        if (Physics.Raycast(transform.position, transform.forward, out raycastHit, 2))
        {

            if (raycastHit.collider.tag == ZOMBIE_AREA_TAG)
            {
                BaseZombie targetZombie = null;

                if (!raycastHit.collider.transform.parent.TryGetComponent(out targetZombie))
                {
                    throw new CharacterException("parent zombie area component BaseZombie not found");
                }

                DamageZombie(targetZombie);
                return;
            }


            if (raycastHit.collider.tag == ZOMBIE_TAG)
            {
                DamageZombie(raycastHit);
            }


            if (raycastHit.collider.tag == TAG_TREE)
            {
                DamageTree(raycastHit);
            }

            if (raycastHit.collider.tag == TAG_DOOR)
            {
                DamageDoor(raycastHit);
            }

        }


    }

    private void DamageZombie(RaycastHit raycastHit)
    {
        BaseZombie target = raycastHit.collider.GetComponent<BaseZombie>();
        target.Hit(currentDamage);
        CheckWearWeapon();
    }

    private void DamageZombie(BaseZombie target)
    {
        target.Hit(currentDamage);
        CheckWearWeapon();
    }

    private void DamageTree(RaycastHit raycastHit)
    {
        Tree target = raycastHit.collider.GetComponent<Tree>();
        target.Hit(currentDamage);
        Debug.Log(target.CurrentHealth);
        CheckWearWeapon();
    }

    private void DamageDoor(RaycastHit raycastHit)
    {
        Door target = raycastHit.collider.GetComponent<Door>();
        target.Hit(currentDamage);
        Debug.Log(target.Health);
        CheckWearWeapon();
    }

    private void CheckWearWeapon()
    {
        if (CharacterUseWeapon())
        {

            currentWeapon.dataWeapon.strength -= 1;

            if (currentWeapon.dataWeapon.strength <= 0)
            {
                GameCacheManager.gameCache.inventory.Remove(currentWeapon.data);
                SetWeapon(null);
                ReturnToBaseDamage();

            }
        }
    }



    #endregion

    public void Hit(int hitValue, bool playHitAnim = true)
    {
          healthStats.value = Mathf.Clamp(healthStats.value - hitValue, 0, 100);
            if (playHitAnim)
            {
                if (Random.Range(0, 10) > 7)
                {
                    SetAnimationState(TypeAnimation.GetHit);

                    characterActive = true;
                }
            }
            if (isSleeping)
            {
                Awakening();
                skinCharacter.transform.localRotation = startQuuaterion;
                
            }



        if (healthStats.value <= 0)
        {
            isDead = true;
            healthStats.value = 0;
            tag = DEAD_PLAYER_TAG;
            animator.SetBool(DEAD_ANIM_NAME, true);
            characterActive = false;
            Dead();
        }



        healthStats.CallOnValueChanged();
    }

    private void Dead()
    {
        onDead?.Invoke();
        animator.Play(DEAD_ANIM_NAME);
        _rb.isKinematic = true;
        boxCollider.enabled = false;
        gameObject.AddComponent<CharacterRebel>();
        enabled = false;
    }

    public bool CharacterIsStand()
    {
        float _Hor = Input.GetAxis(HORIZONTAL_INPUT_NAME);
        float _Ver = Input.GetAxis(VERTICAL_INPUT_NAME);

        Vector3 mov = new Vector3(_Hor, 0, _Ver);

        return mov == Vector3.zero;
    }

    public bool IsRunning()
    {
        return animationState == TypeAnimation.Run;
    }

    public void CallInvokingEveryMethod(Action method, float time)
    {
        InvokeRepeating(method.Method.Name, time, time);
    }

    public void CallInvokingMethod(Action method, float time)
    {
        Invoke(method.Method.Name, time);
    }

    private void SetStateFrezze(bool state)
    {
        isFrezzed = state;
    }

    public void ActivateCharacter()
    {
        SetStateFrezze(false);
    }

    public void FrezzeCharacter()
    {
        SetAnimationState(TypeAnimation.Idle2);
        SetStateFrezze(true);
    }

    #region Sleep System


    public void Sleep(Bed bedTarget)
    {
        if (bedTarget == null)
        {
            throw new CharacterException("bed target is null");
        }
        _rb.Sleep();
        CachingLastTransform();
        SetSleepStatus(true);
       SetNewTransform(bedTarget.PointSleep, bedTarget.QuaternionSleep);
        SetAnimationState(TypeAnimation.Idle2);
    }

    private void SetNewTransform(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        skinCharacter.transform.localRotation = rotation;
    }

    private void CachingLastTransform ()
    {
        lastPosition = transform.position;
        lastQuaternion = skinCharacter.transform.localRotation;
        Debug.Log($"Player transform cached. Position: {lastPosition} Rotation: {lastQuaternion}");
    }

   public void Awakening()
    {
        SetSleepStatus(false);
        SetNewTransform(lastPosition, startQuuaterion);
    }

    private void SetSleepStatus (bool status)
    {

        isSleeping = status;
        onSleep?.Invoke(isSleeping);
        SetStateFrezze(isSleeping);
        characterActive = !status;
        WorldManager.Manager.NewTimeScale(status == true ? 4 : 1);
    }

    public void SetFatigue (bool status)
    {
        isFatigue = status;
    }


    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(TAG_INTERACTION_OBJECT))
        {
            SetStateCollisionInteractionObject(true);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(TAG_INTERACTION_OBJECT))
        {
            SetStateCollisionInteractionObject(false);
        }


    }

    private void SetStateCollisionInteractionObject (bool status)
    {
        collisizedInteractionObject = status;
    }

}
