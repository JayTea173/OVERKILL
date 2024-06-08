using System.Text;
using UnityEngine;

namespace OVERKILL;

public static class Util
{
    public static bool TryGetComponent <T>(this GameObject go, out T comp) where T : Component
    {
        comp = go.GetComponent <T>();
        return comp == null;
    }
    
    public static bool TryGetComponent <T>(this Component go, out T comp) where T : Component
    {
        comp = go.GetComponent <T>();
        return comp == null;
    }

    public static string GetGameObjectScenePath(this GameObject go)
    {
        string s = string.Empty;
        var t = go.transform;

        while (t != null)
        {
            if (s.Length > 0)
                s = "/" + s;
            s = t.gameObject.name + s;
            t = t.parent;
        }

        return s;
    }
}
