using System.Collections.Generic;
using UnityEngine;

public class SystemBase : MonoBehaviour
{
    public List<WorkerController> Workers = new List<WorkerController>();

    [SerializeField]
    private Restaurant Owner;

    public virtual bool HasJob => false;

    public virtual void AddWorker(WorkerController worker)
    {
        Workers.Add(worker);
        worker.CurrentSystem = this;
    }

    public virtual void RemoveWorker(WorkerController worker)
    {
        Workers.Remove(worker);
        worker.StopCoroutine(worker.WorkerJob);
        worker.WorkerJob = null;
        worker.CurrentSystem = null;
    }
}
