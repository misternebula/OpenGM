using OpenGM.VirtualMachine.BuiltInFunctions;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

public static class AsyncManager
{
    public static int NextAsyncId = 0;
    public static int AsyncLoadDsIndex = -1;

    public static Queue<AsyncResult> AsyncResults = new();

    public static void TriggerAsyncResult(AsyncResult result)
    {
        if (AsyncLoadDsIndex == -1)
        {
            AsyncLoadDsIndex = (int)DataStructuresFunctions.ds_map_create();
        }

        // TODO: no way to get data structures from outside the class, so doing this for now
        DataStructuresFunctions.ds_map_clear([AsyncLoadDsIndex]);
        DataStructuresFunctions.ds_map_add([AsyncLoadDsIndex, "id", result.Id]);
        DataStructuresFunctions.ds_map_add([AsyncLoadDsIndex, "status", result.Status]);

        GamemakerObject.ExecuteEvent(result.Instance, result.Instance.Definition, EventType.Other, (int)EventSubtypeOther.AsyncSaveAndLoad);
    }

    public static void HandleAsyncQueue()
    {
        while (AsyncResults.TryDequeue(out var result))
        {
            TriggerAsyncResult(result);
        }
    }
}

public class AsyncResult
{
    public int Id;
    public bool Status;

    public GamemakerObject Instance;

    public AsyncResult(int id, bool status, GamemakerObject instance)
    {
        this.Id = id;
        this.Status = status;
        this.Instance = instance;
    }
}