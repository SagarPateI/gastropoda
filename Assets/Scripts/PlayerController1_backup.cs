using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// Code modified from https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs

// Jumping code modified from https://mikeadev.net/2015/08/variable-jump-height-in-unity/
[RequireComponent(typeof(Rigidbody))]
public class PlayerController1_backup : MonoBehaviour
{

	[SerializeField] private float m_JumpVelocity = 150f;                       // Amount of initial velocity added when the player jumps.
	[SerializeField] private float m_cancelJumpVelocity = 70f;					// Jump can no longer be canceled once the player's y velocity is less than this
	[SerializeField] private float m_JumpAccel = 150f;                          // Amount of acceleration applied if the jump is extended.
	[SerializeField] private float m_JumpBrake = -150f;                         // Amount of deceleration applied at the end of a jump
	[SerializeField] private float m_startJumpTime;                                             // Time at the start of the jump
	[SerializeField] private float m_maxJumpTime;												// Time when jump should end
	[SerializeField] private float m_airJumpTime;                               // Maximum duration of a jump in milliseconds
	[SerializeField] private bool m_isJumping = false;                                           // For determining if the player is performing a jump
	[SerializeField] private bool m_wasJumping = false;                                         // For determining if the player is airborne due to a jump

	private float m_move = 0;
	private bool m_crouch = false;
	private bool m_jump = false;
	private bool m_jumpHeld = false;

	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;          // Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;  // How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
	[SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
	[SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider that will be disabled when crouching

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	[SerializeField] private bool m_Grounded;            // Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private Vector3 m_Velocity = Vector3.zero;


	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

	private void FixedUpdate()
	{
		Move(m_move, m_crouch, m_jump, m_jumpHeld);

		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!m_isJumping)
					m_wasJumping = false;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
	}

	public void setMovementVars(float move, bool crouch, bool jump, bool jumpHeld)
    {
		m_move = move;
		m_crouch = crouch;
		m_jump = jump;
		m_jumpHeld = jumpHeld;
	}

	public void Move(float move, bool crouch, bool jump, bool jumpHeld)
	{
		// If crouching, check to see if the character can stand up
		if (crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//only control the player if grounded or airControl is turned on
		if (m_Grounded || m_AirControl)
		{

			// If crouching
			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			}
			else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			m_Rigidbody2D.velocity = targetVelocity;

			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
		}
		// If the player should jump...
		// @JUMP
		if (m_Grounded && jumpHeld && !m_isJumping)
		{
			// Add a vertical force to the player and set the appropriate flags.
			m_isJumping = true;
			m_Grounded = false;
			m_wasJumping = true;
			
			m_startJumpTime = Time.time;
			m_maxJumpTime = m_startJumpTime + m_airJumpTime;
			m_Rigidbody2D.AddForce(new Vector2(0, m_JumpVelocity), ForceMode2D.Impulse); // Velocity change = Force / Mass
		}
		// Increase jump height if jump button is held.
		else if (m_isJumping && jumpHeld && (Time.time < m_maxJumpTime))
        {
			m_Rigidbody2D.AddForce(new Vector2(0, m_JumpAccel), ForceMode2D.Force);		// Velocity change = Force * dT / Mass
        }
        // End of jump
        Debug.Log(Time.time >= m_maxJumpTime);
		if (!jumpHeld || (Time.time >= m_maxJumpTime)) {
			m_isJumping = false;
		}
		// Apply "brake" velocity to shorten jump height if player is still going upwards
		if (m_wasJumping && !m_isJumping && m_Rigidbody2D.velocity.y > 0)
		{
			Debug.Log("Brake force applied");
			m_Rigidbody2D.AddForce(new Vector2(0, m_JumpBrake), ForceMode2D.Force);
		}
	}


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
