using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool interact;
		public bool action;
		public bool inventory;
		public bool torch;

		[Header("UI Input Values")]
		public bool click; // Variable d'état pour l'UI

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		
		public void OnInteract(InputValue value)
		{
			InteractInput(value.isPressed);
		}

		public void OnAction(InputValue value)
		{
			ActionInput(value.isPressed);
		}

		public void OnInventory(InputValue value)
		{
			InventoryInput(value.isPressed);
		}

		public void OnTorch(InputValue value)
		{
			TorchInput(value.isPressed);
		}

		public void OnClick(InputValue value)
		{
			ClickInput(value.isPressed);
		}
#endif

		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void InteractInput(bool newInteractState)
		{
			interact = newInteractState;
		}

		public void ActionInput(bool newActionState)
		{
			action = newActionState;
		}

		public void InventoryInput(bool newInventoryState)
		{
			inventory = newInventoryState;
		}

		public void TorchInput(bool newTorchState)
		{
			torch = newTorchState;
		}

		public void ClickInput(bool newClickState)
		{
			click = newClickState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
}