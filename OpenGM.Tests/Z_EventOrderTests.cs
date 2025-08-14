using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using UndertaleModLib.Models;

namespace OpenGM.Tests;

[TestClass]
public class Z_EventOrderTests
{
    private static int codeIndex = 0;

    private List<string> events = new();

    private VMCode GetDebugMessageCode(string message)
    {
        var asm = $"""
                   :[0]
                   push.v self.id
                   push.s ":{message}"@0000
                   conv.s.v
                   add.v.v
                   call.i show_debug_message(argc=1)
                   popz.v
                   """;
        var code = GameConverter.ConvertAssembly(asm);
        code.OnCodeExecuted += () => events.Add($"{VMExecutor.Self.GMSelf.instanceId}:{message}");
        GameLoader.Codes.Add(codeIndex++, code);
        return code;
    }

    private int GetDebugMessageCodeID(string message)
    {
        var asm = $"""
                   :[0]
                   push.v self.id
                   push.s ":{message}"@0000
                   conv.s.v
                   add.v.v
                   call.i show_debug_message(argc=1)
                   popz.v
                   """;
        var code = GameConverter.ConvertAssembly(asm);
        code.OnCodeExecuted += () => events.Add($"{VMExecutor.Self.GMSelf.instanceId}:{message}");
        GameLoader.Codes.Add(codeIndex, code);
        return codeIndex++;
    }

    [TestMethod]
    public void TestRoomLoading()
    {
        GameLoader.Codes.Clear();

        var createCode = GetDebugMessageCode("Create");
        createCode.OnCodeExecuted += () => VMExecutor.Self.GMSelf.alarm[0] = 1;

        var objDef = new ObjectDefinition()
        {
            AssetId = 0,
            Name = "test object",
            CreateCode = createCode,
            PreCreateScript = GetDebugMessageCode("PreCreate"),
            OtherScript = new Dictionary<EventSubtypeOther,VMCode>()
            {
                { EventSubtypeOther.RoomStart, GetDebugMessageCode("Room Start") },
                { EventSubtypeOther.GameStart, GetDebugMessageCode("Game Start") }
            },
            StepScript = new Dictionary<EventSubtypeStep, VMCode>()
            {
                { EventSubtypeStep.BeginStep, GetDebugMessageCode("Begin Step") },
                { EventSubtypeStep.Step, GetDebugMessageCode("Step") },
                { EventSubtypeStep.EndStep, GetDebugMessageCode("End Step") }
            },
            DrawScript = new Dictionary<EventSubtypeDraw, VMCode>()
            {
                { EventSubtypeDraw.Draw, GetDebugMessageCode("Draw") },
                { EventSubtypeDraw.DrawBegin, GetDebugMessageCode("Draw Begin") },
                { EventSubtypeDraw.DrawEnd, GetDebugMessageCode("Draw End") },
                { EventSubtypeDraw.PreDraw, GetDebugMessageCode("Pre Draw") },
                { EventSubtypeDraw.PostDraw, GetDebugMessageCode("Post Draw") },
            },
            AlarmScript = new Dictionary<int, VMCode>()
            {
                { 0, GetDebugMessageCode("Alarm 0")}
            }
        };

        InstanceManager.ObjectDefinitions.Add(0, objDef);

        var obj1 = new GameObject()
        {
            Type = ElementType.Instance,
            Id = 0,
            InstanceID = GMConstants.FIRST_INSTANCE_ID,
            DefinitionID = 0,
            PreCreateCodeID = GetDebugMessageCodeID("PreCreation Code"),
            CreationCodeID = GetDebugMessageCodeID("Creation Code")
        };

        var obj2 = new GameObject()
        {
            Type = ElementType.Instance,
            Id = 0,
            InstanceID = GMConstants.FIRST_INSTANCE_ID + 1,
            DefinitionID = 0,
            PreCreateCodeID = GetDebugMessageCodeID("PreCreation Code"),
            CreationCodeID = GetDebugMessageCodeID("Creation Code")
        };

        var room = new Room()
        {
            AssetId = 0,
            Name = "room_test",
            GameObjects = new List<GameObject>() { obj1, obj2 }
        };

        RoomManager.RoomList.Add(0, room);

        RoomManager.FirstRoom = true;
        RoomManager.New_Room = 0;
        RoomManager.ChangeToWaitingRoom();

        var expectedEventOrder = new List<string>
        {
            "100000:PreCreate",
            "100000:PreCreation Code",
            "100000:Create",
            "100000:Creation Code",

            "100001:PreCreate",
            "100001:PreCreation Code",
            "100001:Create",
            "100001:Creation Code",

            "100000:Game Start",
            "100001:Game Start",
            "100000:Room Start",
            "100001:Room Start"
        };

        Assert.AreEqual(expectedEventOrder.Count, events.Count, "Executed wrong number of events.");

        for (var i = 0; i < expectedEventOrder.Count; i++)
        {
            Assert.AreEqual(expectedEventOrder[i], events[i], $"Event {i + 1} was not correct.");
        }

        events.Clear();

        // do one step to call Step and Draw
        DrawManager.FixedUpdate();

        expectedEventOrder = new List<string>
        {
            "100000:Begin Step",
            "100001:Begin Step",
            "100000:Alarm 0",
            "100001:Alarm 0",
            "100000:Step",
            "100001:Step",
            "100000:End Step",
            "100001:End Step",

            "100000:Pre Draw",
            "100001:Pre Draw",
            "100000:Draw Begin",
            "100001:Draw Begin",
            "100000:Draw",
            "100001:Draw",
            "100000:Draw End",
            "100001:Draw End",
            "100000:Post Draw",
            "100001:Post Draw",
        };

        Assert.AreEqual(expectedEventOrder.Count, events.Count, "Executed wrong number of events.");

        for (var i = 0; i < expectedEventOrder.Count; i++)
        {
            Assert.AreEqual(expectedEventOrder[i], events[i], $"Event {i + 1} was not correct.");
        }
    }
}
