using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    #region view
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotationStore;
    private Vector2 mouseInput;

    public bool invertLook;

    private Camera camera;
    #endregion

    #region movement

    public float moveSpeed = 5f, runspeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;

    public CharacterController chacController;

    public float jumpForce = 12f, gravityMod = 2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    #endregion

    #region shooting

    public GameObject bulletImpact;
   // public float timeBetweenShots = 0.1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f, /*heatPerShot = 1f,*/ coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    #endregion

    #region GunSwitching

    public Guns[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    #endregion

    public int maxHealth = 100;
    private int currentHealth;

    public Animator animator;
    public GameObject playerModel;
    public Transform modelGunPoint, gunHolder;

    public Material[] allSkins;

    public float adsSpeed = 5f;
    public Transform adsOutPoint, adsInPoint;

    public AudioSource footstepSLow, footstepFast;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        camera = Camera.main;

        UIController.instance.weaponTemperatureSlider.maxValue = maxHeat;

        //SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        //Transform newTrans = SpawnManager.instance.GetSpawnPoint();

        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;

        if(photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];

    }

    // Update is called once per frame
    void Update()
    {
        if(photonView.IsMine)
        {

        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotationStore += mouseInput.y;

        //Math clamp limit it to these 2 values.
        verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);


        //Checks invertion
        if(invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if(Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runspeed;

            if(!footstepFast.isPlaying &&  moveDir != Vector3.zero)
                {
                    footstepFast.Play();
                    footstepSLow.Stop();
                }
        }
        else
        {
            activeMoveSpeed = moveSpeed;

                if (!footstepSLow.isPlaying && moveDir != Vector3.zero)
                {
                    footstepSLow.Play();
                    footstepFast.Stop();
                }
            }

        if(moveDir == Vector3.zero || !isGrounded)
            {
                footstepSLow.Stop();
                footstepFast.Stop();
            }

        float yVel = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
        movement.y = yVel;

        if(chacController.isGrounded)
        {
            movement.y = 0f;
        }

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

       

        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        chacController.Move(movement * Time.deltaTime);

        if(allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;

            if(muzzleCounter <= 0)
            {

                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }

        if (!overHeated)
        {

            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overheatCoolRate * Time.deltaTime;
            if(heatCounter <= 0)
            {
                overHeated = false;

                UIController.instance.overheatedMessage.gameObject.SetActive(false);
            }
        }

        if(heatCounter < 0)
        {
            heatCounter = 0f;
        }

        UIController.instance.weaponTemperatureSlider.value = heatCounter;

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;

            if (selectedGun >= allGuns.Length)
            {
                selectedGun = allGuns.Length - 1;
            }
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;

            if (selectedGun < 0)
            {
                selectedGun = 0;
            }
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

        for(int i = 0; i < allGuns.Length; i++)
        {
            if(Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
        }

            animator.SetBool("grounded", isGrounded);
            animator.SetFloat("speed", moveDir.magnitude);


        if(Input.GetMouseButton(1))
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, allGuns[selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
            }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if(Cursor.lockState == CursorLockMode.None)
        {
            if(Input.GetMouseButtonDown(0) && !UIController.instance.optionsMenu.activeInHierarchy)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
      }
    }

    private void Shoot()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = camera.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.tag == "Player")
            {
                //Quatertion, tells than it just the default rotation.
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                //Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
           
        }


        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
      
    }

    //PunRPC, mean it will call on the same time on every copy of the game on the network.
    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if(photonView.IsMine)
        {
            // Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);

            currentHealth -= damageAmount;

            if(currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
                UIController.instance.overheatedMessage.gameObject.SetActive(false);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }

            UIController.instance.healthSlider.value = currentHealth;

        }
    }

    private void LateUpdate()
    {
        if(photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                camera.transform.position = viewPoint.position;
                camera.transform.rotation = viewPoint.rotation;
            }
            else
            {
                camera.transform.position = MatchManager.instance.mapCamPoint.position;
                camera.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
            
        }
    }

    void SwitchGun()
    {
        foreach(Guns gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }
}
