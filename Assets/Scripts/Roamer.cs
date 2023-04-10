/*  File:       Roamer
    Purpose:    Holds the methods many objects use to randomly move around the game world. -cb Spring 2022
    Functions compiled and made generic from ATPpathfinding.
    Therefor, please see APTPathfinding.cs for the previous update log of the existing funtions.
*/

using UnityEngine;
using System;
using System.Collections;


public class Roamer
{
    public  float _speed = 5.0f;

    //------------------------------------------------------------------------------------------------
    #region Public Fields + Properties + Events + Delegates + Enums
    public  float maxHeadingChange = 80;              // max possible rotation angle at a time
    public  int maxRoamChangeTime = 60;               // how long before changing heading/speed
    public  int minSpeed = 2;                        // slowest the object will move
    public  int maxSpeed = 4;                        // fastest the object will move
    public float angleToRotate;                 // stores the angle in degrees between ATP and dock

    #endregion Public Fields + Properties + Events + Delegates + Enums
    //------------------------------------------------------------------------------------------------

    //------------------------------------------------------------------------------------------------
    #region Private Fields + Properties + Events + Delegates + Enums
    // private  int objIndex = 0;                   // the index containing the above "trackThis" object
    private Transform origin;
    private Rigidbody rigidbody;
    private float heading = 180;                      // roaming direction
    private  float headingOffset;                // used for smooth rotation while roaming
    private  int movementSpeed;                  // roaming velocity
    private  int roamInterval = 0;               // how long until heading/speed change while roaming
    private  int roamCounter = 0;                // time since last heading speed change while roaming
    private  int curveCounter = 90;              // used for smooth transition when tracking
    private GameObject trackThis;                // the object with which to dock
    private  Quaternion rotate;                  // rotation while tracking
    
    #endregion Private Fields + Properties + Events + Delegates + Enums
    //------------------------------------------------------------------------------------------------
    #region ClassConstructors
    public Roamer()
    {
        //vars already set above
    }
        public Roamer(int mins = 3, int maxs = 4, float mhc = 90 )
    {
        maxHeadingChange = mhc;
        minSpeed = mins;
        maxSpeed = maxs;
    }
    public Roamer(int mins, int maxs)
    {
        minSpeed = mins;
        maxSpeed = maxs;
    }

    #endregion ClassConstructors

    public void SlowSpeed()
    {
        maxSpeed = minSpeed;
    }



