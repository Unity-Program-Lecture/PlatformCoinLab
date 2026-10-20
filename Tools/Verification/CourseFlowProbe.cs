using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CourseFlowProbe
{
    [Serializable] public sealed class Report
    {
        public int day; public string unity; public string checkedUtc;
        public bool passed; public int framesAdvanced; public string[] checks; public string error;
    }
    private static readonly List<string> checks=new List<string>();
    private static object Field(object target,string name) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static object Call(object target,string method,params object[] args) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(target,args);
    private static bool Flag(object target,string name) => (bool)target.GetType().GetProperty(name).GetValue(target);
    private static void Assert(bool value,string message) { if(!value) throw new Exception(message); checks.Add(message); }
    private static async Task Wait(Func<bool> condition,int milliseconds=20000)
    {
        double end=EditorApplication.timeSinceStartup+milliseconds/1000d;
        while(!condition()) { if(EditorApplication.timeSinceStartup>end) throw new TimeoutException("Condition timed out"); await Task.Delay(50); }
    }
    private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
    private static Component FindComponent(string name) => (Component)UnityEngine.Object.FindAnyObjectByType(FindType(name));

    public static object Core()
    {
        var t=FindType("CoinRound"); var r=Activator.CreateInstance(t,new object[]{30f});
        checks.Clear();
        Assert((bool)Call(r,"TryCollect",1),"first ID accepted");
        Assert(!(bool)Call(r,"TryCollect",1),"duplicate ID rejected");
        Call(r,"Tick",31f); Assert(Flag(r,"IsFinished"),"time clamps to zero");
        Assert(!(bool)Call(r,"TryCollect",2),"finished round rejects coins");
        Call(r,"Reset",30f); Assert((int)t.GetProperty("Score").GetValue(r)==0,"reset clears score");
        Assert((bool)Call(r,"TryCollect",1),"reset clears IDs");
        try { Call(r,"Tick",-1f); throw new Exception("negative time accepted"); } catch(TargetInvocationException e) { Assert(e.InnerException is ArgumentOutOfRangeException,"negative delta rejected"); }
        try { Call(r,"Reset",float.NaN); throw new Exception("NaN accepted"); } catch(TargetInvocationException e) { Assert(e.InnerException is ArgumentOutOfRangeException,"NaN duration rejected"); }
        var report=new Report{day=2,unity=Application.unityVersion,checkedUtc=DateTime.UtcNow.ToString("O"),passed=true,checks=checks.ToArray()};
        Directory.CreateDirectory("Evidence/Rebuild"); File.WriteAllText("Evidence/Rebuild/Day02-core.json",JsonUtility.ToJson(report,true)); return report;
    }
    public static async Task<object> Main(int day)
    {
        if(!EditorApplication.isPlaying) throw new Exception("Enter Play mode first.");
        checks.Clear(); int startFrame=Time.frameCount;
        var report=new Report{day=day,unity=Application.unityVersion,checkedUtc=DateTime.UtcNow.ToString("O")};
        var s=FindComponent("GameSession");
        var inputSettings=InputSystem.settings;
        var oldBackground=inputSettings.backgroundBehavior;
        var oldEditorBehavior=inputSettings.editorInputBehaviorInPlayMode;
        inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        inputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        try
        {
            await Wait(()=>Flag(s,"IsReady"));
            Assert(!Flag(s,"IsPlaying"),"menu waits for Start");
            Assert(!(bool)Call(s,"TryCollect",999),"menu rejects collect");
            Call(s,"SetRoundSeconds",10);
            var start=(UnityEngine.UI.Button)Field(s,"startButton"); start.onClick.Invoke();
            Assert(Flag(s,"IsPlaying"),"serialized Start button starts round");
            Assert(((GameObject)Field(s,"hudPanel")).activeSelf,"HUD visible during round");
            var coins=((Array)Field(s,"coins")).Cast<Component>().ToArray();
            var player=(Transform)Field(s,"player"); Vector3 origin=player.position;
            player.position=coins[0].transform.position;
            await Wait(()=>!coins[0].gameObject.activeSelf,3000);
            Assert((int)s.GetType().GetProperty("Score").GetValue(s)==1,"real physics trigger collects coin");
            Assert(!(bool)Call(s,"TryCollect",(int)Field(coins[0],"coinId")),"same coin ID cannot score twice");
            player.position=origin;
            if(day>=3)
            {
                // Exercise the scene-owned virtual keyboard before adding/removing test devices.
                var up=GameObject.Find("Up"); var pointer=new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(up,pointer,ExecuteEvents.pointerDownHandler); await Task.Delay(300);
                ExecuteEvents.Execute(up,pointer,ExecuteEvents.pointerUpHandler); await Task.Delay(70);
                Assert(player.position.z>origin.z+.1f,"on-screen direction button moves Player");
                player.position=origin;
                var keyboard=InputSystem.AddDevice<Keyboard>();
                try
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); await Task.Delay(300);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Task.Delay(70);
                    Assert(player.position.z>origin.z+.1f,"Input System keyboard moves Player");
                }
                finally { InputSystem.RemoveDevice(keyboard); }
                player.position=origin;
                var pad=InputSystem.AddDevice<Gamepad>();
                try
                {
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(1,0)}); await Task.Delay(300);
                    InputSystem.QueueStateEvent(pad,new GamepadState()); await Task.Delay(70);
                    Assert(player.position.x>origin.x+.1f,"Input System gamepad moves Player");
                }
                finally { InputSystem.RemoveDevice(pad); }
                player.position=origin;
            }
            if(day>=4)
            {
                Call(s,"OnApplicationPause",true); Assert(!Flag(s,"CanMove"),"simulated pause blocks movement");
                Call(s,"OnApplicationPause",false); Assert(Flag(s,"CanMove"),"simulated resume restores movement");
            }
            await Wait(()=>!Flag(s,"IsPlaying"),18000);
            Assert(((GameObject)Field(s,"resultPanel")).activeSelf,"real elapsed time shows result");
            Assert(!(bool)Call(s,"TryCollect",999),"timeout rejects collect");
            if(day>=4) Assert(PlayerPrefs.GetInt("PlatformCoinLab.Best")>=1,"best score persisted through PlayerPrefs API");
            var restart=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().First(b=>b.name=="RestartButton" && b.gameObject.scene.IsValid());
            restart.onClick.Invoke(); Assert((int)s.GetType().GetProperty("Score").GetValue(s)==0,"serialized Restart button resets score");
            Assert(coins.All(c=>c.gameObject.activeSelf),"restart reactivates coins");
            Call(s,"ReturnToMenu"); Assert(!Flag(s,"CanMove"),"menu blocks movement");
            if(day>=13)
            {
                var config=FindComponent("PlatformConfigLoader");
                Call(config,"ApplySettings"); await Wait(()=>!Flag(s,"IsLoading"));
                Assert((float)Field(s,"roundSeconds")==45,"real StreamingAssets request applies 45 seconds");
                var url=config.GetType().GetField("remoteUrl",BindingFlags.Instance|BindingFlags.NonPublic);
                url.SetValue(config,new Uri(Path.GetFullPath("Temp/missing-course-config.json")).AbsoluteUri);
                Call(config,"ApplySettings"); await Wait(()=>!Flag(s,"IsLoading"));
                Assert((float)Field(s,"roundSeconds")==30,"failed file request restores default 30 seconds");
                url.SetValue(config,"");
            }
            report.passed=true;
        }
        catch(Exception e) { report.error=e.ToString(); report.passed=false; }
        finally
        {
            inputSettings.backgroundBehavior=oldBackground;
            inputSettings.editorInputBehaviorInPlayMode=oldEditorBehavior;
            Call(s,"ReturnToMenu");
        }
        report.framesAdvanced=Time.frameCount-startFrame; report.checks=checks.ToArray();
        Directory.CreateDirectory("Evidence/Rebuild"); File.WriteAllText($"Evidence/Rebuild/Day{day:00}-play.json",JsonUtility.ToJson(report,true));
        if(!report.passed) throw new Exception(JsonUtility.ToJson(report));
        return report;
    }
}
