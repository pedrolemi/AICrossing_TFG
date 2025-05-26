using UnityEngine;

// Instancia unica de una clase
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            // Si el objeto no se ha creado, se trata de crear
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
        }
        // Si existe otra instancia de la misma clase, se elimina
        else
        {
            Destroy(gameObject);
        }
    }
}
