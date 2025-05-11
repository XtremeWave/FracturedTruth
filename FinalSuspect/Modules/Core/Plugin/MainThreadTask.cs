using System;
using System.Collections.Generic;
using System.Linq;

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

        if (name != "")
            Info("\"" + name + "\" is created", "Main Thread Task");
        Tasks.Add(this);
    }

    public static void Update()
    {
        var TasksToRemove = new List<MainThreadTask>();
        // 创建原集合的副本用于遍历
        foreach (var task in Tasks.ToList()) // 关键修复：.ToList() 创建副本
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
                Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\"\n{ex.StackTrace}", "Main Thread Task.Error",
                    false);
                TasksToRemove.Add(task);
            }
        }

        // 安全移除已处理的任务
        TasksToRemove.ForEach(task => Tasks.Remove(task));
    }
}