using UnityEngine;
namespace QATool {
public static class QAToolSceneValidator 
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static event System.Action forceValidate;

    public static void ForceValidate()
    {
        forceValidate?.Invoke();
    }
    
    
}
}
