using UnityEngine;
using Mirror;

public class MovementController : NetworkBehaviour {
	private CharacterController2D _controller;

    [HideInInspector]
    public MovementState state;

	[HideInInspector]
	public Vector2 velocity;

	[HideInInspector]
	public float jumpStartTime = 0;
	public float jumpSpeed = 12f;
	public float jumpDuration = 0.3f;
	public float floatDuration = 0.25f;


	private bool wallSliding;
	private int wallDirX;

	public float wallSlideSpeedMax = 1;
	public float wallStickTime = .25f;
	private float timeToWallUnstick;
	public float runSpeed = 9f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 12f;

	void Awake() {
		_controller = GetComponent<CharacterController2D>();
        state = new MovementState();
	}

	//states of jump: Jumping (gaining height) -> Floating -> Falling
	public Vector2 HandleMovement(Vector2 directionalInput, bool jumpButtonDown) {
        UpdateState();
		HandleHorizontalMovement(directionalInput.x);
		HandleWallSliding(directionalInput.x);

		if (state.IsGrounded() && jumpStartTime != 0) { 
			jumpStartTime = 0;
		}
		
		// if holding down bump up our movement amount and turn off one way platform detection for a frame.
		// this lets us jump down through one way platforms
		// if( _controller.isGrounded && Input.GetKey( KeyCode.DownArrow ) )
		// {
		// 	state.velocity.y *= 3f;
		// 	_controller.ignoreOneWayPlatformsThisFrame = true;
		// }

		// if (state.jumpState == JumpState.GROUNDED) {
		// 	state.lastWayOfLeavingGround = null;
		// }

		if (jumpButtonDown && state.jumpState == JumpState.GROUNDED) {
			Jump();
		} else if (jumpButtonDown && state.wallSliding) {
			WallJump(directionalInput);
		} else if (state.jumpState == JumpState.JUMPING) {

			if (ShouldStopJumping(jumpButtonDown)) {
				jumpStartTime = Time.time - jumpDuration;
				state.velocity.y = CalculateFloatingVelocity();
			} else {
				state.velocity.y = CalculateJumpingVelocity() * jumpSpeed;
			}

		} else if (state.jumpState == JumpState.JUMP_FLOATING || state.jumpState == JumpState.FALL_FLOATING) {
			state.velocity.y = CalculateFloatingVelocity();

			if (state.wallSliding && state.velocity.y < -wallSlideSpeedMax) {
				state.velocity.y = -wallSlideSpeedMax;
			}

		} else if (jumpStartTime == 0 && !state.IsGrounded()) {
			jumpStartTime = Time.time - jumpDuration - floatDuration / 2;
			state.velocity.y = CalculateFloatingVelocity();
			state.jumpState = JumpState.FALL_FLOATING;
		} else {
			state.velocity.y = _controller.isGrounded ? -1f : CalculateFallingVelocity() * -jumpSpeed;

			if (wallSliding && state.velocity.y < -wallSlideSpeedMax) {
				state.velocity.y = -wallSlideSpeedMax;
			}
		}

        state.lastFrameVelocity = state.velocity;
		return state.velocity;
	}

    private void UpdateState() {
		state.velocity = _controller.velocity;

        UpdateJumpState();
    }

    private void UpdateJumpState() {
        if (_controller.isGrounded) {
            state.jumpState = JumpState.GROUNDED;
        } else if (!_controller.isGrounded && jumpStartTime + jumpDuration > Time.time) {
            state.jumpState = JumpState.JUMPING;
        } else if (!_controller.isGrounded && jumpStartTime + jumpDuration + floatDuration / 2 > Time.time) {
            state.jumpState = JumpState.JUMP_FLOATING;
        } else if (!_controller.isGrounded && jumpStartTime + jumpDuration + floatDuration > Time.time) {
            state.jumpState = JumpState.FALL_FLOATING;
        } else {
            state.jumpState = JumpState.FALLING;
        }
    }

	private bool IsJumping() {
		return !_controller.isGrounded && jumpStartTime + jumpDuration > Time.time;
	}

	private bool ShouldStopJumping(bool jumpButtonDown) {
		return !jumpButtonDown && jumpStartTime + jumpDuration / 2 < Time.time;
	}

	private bool IsFloating() {
		return !_controller.isGrounded && jumpStartTime + jumpDuration + floatDuration > Time.time;
	}

	private void Jump() {
		jumpStartTime = Time.time;
		state.velocity.y = CalculateJumpingVelocity() * jumpSpeed;
        state.lastWayOfLeavingGround = WayOfLeavingGround.JUMP;	
	}

	private void WallJump(Vector2 directionalInput) {
		jumpStartTime = Time.time - jumpDuration / 2.8f;
		state.velocity = new Vector2(-state.wallDirX * runSpeed * 1.3f, CalculateJumpingVelocity());
        state.lastWayOfLeavingGround = WayOfLeavingGround.WALL_JUMP;
	}

	public float CalculateJumpingVelocity() {
		float x = (Time.time - jumpStartTime) / jumpDuration;

		return -Mathf.Pow(x, 2.6f) + 1.2f;
	}

	public float CalculateFloatingVelocity() {
		float x = (Time.time - jumpStartTime - jumpDuration) / floatDuration;
		int sign = x < 0.5f ? 1 : -1;

		return sign * 32 * Mathf.Pow(x - 0.5f, 2);
	}

	public float CalculateFallingVelocity2() {
		float x = (Time.time - jumpStartTime - jumpDuration - floatDuration) / 0.2f;

		if (x < 0) {
			return 0;
		}

		return -1 / (x + 1.2f) + 1.4f;
	}

	public float CalculateFallingVelocity() {
		float x = (Time.time - jumpStartTime - jumpDuration - floatDuration) / 2;

		if (x < 0) {
			return 0;
		}

		if (x > 0.5) {
			return 2;
		}

		return Mathf.Pow(2 * x / 5 + 1f, 2);
	}

	private void HandleHorizontalMovement(float horizontalInput) {
        if (state.lastWayOfLeavingGround == WayOfLeavingGround.WALL_JUMP && jumpStartTime + 0.28f > Time.time) {
            state.velocity.x = state.lastFrameVelocity.x;
        } else {
            var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
            state.velocity.x = Mathf.Lerp( state.velocity.x, horizontalInput * runSpeed, Time.deltaTime * smoothedMovementFactor );            
        }
	}

	void HandleWallSliding(float horizontalInput) {
		state.wallDirX = (_controller.collisionState.left) ? -1 : 1;
		state.wallSliding = false;

		if ((_controller.collisionState.left || _controller.collisionState.right) && !_controller.collisionState.below && state.velocity.y < 0) {
			state.wallSliding = true;

			// if (_velocity.y < -wallSlideSpeedMax) {
			// 	velocity.y = -wallSlideSpeedMax;
			// }

			// if (timeToWallUnstick > 0) {
			// 	// velocityXSmoothing = 0;
			// 	state.velocity.x = 0;

			// 	if (horizontalInput != wallDirX && horizontalInput != 0) {
			// 		timeToWallUnstick -= Time.deltaTime;
			// 	}
			// 	else {
			// 		timeToWallUnstick = wallStickTime;
			// 	}
			// }
			// else {
			// 	timeToWallUnstick = wallStickTime;
			// }
		}
	}
}