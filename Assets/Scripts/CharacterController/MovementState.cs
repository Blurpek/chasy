using UnityEngine;

public enum WayOfLeavingGround { FALL, JUMP, WALL_JUMP }
public enum JumpState { GROUNDED, JUMPING, JUMP_FLOATING, FALL_FLOATING, FALLING }

public class MovementState {
    public Vector2 velocity;
    public Vector2 lastFrameVelocity;
    public float jumpStartTime;
    public bool wallSliding;
    public int wallDirX;
    public WayOfLeavingGround lastWayOfLeavingGround;
    public JumpState jumpState;

	public bool IsGrounded() { 
		return this.jumpState == JumpState.GROUNDED; 
	}
}