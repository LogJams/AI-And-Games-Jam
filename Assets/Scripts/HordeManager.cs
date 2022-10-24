using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;

using math = Unity.Mathematics.math;
using random = Unity.Mathematics.Random;


public class HordeManager : MonoBehaviour {

    public static HordeManager instance;

    //transforms and velocities for each of the agents
    private TransformAccessArray transforms;
    private NativeArray<Vector3> position, velocity;
    private NativeArray<Vector3> nbPosition, nbVelocity;
    private NativeArray<float> timer;
    private NativeArray<bool> triggered;

    private NativeArray<Vector3> playerPosition;

    private Queue<int> triggerBuffer;
    private Queue<(int, Vector3)> triggerLocations;
    private Queue<int> unTriggerBuffer;

    private Queue<(int, Vector3)> moveZombieBuffer;



    [Header("Horde Parameters")]
    public int hordeSize = 10000;

    public Vector3 spawnBounds = new Vector3(100, 0, 100);
    public float spawnHeight = 0f;

    public Transform zombiePrefab;


    //zombie behaviors which are stored in job structs
    private DynamicsJob dynamicsJob;
    private JobHandle dynamicsHandle;

    private BehaviorJob behaviorJob;
    private JobHandle nbhdHandle;

    [Header("Zombie Behavior Settings")]
    public float moveSpeed = 1.0f;
    public float moveTime = 2.5f;
    public float idleTime = 5.5f;

    public float interactionDistance = 10f;
    public float diskSize = 5.0f;
    public float wanderChance = 0.3f;
    public float restitution = 0.8f;


    [Header("Player Reference")]
    public Transform player; 
    public float playerTrackDistance = 5f;