    //------------------------------------------------------------------------------------------------
    // Obj should use this to wander around when it has no vaid targets. This method causes the Obj to randomly
    // change direction and speed at random intervals.  The tendency for purely random motion objects
    // to generally gravitate toward the edges of a circular container has been artificially remedied
    // by Raycasting and turning the Obj onto a 180 degree course (directing them toward the center).
    //Previously only used in ATP, now generic for all objects that roam in a brownian motion type way - cb Spring2022
    public void Roaming(GameObject Obj)  
    {
        rigidbody = Obj.GetComponent<Rigidbody>();
        if (Time.timeScale != 0)// if game not paused
        {
            roamCounter++;
            RaycastHit2D collision = Physics2D.Raycast(Obj.transform.position, Obj.transform.up);
            //Debug.DrawRay(Obj.transform.position, Obj.transform.up * 6f, Color.red);
            if (roamCounter > roamInterval) //where the movement amounts are calculated every second or so
            {
                roamCounter = 0;
                var floor = Mathf.Clamp(heading - maxHeadingChange, 0, 360);
                var ceiling = Mathf.Clamp(heading + maxHeadingChange, 0, 360);
                roamInterval = UnityEngine.Random.Range(5, maxRoamChangeTime);  
                movementSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);
                if (collision.collider != null && (collision.collider.name == "Cell Membrane" || collision.collider.name == "Cell Membrane(clone)" ) &&
                   collision.distance < 6) //if it's too close to the cell wall, then turn around, and go maxSpeed and for the longest amount of time to get further away
                {
                    if (heading <= 180) //reset heading to a better direction
                        heading = heading + 160;
                    else
                        heading = heading - 160;
                    //Debug.Log("soft turn " + heading.ToString() );

                    movementSpeed = maxSpeed;
                    roamInterval = maxRoamChangeTime;

                }
                else {
                    heading = UnityEngine.Random.Range(floor, ceiling);
                }

                headingOffset = (Obj.transform.eulerAngles.z - heading) / (float)roamInterval;
            }
            
            //where the movement is applied:
            Obj.transform.eulerAngles = new Vector3(0, 0, Obj.transform.eulerAngles.z - headingOffset);
            //Obj.transform.position += Obj.transform.up * Time.deltaTime * movementSpeed;
            Obj.GetComponent<Rigidbody2D>().AddForce(Obj.transform.up * movementSpeed * 2);


        }
    }
    //------------------------------------------------------------------------------------------------
    // Directs the Obj to the proper dock. The Obj seeks after the circle(or other, check trackcollider) 
    // collider of the "trackThis" object, which should be projected to the side of the object. This 
    // method will detect whether or not the "Inner Cell Wall" is in the Obj's line of sight with the
    // collider. If it is, a path will be plotted around it. The incident angle is also calculated 
    // ("angleToRotate") in order to give the "dropOff" function a baseline angle to use for rotation.
    //Previously only used in ATP, now generic for all objects that aproach other objects - cb Spring2022
    //changed to RETURN angletorotate instead of just setting it for the class.
    public float moveToDock(GameObject Obj, GameObject dock)
    {
        angleToRotate = 0;
        origin = Obj.transform;
        trackThis = dock;

        Vector3 trackCollider = Vector3.zero;
        if (trackThis.GetComponent<CircleCollider2D>() != null)
        {
            trackCollider = trackThis.GetComponent<CircleCollider2D>().bounds.center;
        } else if (trackThis.GetComponent<BoxCollider2D>() != null)
        {
            trackCollider = trackThis.GetComponent<BoxCollider2D>().bounds.center;
        } else if(trackThis.GetComponent<PolygonCollider2D>() != null)
        {
            trackCollider = trackThis.GetComponent<PolygonCollider2D>().bounds.center;
        } else if (trackThis.GetComponent<EdgeCollider2D>() != null)
        {
            trackCollider = trackThis.GetComponent<EdgeCollider2D>().bounds.center;
        }

        RaycastHit2D collision = Physics2D.Linecast(origin.position, trackCollider);

        if (collision != null && collision.collider.name == "Inner Cell Wall")
        {
            Vector3 collisionAngle = collision.normal;
            Vector3 direction = trackCollider - origin.position;
            Vector3 angle = Vector3.Cross(direction, collisionAngle);

            if (angle.z < 0)// track to the right of the nucleus
            {
                rotate = Quaternion.LookRotation(origin.position - trackCollider, trackThis.transform.right);
                curveCounter = 90;
            }
            else//track to the left of the nucleus
            {
                rotate = Quaternion.LookRotation(origin.position - trackCollider, -trackThis.transform.right);
                curveCounter = -90;
            }
        }
        else// calculate approach vector
        {
            float diffX = origin.position.x - trackCollider.x;
            float diffY = origin.position.y - trackCollider.y;
            float degrees = ((float)Math.Atan2(diffY, diffX) * (180 / (float)Math.PI) + 90);
            Obj.transform.eulerAngles = new Vector3(0, 0, degrees - curveCounter);
            rotate = Obj.transform.localRotation;
            if (curveCounter > 0)
                curveCounter -= 1;// slowly rotate left until counter empty
            else if (curveCounter < 0)
                curveCounter += 1;// slowly rotate right until counter empty
        }
        Obj.transform.localRotation = new Quaternion(0, 0, rotate.z, rotate.w);
        Obj.transform.position += Obj.transform.up * Time.deltaTime * (maxSpeed+2);
        //Obj.GetComponent<Rigidbody2D>().AddForce(Obj.transform.up * movementSpeed * 4);

        angleToRotate = Vector3.Angle(trackThis.transform.up, Obj.transform.up);
        Vector3 crossProduct = Vector3.Cross(trackThis.transform.up, Obj.transform.up);
        if (crossProduct.z < 0)
            angleToRotate = -angleToRotate; // .Angle always returns a positive #
        return angleToRotate;
    }


    //old Roam functions, left because still in use by other classes. -cb Spring2022
    public interface CollectObject //honestly no idea what this was for. Leaving it in. -cb Spring2022
    {
        void GetObject(GameObject obj, string newTag);
    }

    public bool ApproachMidpoint(GameObject obj1, GameObject obj2, bool[] midpointAchieved, Vector3 midpoint, Vector3 Offset, float Restraint)
    {
        if (!midpointAchieved[0])
        {
            midpointAchieved[0] = ApproachVector(obj1, midpoint, Offset, Restraint);
        }

        if (!midpointAchieved[1])
        {
            midpointAchieved[1] = ApproachVector(obj2, midpoint, -1 * Offset, Restraint);
        }
        return (midpointAchieved[0] && midpointAchieved[1]);
    }

    public bool ApproachVector(GameObject obj, Vector3 destination, Vector3 offset, float restraint)
    {
        if(Vector3.Distance(obj.transform.position, destination) > restraint)
        {
        //    Roaming(obj);
        }
        return ProceedToVector(obj, destination + offset);
    }

    public bool ProceedToVector(GameObject obj, Vector3 approachVector)
    {
        float step = _speed * Time.deltaTime;
        obj.transform.position = Vector3.MoveTowards(obj.transform.position, approachVector, step);
        return (approachVector == obj.transform.position);
    }

}