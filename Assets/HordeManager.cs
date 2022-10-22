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
    private NativeArray<int> numNeighbors;

    public int hordeSize = 10000;

    public Vector3 spawnBounds = new Vector3(100, 0, 100);
    public float spawnHeight = 0f;


    public Transform zombiePrefab;


    //zombie behaviors which are stored in job structs
    private DynamicsJob dynamicsJob;
    private JobHandle dynamicsHandle;

    private NbhdJob nbhdJob;
    private JobHandle nbhdHandle;


    public float moveSpeed = 1.0f;
    public float moveTime = 2.5f;
    public float idleTime = 5.5f;

    public float interactionDistance = 10f;
    public float diskSize = 5.0f;
    public float wanderChance = 0.3f;
    public int neighborLimit = 6;

    Vector3 centroid;

    public float TimeScale = 1f;

    public float restitution = 0.8f;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this);
            return;
        }
        instance = this;

        centroid = new Vector3();
    }

    void Start() {

        Time.timeScale = TimeScale;

        position   = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        velocity   = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        nbPosition = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);
        nbVelocity = new NativeArray<Vector3>(hordeSize, Allocator.Persistent);

        transforms = new TransformAccessArray(hordeSize);

        timer = new NativeArray<float>(hordeSize, Allocator.Persistent);

        numNeighbors = new NativeArray<int>(hordeSize, Allocator.Persistent);

        for (int i = 0; i < hordeSize; i++) {

            float distanceX = Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);
            float distanceZ = Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);
            Vector3 spawnPoint = transform.position + Vector3.up * spawnHeight
                                    + new Vector3(distanceX, 0, distanceZ);

            Transform t = (Transform)Instantiate(zombiePrefab, spawnPoint, Quaternion.identity);
            t.GetComponent<Zombie>().index = i;

            transforms.Add(t);

            position[i] = transforms[i].position;
            velocity[i] = Vector3.zero;

            nbVelocity[i] = new Vector3(0, 0, 0);
            nbPosition[i] = new Vector3(0, 0, 0);

            timer[i] = Random.Range(0, moveTime + idleTime);
            numNeighbors[i] = 0;
        }


        //neighborhood update job
        nbhdJob = new NbhdJob() {
            positions = position,
            velocities = velocity,
            interactionDistance = this.interactionDistance,
            avgVelocity = nbVelocity,
            avgPosition = nbPosition,
            numNeighbors = this.numNeighbors,
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
            neighborLimit = this.neighborLimit,
            numNeighbors = this.numNeighbors,
        };


    }


    // Update is called once per frame
    void Update() {

        //job 1: update all neighborhood info
        nbhdHandle = nbhdJob.Schedule(transforms);

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

    private void OnDestroy() {
        position.Dispose();
        velocity.Dispose();
        transforms.Dispose();
        nbVelocity.Dispose();
        nbPosition.Dispose();
        timer.Dispose();
        numNeighbors.Dispose();
    }


    [BurstCompile]
    struct NbhdJob : IJobParallelForTransform {
        public NativeArray<Vector3> avgVelocity;
        public NativeArray<Vector3> avgPosition;

        [ReadOnly] public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<Vector3> velocities;

        public NativeArray<int> numNeighbors;

        public float interactionDistance;

        public void Execute(int i, TransformAccess transform) {
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
            numNeighbors[i] = count - 1;
        }

    }


    [BurstCompile]
    struct DynamicsJob : IJobParallelForTransform {
        public NativeArray<Vector3> positions;
        public NativeArray<Vector3> velocities;

        [ReadOnly] public NativeArray<Vector3> avgVelocity;
        [ReadOnly] public NativeArray<Vector3> avgPosition;
        [ReadOnly] public NativeArray<int> numNeighbors;

        public Vector3 bounds;


        public uint time;
        public float deltaTime;
        public float moveSpeed;
        public float moveTime;
        public float idleTime;

        public float diskSize;
        public float wanderChance;
        public int neighborLimit;

        public Vector3 centroid;


        public NativeArray<float> timer;


        public void Execute(int i, TransformAccess transform) {
            Vector3 currentVelocity = velocities[i];
            timer[i] += deltaTime;

            //we are moving!
            if (timer[i] > idleTime && timer[i] < idleTime + deltaTime) {
                Vector3 dp = avgPosition[i] - positions[i];
                Vector3 dv = avgVelocity[i] - velocities[i];

                var rng = new random(time + (uint)(10*i));

                Vector3 offset = new Vector3(rng.NextFloat() - 0.5f, 0, rng.NextFloat() - 0.5f).normalized;

                currentVelocity = (dp + dv*moveTime + offset*diskSize/2).normalized * moveSpeed;

                if (currentVelocity.sqrMagnitude > 0) {
                    transform.rotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
                }



                if (dp.sqrMagnitude <= diskSize*diskSize || rng.NextFloat() < wanderChance || numNeighbors[i] > neighborLimit) {
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
