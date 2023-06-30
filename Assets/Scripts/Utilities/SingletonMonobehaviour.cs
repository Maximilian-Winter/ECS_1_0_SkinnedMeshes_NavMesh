using UnityEngine;

namespace Utilities
{

public class SingletonMonoBehaviour < T > : MonoBehaviour where T : MonoBehaviour
{
    private static T s_Instance;

    public static T Instance
    {
        get
        {
            if ( s_Instance == null )
            {
                s_Instance = FindObjectOfType < T >();
            }

            return s_Instance;
        }
    }

    #region Protected

    protected void RegisterInstanceOrDestroy( T instance )
    {
        if ( s_Instance == null )
        {
            s_Instance = instance;
        }
        else if ( s_Instance != this )
        {
            Destroy( instance );
        }
    }

    #endregion
}

}
