using UnityEngine;
using StarterAssets; // Requis pour accéder à FirstPersonController et StarterAssetsInputs

public class InventoryUI : MonoBehaviour
{
    [Header("Références Joueur")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private StarterAssetsInputs _input;    
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Structure UI")]
    [SerializeField] private GameObject inventoryPanel; 
    [SerializeField] private InventorySlotUI[] slots;

    private bool isOpen = false;

    private void Start()
    {
        if (playerController == null && PersistentCoreManager.Instance != null)
        {
            playerController = PersistentCoreManager.Instance.PlayerController;
        }

        if (PlayerInventory.Instance != null)
        {
            if (_input == null) _input = PlayerInventory.Instance.GetComponent<StarterAssetsInputs>();

            PlayerInventory.Instance.OnInventoryChanged += RefreshUI;
        }

        SetInventoryState(false);
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= RefreshUI;
        }
    }

    private void Update()
    {
        if (_input != null && _input.inventory)
        {
            ToggleInventory();
            _input.inventory = false;
        }
    }

    public void ToggleInventory()
    {
        SetInventoryState(!isOpen);
    }

    private void SetInventoryState(bool open)
    {
        isOpen = open;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = open ? 1f : 0f;          // 1 = Visible, 0 = Transparent
            canvasGroup.interactable = open;             // Active/désactive les boutons
            canvasGroup.blocksRaycasts = open;           // Bloque ou laisse passer les clics
        }

        // Curseur et joueur
        //Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        //Cursor.visible = open;

        if (playerController != null) playerController.enabled = !open;

        if (open) RefreshUI();
    }

    private void RefreshUI()
    {
        if (PlayerInventory.Instance == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < PlayerInventory.Instance.items.Count)
            {
                slots[i].SetItem(PlayerInventory.Instance.items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }
}