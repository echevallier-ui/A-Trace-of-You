using UnityEngine;

public class ItemTag : MonoBehaviour
{
    [SerializeField] private string itemID = "Key";
    public string ItemID => itemID;
}