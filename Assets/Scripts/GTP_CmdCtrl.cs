﻿/*  File:       GTP_CmdCtrl
    Purpose:    This file handles the movement of a GTP. A GTP will seek out
                a G-Protein in level one and intro levels. It will seek out
                a Trimeric G-Protein Alpha subunit in level 2. After being
                attached to an Alpha subunit for a period of time, it hydrolizes
                and self-destructs.
*/

using UnityEngine;
using System.Collections; 
using System;//for math

public class GTP_CmdCtrl: MonoBehaviour
{
    //------------------------------------------------------------------------------------------------
    #region Public Fields + Properties + Events + Delegates + Enums
    public ParticleSystem destructionEffect; //'poof' special effect for 'expended' GDP
    public GameObject GTP1;                // transform GTP upon docking
    public GameObject trackThis;           // the object with which to dock
    public Quaternion rotation;
    public Transform  origin;              // origin location/rotation is the physical GTP
    public string     trackingTag;         // objects of this tag are searched for and tracked
    public float      maxHeadingChange;    // max possible rotation angle at a time
    public float      angleToRotate;       // stores the angle in degrees between GTP and dock
    public bool       droppedOff = false;  // is phospate gone?
    public int        minSpeed;            // slowest the GTP will move
    public int        maxSpeed;            // fastest the GTP will move
    #endregion Public Fields + Properties + Events + Delegates + Enums
    //------------------------------------------------------------------------------------------------
    
    //------------------------------------------------------------------------------------------------
    #region Private Fields + Properties + Events + Delegates + Enums
    private float      heading;          // roaming direction
    private float      headingOffset;    // used for smooth rotation while roaming
    private int        movementSpeed;    // roaming velocity
    private int        roamInterval = 0; // how long until heading/speed change while roaming
    private int        roamCounter  = 0; // time since last heading speed change while roaming
    private int        curveCounter = 90;// used for smooth transition when tracking
    private Quaternion rotate;           // rotation while tracking
    private bool       doOnce      = true;
    #endregion Private Fields + Properties + Events + Delegates + Enums

    //------------------------------------------------------------------------------------------------
    static float _speed = 5f;   

    private bool docked    = false;     // GTP position = Docked G-protein position
    private bool targeting = false;     // is GTP targeting docked G-protein
    private int timerforlockon;         // has GTP been targeting for too long?
    private int timerforlockonMAX = 800; //when is the GTP never going to arrive because its stuck or behind a membrain

    private float delay = 0f;
    private float deltaDistance;        // measures distance traveled to see if GTP is stuck behind something
    //private float randomX, randomY;       // random number between MIN/MAX_X and MIN/MAX_Y
    
    private GameObject openTarget;      //  found docked g-protein
    public Transform myTarget;         // = openTarget.transform
    
    //private Vector2 randomDirection;  // new direction vector
    public Vector3 dockingPosition;    // myTarget position +/- offset
    private Vector3 lastPosition;

    private Roamer r;                             //an object that holds the values for the roaming (random movement) methods


    /*  Function:   Start()
        Purpose:    This function is called upon instantiation
    */
    private void Start()
    {
        lastPosition = transform.position;
        r = new Roamer(minSpeed, maxSpeed, maxHeadingChange);
    }

 

    /*  Function:   FixedUpdate()
        Purpose:    This function determines whether the GTP should roam around
                    or if there is something for it to seek our and bind with.
                    In level one, looks for a G-Protein that is docked with the
                    Recptor to go bind with. This it finds by looking for a
                    nearby GameObject with the tag "DockedG_Protein". In the
                    second level, looks for a T_GProtein's Alpha Subunit with
                    which to bind. This it finds by searching for a nearby Game
                    Object with the tag "tGProteinDock". Once an appropriate
                    Object is found, the GTP targets the object and tracks
                    to it until it collides with it.
                    This function also continuously checks the GTP's tag for
                    ReleasedGDP, which causes it to explode and be removed from
                    the game
    */
    public void FixedUpdate() 
    {
        GameObject obj       = null;
        GameObject objParent = null;

        if(Time.timeScale > 0)
        {

            if (!targeting && !docked)//Look for a target
            {
                r.Roaming(this.gameObject); //move randomly

                openTarget = BioRubeLibrary.FindRandom("DockedG_Protein");//level one, find a target
                if (null == openTarget)
                {
                    obj = BioRubeLibrary.FindRandom("tGProteinDock");//level 2 find a target
                    if (null != obj)
                    {
                        //get the TGProtien. Doc has parent alpha, which has parent TGProtein
                        objParent = obj.transform.parent.gameObject;
                        if (null != objParent && objParent.name == "alpha")
                            objParent = objParent.transform.parent.gameObject;

                        TGProteinProperties objProps = (TGProteinProperties)objParent.GetComponent("TGProteinProperties");
                        if (objProps.isActive)
                        {
                            openTarget = obj;
                        }
                    }
                }

                if (openTarget != null)
                {
                    myTarget = openTarget.transform;
                    dockingPosition = GetOffset();
                    LockOn();//call dibs
                             //  r.moveToDock(this.gameObject, openTarget);
                }
            }
            else if (!docked)
            {
                if ((delay += Time.deltaTime) < 3) //wait 3(from 5) seconds before proceeding to target becuse this gives time for the GDP to exit the TrimericGprotein before it starts targeting it
                    r.Roaming(this.gameObject);
                else
                {
                    //dockingPosition = GetOffset();
                    docked = r.ProceedToVector(this.gameObject, dockingPosition);
                    timerforlockon++;

                    if (timerforlockon > timerforlockonMAX && !docked) //if timer is high
                    {                                  //reset lockedon,opentarget?,timer, mytarget.tag
                        targeting = false;
                        if (openTarget != null)
                        {
                            openTarget = null;
                            myTarget.tag = "DockedG_Protein";
                        }
                        else
                        {
                            myTarget.tag = "tGProteinDock";
                            myTarget = null;
                        }
                        openTarget = null;
                        obj = null;
                        timerforlockon = 0;
                    }

                }
                if (docked)
                {
                    if (doOnce)
                    {
                        StartCoroutine( Cloak() );
                        doOnce = false;
                    }
                }
            }
            if(tag == "ReleasedGTP")
            {
                tag = "DyingGDP";
                StartCoroutine(ReleasingGTP());
                StartCoroutine(DestroyGTP()); //Destroy GDP
            }
        }
    }

