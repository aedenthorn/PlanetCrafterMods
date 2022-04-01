using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class AedenthornUtils
{
    public static bool CheckKeyDown(string value)
    {
        try
        {
            return Input.GetKeyDown(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
    public static bool CheckKeyUp(string value)
    {
        try
        {
            return Input.GetKeyUp(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
    public static bool CheckKeyHeld(string value, bool req = true)
    {
        try
        {
            return Input.GetKey(value.ToLower());
        }
        catch
        {
            return !req;
        }
    }

    public static void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static string GetAssetPath(object obj, bool create = false)
    {
        return GetAssetPath(obj.GetType().Namespace, create);
    }
    public static string GetAssetPath(string name, bool create = false)
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name);
        if (create && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
    public static string GetTransformPath(Transform t)
    {
        if (!t.parent)
        {
            return t.name;

        }
        return GetTransformPath(t.parent) + "/" + t.name;
    }

}
