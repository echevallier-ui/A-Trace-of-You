using UnityEngine;

public class SaveActiveState : MonoBehaviour, ISaveable
{
    public object SaveState()
    {
        bool state = gameObject.activeSelf;
        //Debug.Log($"[SaveActiveState] 💾 SAUVEGARDE de '{GetPath(transform)}' | State = {state}");
        return state;
    }

    public void LoadState(object state)
    {
        if (state is bool isActive)
        {
            //Debug.Log($"[SaveActiveState] 📂 RESTAURATION de '{GetPath(transform)}' | SetActive({isActive})");
            gameObject.SetActive(isActive);
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}