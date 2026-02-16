using UnityEngine;

[CreateAssetMenu(fileName = "QAConfig", menuName = "QATool/Config")]
public class QAToolConfig : ScriptableObject
{
    private static QAToolConfig instance;
    public static QAToolConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<QAToolConfig>("QAToolConfig");
            }
            return instance;
        }
    }
}
