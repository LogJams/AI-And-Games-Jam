using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;

using math = Unity.Mathematics.math;
using random = Unity.Mathematics.Random;


public class FishGenerator : MonoBehaviour {

    // 1
    private NativeArray<Vector3> velocities;

    // 2
    private TransformAccessArray transformAccessArray;


    public int amountOfFish = 10000;

    public Vector3 spawnBounds = new Vector3(100, 0, 100);


    public Transform objectPrefab;

    public float spawnHeight = 0f;

    private PositionUpdateJob positionUpdateJob;

    private JobHandle positionUpdateJobHandle;


    public float swimSpeed;
    public float turnSpeed;
    public int swimChangeFrequency;


    // Start is called before the first frame update
    void Start() {
        // 1
        velocities = new NativeArray<Vector3>(amountOfFish, Allocator.Persistent);

        // 2
        transformAccessArray = new TransformAccessArray(amountOfFish);

        for (int i = 0; i < amountOfFish; i++) {

            float distanceX =
            Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);

            float distanceZ =
            Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);

            // 3
            Vector3 spawnPoint =
            (transform.position + Vector3.up * spawnHeight) + new Vector3(distanceX, 0, distanceZ);

            // 4
            Transform t = (Transform)Instantiate(objectPrefab, spawnPoint, Quaternion.identity);

            // 5
            transformAccessArray.Add(t);
        }
    }

    // Update is called once per frame
    void Update() {
        positionUpdateJob = new PositionUpdateJob() {
            velocities = velocities,
            jobDeltaTime = Time.deltaTime,
            swimSpeed = this.swimSpeed,
            turnSpeed = this.turnSpeed,
            time = Time.time,
            swimChangeFrequency = this.swimChangeFrequency,
            center = Vector3.zero,
            bounds = spawnBounds,
            seed = System.DateTimeOffset.Now.Millisecond
        };

        // 2
        positionUpdateJobHandle = positionUpdateJob.Schedule(transformAccessArray);
    }


    private void LateUpdate() {
        positionUpdateJobHandle.Complete();
    }


    private void OnDestroy() {
        velocities.Dispose();
        transformAccessArray.Dispose();
    }




    [BurstCompile]
    struct PositionUpdateJob : IJobParallelForTransform {
        public NativeArray<Vector3> velocities;

        public Vector3 bounds;
        public Vector3 center;

        public float jobDeltaTime;
        public float time;
        public float swimSpeed;
        public float turnSpeed;
        public int swimChangeFrequency;

        public float seed;

        public void Execute(int i, TransformAccess transform) {
            Vector3 currentVelocity = velocities[i];

            // 2            
            random randomGen = new random((uint)(i * time + 1 + seed));

            // 3
            transform.position +=
            transform.localToWorldMatrix.MultiplyVector(new Vector3(0, 0, 1)) *
            swimSpeed *
            jobDeltaTime *
            randomGen.NextFloat(0.3f, 1.0f);

            // 4
            if (currentVelocity != Vector3.zero) {
                transform.rotation =
                Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(currentVelocity), turnSpeed * jobDeltaTime);
            }


            Vector3 currentPosition = transform.position;

            bool randomise = true;

            // 1
            if (currentPosition.x > center.x + bounds.x / 2 ||
                currentPosition.x < center.x - bounds.x / 2 ||
                currentPosition.z > center.z + bounds.z / 2 ||
                currentPosition.z < center.z - bounds.z / 2) {
                Vector3 internalPosition = new Vector3(center.x +
                randomGen.NextFloat(-bounds.x / 2, bounds.x / 2) / 1.3f,
                0,
                center.z + randomGen.NextFloat(-bounds.z / 2, bounds.z / 2) / 1.3f);

                currentVelocity = (internalPosition - currentPosition).normalized;

                velocities[i] = currentVelocity;

                transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(currentVelocity),
                turnSpeed * jobDeltaTime * 2);

                randomise = false;
            }

            // 2
            if (randomise) {
                if (randomGen.NextInt(0, swimChangeFrequency) <= 2) {
                    velocities[i] = new Vector3(randomGen.NextFloat(-1f, 1f),
                    0, randomGen.NextFloat(-1f, 1f));
                }
            }
        }
    }

}
