/*using System;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

public static class HarmonyCleaner
{
    public static void UnpatchAllHarmonyInstances(Assembly targetAssembly)
    {
        foreach (var type in targetAssembly.GetTypes())
        {
            ProcessStaticHarmonyMembers(type);
            ProcessInstanceHarmonyMembers(type);
        }
    }

    private static void ProcessStaticHarmonyMembers(Type type)
    {
        // 处理静态字段
        foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(Harmony))
            {
                _ = (Harmony)field.GetValue(null);
                //harmony?.UnpatchAll();
            }
        }

        // 处理静态属性
        foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.PropertyType == typeof(Harmony) && prop.CanRead)
            {
                _ = (Harmony)prop.GetValue(null);
                //harmony?.UnpatchAll();
            }
        }
    }

    private static void ProcessInstanceHarmonyMembers(Type type)
    {
        try
        {
            var instance = Activator.CreateInstance(type);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Harmony))
                {
                    var harmony = (Harmony)field.GetValue(instance);
                    //harmony?.UnpatchAll();
                }
            }

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.PropertyType == typeof(Harmony) && prop.CanRead)
                {
                    var harmony = (Harmony)prop.GetValue(instance);
                    //harmony?.UnpatchAll();
                }
            }
        }
        catch (MissingMethodException)
        {
            // 忽略无默认构造函数的类型
        }
    }
}*/

