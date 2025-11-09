using UnityEngine;
using Unity.Jobs;
using Unity.Collections; // Required for NativeArray


public struct MyParallelJob : IJob
{
    [ReadOnly]
    public NativeArray<float> a;
    [ReadOnly]
    public NativeArray<float> b;
    public NativeArray<float> result;

    public void Execute(int i)
    {
        result[i] = a[i] + b[i];
    }
}


public class JobController : MonoBehaviour
{
    
    void Start()
    {
        NativeArray<float> a = new NativeArray<float>(2, Allocator.TempJob);

        NativeArray<float> b = new NativeArray<float>(2, Allocator.TempJob);

        NativeArray<float> result = new NativeArray<float>(2, Allocator.TempJob);

        a[0] = 1.1f;
        b[0] = 2.2f;
        a[1] = 3.3f;
        b[1] = 4.4f;

        MyParallelJob jobData = new MyParallelJob();
        jobData.a = a;
        jobData.b = b;
        jobData.result = result;


        MyParallelJob firstJob = new MyParallelJob();
        MyParallelJob secondJob = new MyParallelJob();


        JobHandle firstJobHandle = firstJob.Schedule();
        secondJob.Schedule(firstJobHandle);


        // Schedule the job with one Execute per index in the results array
        // only 1 item per processing batch
        JobHandle handle = jobData.Schedule(result.Length, 1);

        // Wait for the job to complete
        handle.Complete();

        // Free the memory allocated by the arrays
        a.Dispose();
        b.Dispose();
        result.Dispose();
    }
}