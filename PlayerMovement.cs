using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioClip jumpSound;
    public AudioClip releaseSound;
    public AudioClip stackSound;
    public AudioClip walkSound;
    public AudioClip landSound;
    public AudioClip SlimeScream;

    public GameManager gm;

    public CharacterController controller;
    public float speed = 12f;
    public float sprintMultiplyer = 2f;
    public KeyCode sprint;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance;
    public LayerMask groundMask;

    public Vector3 velocity;
    public bool isGrounded;
    public bool rdyToJump = true;
    public bool wasAerial;

    float grav;

    public GameObject stickyPrefab;

    public int blobCount = 4;

    //jumpcount determines if we can double jump or not
    public int jumpCount;
    public Transform shotSpawn;

    float unstackTimer = 0;

    public Transform cam;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    public float forceDown = 10f;

    public bool aiming;

    Animator anim;
    public List<Transform> stackAnim;
    Vector3 direction;
    public Transform Mesh;

    ResetController rc;

    public bool canMove;

    public bool doubleJump;
    public bool Shoot;

    void Awake()
    {

        gm = FindObjectOfType<GameManager>();
        rc = FindObjectOfType<ResetController>();
        anim = GetComponent<Animator>();

    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        grav = gravity;

        canMove = true;

    }

    void Update()
    {
        blobCount = gm.blobCount;

        if (canMove == true)
        {
            Movement();
        }



        if (blobCount >= 1 && isGrounded == false)
        {
            if (Input.GetButtonDown("Jump") && jumpCount >= 0 && jumpCount < 5 && doubleJump == true)
            {
               
                audioSource.PlayOneShot(jumpSound);

                if (!Physics.Raycast(transform.position, Vector3.down, 50))
                {
                    Invoke("PlayScream", 0.5f);
                }

                anim.SetTrigger("Jump");
                Animation(true,true);
                Invoke("DoubleJump", 0.1f);

            }

        }

        if (blobCount >= 2)
        {
            //Swing
        }

        if (Input.GetButtonDown("Fire2") && Shoot == true)
        {
            aiming = true;
        }

        if (blobCount >= 1)
        {

            ThrowBlob();

        }

    }


    void Movement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        direction = new Vector3(x, 0f, z).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
            controller.Move(new Vector3(0f, -0.01f, 0));

            if(stackAnim.Count > 0)
            {
                Animation(false, false);
            }
            //else
            {
                anim.SetBool("Moving", true);
            }
        }
        else
        {
            if (stackAnim.Count > 0)
            {
                Animation(false, false);
            }
            //else
            {
                anim.SetBool("Moving", false);
            }
        }
        anim.SetBool("Grounded", isGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded && rdyToJump == true)
        {
            audioSource.PlayOneShot(jumpSound);
            anim.SetTrigger("Jump");
            Animation(true,false);
            rdyToJump = false;
            jumpCount += 1;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * grav);
            Invoke("ResetJump", 0.1f);

        }

        if (isGrounded && wasAerial == true)
        {
            audioSource.PlayOneShot(landSound);
            jumpCount = 0;
            wasAerial = false;
        }

        velocity.y += grav * Time.deltaTime;
        velocity.y = Mathf.Clamp(velocity.y, -20, 20);
        controller.Move(velocity * Time.deltaTime);

        if (isGrounded == false)
        {
            if (gm.blobCount < 5)
            {
                unstackTimer -= Time.deltaTime;
                Ray ray = new Ray(groundCheck.position, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit detectedBlob, .5f) && unstackTimer <= 0)
                {
                    GameObject currentBlob = detectedBlob.collider.gameObject;
                    if (currentBlob.layer == 10 || currentBlob.layer == 12)
                    {
                        StackBlob(currentBlob);
                    }
                    
                }
            }
        }
            
        if (Input.GetKeyDown(KeyCode.R))
        {
            //rc.currentArea.GetComponent<CheckpointController>().Send();
            rc.Restart();
        }

    }

    public void Animation(bool jump, bool dj)
    {
        if(stackAnim.Count > 0)
        {
            for (int i = 0; i < stackAnim.Count; i++)
            {
                stackAnim[i].GetComponent<Animator>().SetBool("Moveing", false);
                if (jump)
                {
                    if(dj)
                        if (i != stackAnim.Count - 1)
                            stackAnim[i].DOPunchPosition((Vector3.down * (i + 1)) / 4, .2f, 2, 1, false);
                    if(!dj)
                        stackAnim[i].DOPunchPosition((Vector3.down * (i + 1)) / 4, .2f, 2, 1, false);
                }
                if (!jump && direction.magnitude >= 0.1f)
                {
                    if (i != stackAnim.Count - 1)
                        stackAnim[i].localPosition = ((Vector3.down) * (i + 1)) + ((Vector3.forward) * ((stackAnim.Count - i - 1))) / 8;
                    Mesh.localPosition = (Vector3.forward * ((i + 1))) / 8;
                }
                if (!jump && direction.magnitude < 0.1f)
                {
                    stackAnim[i].localPosition = ((Vector3.down) * (i + 1));
                    Mesh.localPosition = Vector3.zero;
                }
            }
        }
    }

    void DoubleJump()
    {

        jumpCount += 1;
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * grav);


        GameObject bottomBlob = gm.getBottomBlob();
        UnstackBlob(bottomBlob, false);
        bottomBlob.GetComponent<Rigidbody>().AddForce(Vector3.down * forceDown, ForceMode.Impulse);


    }

    void ThrowBlob()
    {
        if (Input.GetButtonDown("Fire1") && aiming == true)
        {

            Instantiate(stickyPrefab, shotSpawn.position, shotSpawn.rotation);
            GameObject blobThrown = gm.getTopBlob();
            UnstackBlob(blobThrown, true);
            //gm.RemoveBlob(blobThrown);

        }
    }

    void ResetJump()
    {
        rdyToJump = true;
        wasAerial = true;
    }

    public void StackBlob(GameObject blobToStack)
    {
        gm.AddBlob(blobToStack);

        blobToStack.GetComponent<NavMeshAgent>().enabled = false;
        stackAnim.Add(blobToStack.GetComponent<Transform>());

        transform.position = blobToStack.transform.position + new Vector3(0, gm.blobCount, 0);
        blobToStack.GetComponent<Collider>().enabled = false;
        blobToStack.GetComponent<Rigidbody>().isKinematic = true;
       // blobToStack.GetComponent<Animator>().enabled = false;

        groundCheck.position += new Vector3(0, -1, 0);
        gameObject.GetComponent<CapsuleCollider>().height += 1;
        gameObject.GetComponent<CapsuleCollider>().center += new Vector3(0, -0.5f, 0);

        controller.height += 1;
        controller.center += new Vector3(0, -0.5f, 0);

       
        blobToStack.transform.SetParent(gameObject.transform);

        audioSource.PlayOneShot(stackSound);
    }

    public void UnstackBlob(GameObject blobToUnstack, bool isTop)
    {
        unstackTimer = .2f;
        gm.RemoveBlob(blobToUnstack);

        stackAnim.Remove(blobToUnstack.GetComponent<Transform>());

        groundCheck.position += new Vector3(0, 1, 0);
        gameObject.GetComponent<CapsuleCollider>().height -= 1;
        gameObject.GetComponent<CapsuleCollider>().center -= new Vector3(0, -0.5f, 0);

        controller.height -= 1;
        controller.center -= new Vector3(0, -0.5f, 0);

        if (isTop == false)
        {
            blobToUnstack.GetComponent<basicBlobMovement>().jump = false;
            blobToUnstack.GetComponent<basicBlobMovement>().Return();
            //blobToUnstack.GetComponent<Animator>().enabled = false;

            blobToUnstack.GetComponent<Collider>().enabled = true;
            blobToUnstack.GetComponent<Rigidbody>().isKinematic = false;
            blobToUnstack.GetComponent<Rigidbody>().useGravity = true;
            blobToUnstack.transform.SetParent(null);

        }


        if (isTop)
        {
            //blobToUnstack.transform.position += new Vector3(0, 2, 0);
            transform.position -= new Vector3(0, 1, 0);
            Destroy(blobToUnstack);

            for (int i = 0; i < gm.collectedBlobs.Count; i++)
            {
                gm.collectedBlobs[i].transform.position += new Vector3(0, 1, 0);
            }
        }

        audioSource.PlayOneShot(releaseSound);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isGrounded && hit.gameObject.layer != 10)
        {
            float velocityReduction = Vector3.Dot(velocity, hit.normal);
            velocity = velocity - velocityReduction * hit.normal;
        }
        else
        {
            velocity = Vector3.up * velocity.y;
        }
    }

    void PlayScream() 
    {
        audioSource.PlayOneShot(SlimeScream);
    }

}

