using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class StageLoader : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private string address = "stage-main";

    private AsyncOperationHandle<GameObject> handle;
    private bool ownerDestroyed;

    private void Start()
    {
        LoadStage();
    }

    public void LoadStage()
    {
        if (session.IsPlaying || session.IsLoading)
        {
            return;
        }

        session.SetBusy(true);
        session.SetStatus("Loading stage...");
        session.SetStage(null);

        ReleaseOwnedOperation();

        handle = Addressables.InstantiateAsync(address, Vector3.zero, Quaternion.identity);
        handle.Completed += OnCompleted;
    }

    private void OnCompleted(AsyncOperationHandle<GameObject> operation)
    {
        if (ownerDestroyed)
        {
            ReleaseOwnedOperation();
            return;
        }

        if (operation.Status != AsyncOperationStatus.Succeeded)
        {
            session.SetBusy(false);
            session.SetStatus("Stage load failed. Press Reload Stage.");
            Debug.LogError(operation.OperationException);
            ReleaseOwnedOperation();
            return;
        }

        CoinPickup[] coins = operation.Result.GetComponentsInChildren<CoinPickup>(true);
        if (coins.Length == 0)
        {
            ReleaseOwnedOperation();
            session.SetBusy(false);
            session.SetStatus("Stage has no coins. Check the prefab.");
            return;
        }

        session.SetStage(coins);
        session.SetBusy(false);
        session.SetStatus("Stage ready. Press Start.");
    }

    private void OnDestroy()
    {
        ownerDestroyed = true;
        
        if (handle.IsValid() && handle.IsDone)
        {
            ReleaseOwnedOperation();
        }
    }

    private void ReleaseOwnedOperation()
    {
        if (!handle.IsValid())
        {
            return;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.ReleaseInstance(handle);
        }
        else
        {
            Addressables.Release(handle);
        }

        handle = default;
    }
}
