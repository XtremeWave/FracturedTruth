using System;
using System.Reflection;
using HarmonyLib;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

using System;
using System.Reflection;
using HarmonyLib;

public static class HarmonyCleaner
{
    public static void UnpatchAllHarmonyInstances(Assembly targetAssembly)
    {
        foreach (Type type in targetAssembly.GetTypes())
        {
            ProcessStaticHarmonyMembers(type);
            ProcessInstanceHarmonyMembers(type);
        }
    }

    private static void ProcessStaticHarmonyMembers(Type type)
    {
        // 处理静态字段
        foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(Harmony))
            {
                var harmony = (Harmony)field.GetValue(null);
                harmony?.UnpatchAll();
            }
        }

        // 处理静态属性
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.PropertyType == typeof(Harmony) && prop.CanRead)
            {
                var harmony = (Harmony)prop.GetValue(null);
                harmony?.UnpatchAll();
            }
        }
    }
    
    private static void ProcessInstanceHarmonyMembers(Type type)
    {
        try
        {
            object instance = Activator.CreateInstance(type);
            
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Harmony))
                {
                    var harmony = (Harmony)field.GetValue(instance);
                    harmony?.UnpatchAll();
                }
            }

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.PropertyType == typeof(Harmony) && prop.CanRead)
                {
                    var harmony = (Harmony)prop.GetValue(instance);
                    harmony?.UnpatchAll();
                }
            }
        }
        catch (MissingMethodException)
        {
            // 忽略无默认构造函数的类型
        }
    }
}