    Vector3 centroid;


    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this);
            return;
        }
        instance = this;

        centroid = new Vector3();
    }

    void Start() {

        position   = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        velocity   = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        nbPosition = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        nbVelocity = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);

        transforms = new TransformAccessArray(hordeSize);

        timer = new NativeArray<float>(hordeSize, Allocator.Persistent);

        triggered = new NativeArray<bool>(hordeSize, Allocator.Persistent);
        
        triggerBuffer = new Queue<int>();
        unTriggerBuffer = new Queue<int>();
        triggerLocations = new Queue<(int,Vector3)>();
        moveZombieBuffer = new Queue<(int, Vector3)>();

        playerPosition = new NativeArray<Vector3>(1,Allocator.Persistent);
        playerPosition[0] = player.position;

        for (int i = 0; i < hordeSize; i++) {

            float distanceX = Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);
            float distanceZ = Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);
            Vector3 spawnPoint = transform.position + Vector3.up * spawnHeight
                                    + new Vector3(distanceX, 0, distanceZ);

            Transform t = (Transform)Instantiate(zombiePrefab, spawnPoint, Quaternion.identity);
            t.GetComponent<Zombie>().index = i;
            t.parent = this.transform;

            transforms.Add(t);

            position[i] = transforms[i].position;
            velocity[i] = Vector3.zero;

            nbVelocity[i] = new Vector3(0, 0, 0);
            nbPosition[i] = new Vector3(0, 0, 0);

            timer[i] = Random.Range(0, moveTime + idleTime);
            triggered[i] = false;
        }


        //neighborhood update job
        behaviorJob = new BehaviorJob() {
            playerPosition = this.playerPosition,
            positions = position,
            velocities = velocity,
            interactionDistance = this.interactionDistance,
            avgVelocity = nbVelocity,
            avgPosition = nbPosition,
            triggered = this.triggered,
            playerTrackDistance = this.playerTrackDistance,
        };

        //agent dynamics job
        dynamicsJob = new DynamicsJob() {
            positions = position,
            velocities = velocity,
            moveSpeed = this.moveSpeed,
            moveTime = this.moveTime,
            idleTime = this.idleTime,
            bounds = spawnBounds,
            avgVelocity = nbVelocity,
            avgPosition = nbPosition,
            timer = this.timer,
            diskSize = this.diskSize,
            time = (uint)System.DateTime.UtcNow.Second,
            wanderChance = this.wanderChance,
            triggered = this.triggered,
        };


    }


    // Update is called once per frame
    void Update() {

        //update the player position nativearray before jobs start
        playerPosition[0] = player.position;

        //job 1: update all neighborhood info
        nbhdHandle = behaviorJob.Schedule(transforms);

        foreach (var p in position) {
            centroid += p;
        }
        centroid = centroid / hordeSize;

        //job 2: do the dynamics!
        dynamicsJob.deltaTime = Time.deltaTime;
        dynamicsJob.centroid = this.centroid;
        dynamicsHandle = dynamicsJob.Schedule(transforms, dependsOn: nbhdHandle);
    }


    private void LateUpdate() {
        dynamicsHandle.Complete();

        //manage the buffers that update nativearray info
        while (triggerBuffer.Count > 0) {
            int idx = triggerBuffer.Dequeue();
            triggered[idx] = true;
        }
        while (triggerLocations.Count > 0) {
            var (idx, pos) = triggerLocations.Dequeue();
            triggered[idx] = true;
            nbPosition[idx] = pos;
            nbVelocity[idx] = Vector3.zero;
        }
        while (unTriggerBuffer.Count > 0) {
            triggered[unTriggerBuffer.Dequeue()] = false;
        }
        while (moveZombieBuffer.Count > 0) {
            var (idx, pos) = moveZombieBuffer.Dequeue();
            triggered[idx] = false;
            transforms[idx].position = pos;
        }
    }

    public void OnZombieCollision(int idx) {
        //todo: handle this in a collision queue that's updated after dynamicsHandle is completed
        velocity[idx] = Vector3.zero;
    }

    public void OnZombieCollision(int idx, int other) {
        //did you know zombies stick together?
        Vector3 vel = (velocity[idx] + velocity[other]) / 2 * restitution;
        velocity[idx]   = vel;
        velocity[other] = vel;
    }

    public void ZombieTrigger(int idx, Vector3 location) {
        //todo: handle this in a trigger queue that's updated after dynamicsHandle is completed
        triggerLocations.Enqueue( (idx, location) );
    }

    public void ZombieUnTrigger(int idx) {
        //todo: handle this in a trigger queue that's updated after dynamicsHandle is completed
        unTriggerBuffer.Enqueue(idx);
    }

    public void ZombieDeath(int idx) {
        //move the zombie across the map
        //todo: randomly pick a location far from the player (or in a horde far from the player?)
        float distanceX = Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);
        float distanceZ = Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);
        Vector3 spawnPoint = transform.position + Vector3.up * spawnHeight
                                + new Vector3(distanceX, 0, distanceZ);
        moveZombieBuffer.Enqueue( (idx, spawnPoint ) );

    }

    private void OnDestroy() {
        position.Dispose();
        velocity.Dispose();
        transforms.Dispose();
        nbVelocity.Dispose();
        nbPosition.Dispose();
        timer.Dispose();
        triggered.Dispose();
        playerPosition.Dispose();
    }


    [BurstCompile]
    struct BehaviorJob : IJobParallelForTransform {
        public NativeArray<Vector3> avgVelocity;
        public NativeArray<Vector3> avgPosition;

        [ReadOnly] public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<Vector3> velocities;
        [ReadOnly] public NativeArray<Vector3> playerPosition;


        public NativeArray<bool> triggered;

        public float interactionDistance;
        public float playerTrackDistance;

        public void Execute(int i, TransformAccess transform) {
            Vector3 player = playerPosition[0];

            //check if the player is TRIGGERING us
            //track the player if we have LoS to them and they've triggered us
            if ((player - transform.position).sqrMagnitude <= playerTrackDistance * playerTrackDistance) {
                avgPosition[i] = player;
                triggered[i] = true;
            }


            //exit out if we've been triggered by something else
            if (triggered[i]) return;

            //read current position and velocity to calculate avg values
            avgPosition[i] = positions[i];
            avgVelocity[i] = velocities[i];
            int count = 1;
            for (int k = 0; k < positions.Length; k++) {
                if (i == k) continue;
                if ((positions[i] - positions[k]).sqrMagnitude <= interactionDistance*interactionDistance) {
                    avgPosition[i] += positions[k];
                    avgVelocity[i] += velocities[k];
                    count++;
                }
            }
            //take the average
            avgPosition[i] /= count;
            avgVelocity[i] /= count;
        }
    }

    [BurstCompile]
    struct DynamicsJob : IJobParallelForTransform {
        public NativeArray<Vector3> positions;
        public NativeArray<Vector3> velocities;
        public NativeArray<float> timer;

        [ReadOnly] public NativeArray<Vector3> avgVelocity;
        [ReadOnly] public NativeArray<Vector3> avgPosition;
        public NativeArray<bool> triggered;

        public Vector3 bounds;


        public uint time;
        public float deltaTime;
        public float moveSpeed;
        public float moveTime;
        public float idleTime;

        public float diskSize;
        public float wanderChance;

        public Vector3 centroid;




        public void Execute(int i, TransformAccess transform) {
            Vector3 currentVelocity = velocities[i];
            timer[i] += deltaTime;

            //we are moving!
            if ( (timer[i] > idleTime && timer[i] < idleTime + deltaTime) || triggered[i]) {
                Vector3 dp = avgPosition[i] - positions[i];
                Vector3 dv = avgVelocity[i] - velocities[i];



                Vector3 offset = Vector3.zero;

                var rng = new random(time + (uint)(10 * i));
                if (!triggered[i]) {
                    offset = new Vector3(rng.NextFloat() - 0.5f, 0, rng.NextFloat() - 0.5f).normalized;
                }


                //currentVelocity = (dp + dv * moveTime + offset * diskSize / 2).normalized * moveSpeed;
                currentVelocity = (dp + offset*diskSize/2).normalized * moveSpeed;

                if (currentVelocity.sqrMagnitude > 0) {
                    transform.rotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
                }


                //never wander randomly if we're triggered
                if (!triggered[i] && (dp.sqrMagnitude <= diskSize*diskSize || rng.NextFloat() < wanderChance) ) {
                    //currentVelocity = -centroid; //reflect centroid and go there;
                    //currentVelocity = currentVelocity.normalized * moveSpeed;
                    currentVelocity = new Vector3(0, 0, moveSpeed) * math.min(1, dp.sqrMagnitude / diskSize / diskSize);
                    currentVelocity = Quaternion.AngleAxis(rng.NextFloat() * 360, Vector3.up) * currentVelocity;
                }
            }

            //we finished moving, do nothing for a while
            if (timer[i] > idleTime + moveTime) {
                currentVelocity = Vector3.zero;
                timer[i] -= idleTime + moveTime;
                triggered[i] = false;
            }


            Vector3 currentPosition = transform.position;

            if (currentPosition.x > bounds.x / 2 || currentPosition.x < -bounds.x / 2
                ||currentPosition.z > bounds.z / 2 || currentPosition.z < -bounds.z / 2) {

                currentVelocity = -currentPosition.normalized * moveSpeed;

            }


            //update our saved position and velocity values
            transform.position = currentPosition + currentVelocity * deltaTime;
            positions[i]       = transform.position;
            velocities[i] = currentVelocity;

        }
    }


}
