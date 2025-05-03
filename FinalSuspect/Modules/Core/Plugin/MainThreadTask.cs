using System;
using System.Collections.Generic;

namespace FinalSuspect.Modules.Core.Plugin;

public class MainThreadTask
{
    private readonly string name;
    private readonly Action action;
    private static readonly List<MainThreadTask> Tasks = [];
    
    public MainThreadTask(Action action, string name = "No Name Task")
    {
        this.action = action;
        this.name = name;
        Tasks.Add(this);
        if (name != "")
            Info("\"" + name + "\" is created", "Main Thread Task");
    }
    
    public static void Update()
    {
        var TasksToRemove = new List<MainThreadTask>();
        foreach (var task in Tasks)
        {
            try
            {
                task.action();
                if (task.name != "")
                    Info($"\"{task.name}\" is finished", "Main Thread Task");
                TasksToRemove.Add(task);
            }
            catch (Exception ex)
            {
                Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\"\n{ex.StackTrace}", "Main Thread Task.Error", false);
                TasksToRemove.Add(task);
            }
        }
        
        TasksToRemove.ForEach(task => Tasks.Remove(task));
    }
}
