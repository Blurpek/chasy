using UnityEngine;
using System.Collections;
using Mirror;

public class DashController : NetworkBehaviour {
    
    public Vector2 velocity = Vector2.zero;

    private BoxCollider2D boxCollider;
    private Animator animator;
    private MovementController movementController;
    private CharacterController2D characterController;

	public float dashDistance = 3.5f;
	public float dashTime = 0.2f;
	public float dashReadingDirectionTime = 0.1f;
	public float dashCooldown = 2f;

	private float startDashTime = 0f;

	private Vector2 dashDirection = Vector2.zero;
    public bool isDashing = false;

	void Awake()
	{
		// _animator = GetComponent<Animator>();
		characterController = GetComponent<CharacterController2D>();
		animator = GetComponent<Animator>();
		boxCollider = GetComponent<BoxCollider2D>();
		movementController = GetComponent<MovementController>();
	}

    // public DashController(BoxCollider2D _boxCollider, Animator _animator, JumpController _jumpController, CharacterController2D _controller) {
    //     this.boxCollider = _boxCollider;
    //     animator = _animator;
    //     jumpController = _jumpController;
    //     characterController = _controller;
    // }

    public bool HandleDash() {
        if (Input.GetKeyDown(KeyCode.LeftControl) && startDashTime + dashTime + dashCooldown < Time.time) { 
            isDashing = true;
            return true;
        }
        
        return false;
    }

//wall jump + dash + jumpbutton = go left? xd

	public IEnumerator Dash(GetDirectionalInputDelegate GetDirectionalInput) {
		startDashTime = Time.time;

		// velocity.x = dashDistance / dashTime;
		animator.SetBool("dash", true);
		// yield return new WaitForSeconds(0.1);

		Vector2 oldVelocity = velocity;
		velocity.x = 0;
		velocity.y = 0;
		// RaycastHit2D raycastHit = Physics2D.Raycast(new Vector2(boxCollider.bounds.max.x, boxCollider.bounds.max.y), Vector2.right, dashDistance);

		yield return new WaitForSeconds(dashReadingDirectionTime);

		dashDirection = GetDirectionalInput();
            
		Vector2 shift = Vector2.zero;

		if (dashDirection.x != 0 || dashDirection.y != 0) {
			float skinWidth = 0.1f;
			Vector2[] verts = new Vector2[4];
			verts[0] = transform.TransformPoint(boxCollider.offset + new Vector2(-boxCollider.size.x + skinWidth, boxCollider.size.y - skinWidth) * 0.5f);
			verts[1] = transform.TransformPoint(boxCollider.offset + new Vector2(boxCollider.size.x - skinWidth, boxCollider.size.y - skinWidth) * 0.5f);
			verts[2] = transform.TransformPoint(boxCollider.offset + new Vector2(boxCollider.size.x - skinWidth, -boxCollider.size.y + skinWidth) * 0.5f);
        	verts[3] = transform.TransformPoint(boxCollider.offset + new Vector2(-boxCollider.size.x + skinWidth, -boxCollider.size.y + skinWidth) * 0.5f);

			if (dashDirection.x == 0) {
				shift = dashDirection.y == 1 
					  ? CalculateDashShift(verts[0], verts[1], 6, dashDirection, dashDistance, characterController.platformMask)
					  : CalculateDashShift(verts[3], verts[2], 6, dashDirection, dashDistance, characterController.platformMask);
			} else if (dashDirection.y == 0) {
				shift = dashDirection.x == 1
					  ? CalculateDashShift(verts[1], verts[2], 6, dashDirection, dashDistance, characterController.platformMask)
					  : CalculateDashShift(verts[0], verts[3], 6, dashDirection, dashDistance, characterController.platformMask);
			} else {
				shift = dashDirection.x == dashDirection.y
					? CalculateDashShift(verts[0], verts[2], 6, dashDirection, dashDistance, characterController.platformMask)
					: CalculateDashShift(verts[1], verts[3], 6, dashDirection, dashDistance, characterController.platformMask);
			}
		}


		velocity.x = shift.x / (dashTime - dashReadingDirectionTime);
		velocity.y = shift.y / (dashTime - dashReadingDirectionTime);

		yield return new WaitForSeconds(dashTime - dashReadingDirectionTime);

		// Debug.Log(isDashing);

		// this.transform.position = new Vector2(this.transform.position.x, this.transform.position.y) + shift;

		// yield return new WaitForEndOfFrame();

		movementController.jumpStartTime = Time.time - movementController.jumpDuration / 2;
		
		if (characterController.isGrounded) {
			velocity.y = 0;
		} else if (dashDirection.y == 0) { 
			movementController.jumpStartTime = Time.time - movementController.jumpDuration - movementController.floatDuration / 2;
			velocity.y = movementController.CalculateFloatingVelocity();
		} else if (dashDirection.y > 0) {
			movementController.jumpStartTime = Time.time - movementController.jumpDuration / 2;
			velocity.y = movementController.CalculateJumpingVelocity();
		} else { 
			movementController.jumpStartTime = Time.time - movementController.jumpDuration - movementController.floatDuration;
			velocity.y = -movementController.jumpSpeed;
		}

		velocity.x = dashDirection.x * 6;

		animator.SetBool("dash", false);
		isDashing = false;
	}

	Vector2 CalculateDashShift(Vector2 start, Vector2 end, int totalRays, Vector2 direction, float distance, LayerMask platformMask) {
		Vector2 step = (end - start) / (totalRays - 1);
		Vector2 shift = direction.x != 0 && direction.y != 0
						? new Vector2(direction.x * distance * Mathf.Sqrt(0.5f), direction.y * distance * Mathf.Sqrt(0.5f))
						: new Vector2(direction.x * distance, direction.y * distance);


		for (int i = 0; i < totalRays; i++) {
			RaycastHit2D hit = Physics2D.Raycast(start + i * step, direction, distance - 0.15f, platformMask);

			if (hit.collider != null) {
				Vector2 shiftToHit = hit.point - (start + i * step);
				
				if (Mathf.Pow(shiftToHit.x, 2) + Mathf.Pow(shiftToHit.y, 2) < Mathf.Pow(shift.x, 2) + Mathf.Pow(shift.y, 2)) {
					shift = shiftToHit;
				}
			}
		}

		return shift - direction * 0.5f;
	}

}