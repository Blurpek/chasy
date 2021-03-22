using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Mirror;
using Cinemachine;

public delegate Vector2 GetDirectionalInputDelegate();

[RequireComponent( typeof(DashController), typeof(MovementController) )]
public class PlayerController2d : NetworkBehaviour
{
	Animator animator;
	BoxCollider2D boxCollider;

	// movement config
	public float gravity = -25f;
	public float runSpeed = 2f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;

	private DashController dashController;

	private MovementController movementController;

	private CharacterController2D _controller;
	// private Animator _animator;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;
	
	private Vector2 directionalInput;

	private InputAction horizontalInputAction;
	private InputAction verticalInputAction;
	private InputAction jumpInputAction;

	public override void OnStartClient() {
		if (this.isLocalPlayer) { 
			GameObject camera = GameObject.FindGameObjectWithTag("PlayerCamera");
			camera.GetComponent<CinemachineVirtualCamera>().m_Follow = transform;
		}
	} 

	void Awake()
	{
		// _animator = GetComponent<Animator>();
		_controller = GetComponent<CharacterController2D>();
		animator = GetComponent<Animator>();
		boxCollider = GetComponent<BoxCollider2D>();
		movementController = GetComponent<MovementController>();
		dashController = GetComponent<DashController>();

		PlayerInput playerInput = GetComponent<PlayerInput>();
		horizontalInputAction = playerInput.actions["HorizontalMove"];
		verticalInputAction = playerInput.actions["VerticalMove"];
		jumpInputAction = playerInput.actions["Jump"];

		// listen to some events for illustration purposes
		_controller.onControllerCollidedEvent += onControllerCollider;
		_controller.onTriggerEnterEvent += onTriggerEnterEvent;
		_controller.onTriggerExitEvent += onTriggerExitEvent;
	}


	#region Event Listeners

	void onControllerCollider( RaycastHit2D hit )
	{
		// bail out on plain old ground hits cause they arent very interesting
		if( hit.normal.y == 1f )
			return;

		// logs any collider hits if uncommented. it gets noisy so it is commented out for the demo
		// Debug.Log( "flags: " + _controller.collisionState + ", hit.normal: " + hit.normal );
	}


	void onTriggerEnterEvent( Collider2D col )
	{
		Debug.Log( "onTriggerEnterEvent: " + col.gameObject.name );
	}


	void onTriggerExitEvent( Collider2D col )
	{
		Debug.Log( "onTriggerExitEvent: " + col.gameObject.name );
	}

	#endregion

	public Vector2 GetDirectionalInput() { return directionalInput; }

	//dont use get axis because when releasing button it still has non zero values (coz it floatz)

	// the Update loop contains a very simple example of moving the character around and controlling the animation
	void Update()
	{
		if (!this.isLocalPlayer) { return; }

		directionalInput = new Vector2(horizontalInputAction.ReadValue<float>(), verticalInputAction.ReadValue<float>());
		
		if (dashController.isDashing) {
			_controller.move( dashController.velocity * Time.deltaTime );
		} else { 
			
			if (dashController.HandleDash()) {
				StartCoroutine(dashController.Dash(new GetDirectionalInputDelegate(GetDirectionalInput)));
			}

			_velocity = movementController.HandleMovement(directionalInput, jumpInputAction.ReadValue<float>() > 0);			

			_controller.move( _velocity * Time.deltaTime );
		}


		_velocity = _controller.velocity;
		FaceMovementDirection();

		animator.SetFloat("speed", Mathf.Abs(_velocity.x));
		animator.SetFloat("jumpSpeed", _velocity.y);
	}

	float easeOutQuint(float x) {
		return 1 - Mathf.Pow(1 - x, 5);
	}

	void FaceMovementDirection() {
		if (_velocity.x > 0 && transform.localScale.x < 0 || _velocity.x < 0 && transform.localScale.x > 0) { 	
			transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
		}
	}

}