    //attempting re-write of how GTP attaches to Trimeric G-Protein (ABG-ALL)
 //   private void OnTriggerEnter2D(Collider2D other) //Triggered from Unity when GTP's collider component hits another collider
//    {
 //       if(other.gameObject.name == "ABG-ALL(Clone)") //if that other collider is "ABG-ALL(Clone)", then:
//        {
//            docked = true;
 //           Cloak();
 //       }
//
 //   }

    /*  Function:   GetOffset() Vector3
        Purpose:    GetOffset determines whether a target is to the  left or right of the receptor
                    and based on the target's position, relative to the receptor, an offset is 
                    is figured into the docking position so the GTP will mate up with the
                    G-protein.
        Return:     the offset from the target, a smidge to the left or right
    */
    private Vector3 GetOffset()
    {
        Debug.Log("GettingOffset() to: " + myTarget.position.ToString());

        if(myTarget.childCount > 0)//if we have children dealing Level1 G-Protein
        {
            if (myTarget.GetChild(0).tag == "Left")
                return myTarget.position + new Vector3(-2.2f, 0.28f, 0);
            else if (myTarget.GetChild(2).name == "Transporter Side A")
            {
                Debug.Log("GettingOffset() of side A to: " + myTarget.position.ToString()+ myTarget.GetChild(2).position.ToString() + " = " + (myTarget.position + myTarget.GetChild(2).position).ToString() );
                return myTarget.position + myTarget.GetChild(2).localPosition + new Vector3(-1f,0,0);
            }
                
        }
        else
            return myTarget.position;//if no children, Level2 T-G-Protein, just get the target's pos
        return myTarget.position + new Vector3 (-2.2f, 0.28f, 0);
    }

    /*  Function:   LockOn()
        Purpose:    retags the target 'DockedG_Protein' to 'Target' so it
                    is overlooked in subsequent searches for 'DockedG_Protein's.  This
                    and the assigning of a 'dockingPosition' ensures only one GTP
                    will target an individual docked g-protein.
    */
    private void LockOn()
    {
        targeting    = true;
        myTarget.tag = "Target";
        timerforlockon = 0;   //sudo fix for issue where GTP is outside the cell wall and or stuck
    }

    //Cloak retags objects for future reference
    private IEnumerator Cloak()
    {
        transform.GetComponent<CircleCollider2D>().enabled = false;
        //transform.GetComponent<Rigidbody2D>().isKinematic = false;
        GetComponent<Rigidbody2D>().simulated = false;
        transform.GetComponent<Rigidbody2D>().velocity = Vector3.zero;  //added to stop the random slide that occasionally happens after docking

        //transform.position = dockingPosition;
        transform.parent   = myTarget;
        myTarget.tag       = "OccupiedG_Protein";
        transform.tag      = "DockedGTP";
        myTarget           = null;

        //determine if win condition has been reached
        if (GameObject.FindWithTag("Win_DockedGTP")) WinScenario.dropTag("Win_DockedGTP");

        yield return new WaitForSeconds(1.0f);
        //Debug.Log("Moving to -1f,.1f");
        transform.localPosition = new Vector3(-1f, 0.1f, 0);
    }

    public IEnumerator ReleasingGTP ()
    {
        GameObject parentObject = null;
        yield return new WaitForSeconds (3f);

        parentObject     = GameObject.FindGameObjectWithTag ("MainCamera");
        transform.parent = parentObject.transform;

        transform.GetComponent<Rigidbody2D> ().isKinematic  = false;
        transform.GetComponent<CircleCollider2D>().enabled = true;
    } 

    public IEnumerator DestroyGTP()
    {
        GameObject parentObject = GameObject.FindGameObjectWithTag ("MainCamera");

        yield return new WaitForSeconds (2f);
        ParticleSystem explosionEffect     = Instantiate(destructionEffect) as ParticleSystem;
        explosionEffect.transform.parent   = parentObject.transform;
        explosionEffect.transform.position = transform.position;
        explosionEffect.loop = false;
        explosionEffect.Play();
        Destroy(explosionEffect.gameObject, explosionEffect.duration);
        Destroy(gameObject);
    }

}