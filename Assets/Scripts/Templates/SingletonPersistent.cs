using UnityEngine;

// Instancia unica de una clase
public abstract class SingletonPersistent<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject gameObject = new GameObject(typeof(T).Name);
                    instance = gameObject.AddComponent<T>();
                }
            }
            return instance;
        }
    }
    protected virtual void Awake()
    {
        // Si no existe la clase, se crea
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        // Si existe otra instancia de la misma clase, se elimina
        else
        {
            Destroy(gameObject);
        }
    }
}
