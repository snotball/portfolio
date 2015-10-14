using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class Player : NetworkBehaviour, IDamageable
{
	// Look
	Vector2 prevMouseInput = Vector2.zero;
	float lookXSpeed = 6.0f;
	float lookYSpeed = 3.0f;

	// Move
	Vector3 inputVector = Vector3.zero;
	Vector3 lerpVector = Vector3.zero;
	Vector3 moveVector = Vector3.zero;
	Vector3 rotationVector = Vector3.forward;

	Vector3 overrideMovement = Vector3.zero;
	Vector3 overrideRotation = Vector3.zero;
	
	float moveSpeed = 4.0f;
	float moveSpeedSprint = 6.0f;
	float moveSpeedWalk = 2.0f;
	float moveSpeedRoll = 7.5f;

	float turnSpeed = 10.0f;
	float accSpeed = 7.0f;
	float gravity = 25.0f;

	enum Stances { Stand, Sprint, Roll }
	[SyncVar] Stances stance = Stances.Stand;

	// Interact
	InteractableList interactables = new InteractableList();

	// Weapons
	[SyncVar] GameObject weaponMain;

	// Attack
	enum Attacks { Left, Right }
	Attacks prevAttack = Attacks.Right;

	// Animation
	Vector3 prevPos;

	// Properties
	Transform characterModel;
	Transform CharacterModel
	{
		get
		{
			if (characterModel == null)
			{
				characterModel = Resources.Load<Transform>("models/dummy");
				characterModel = (Transform)Instantiate(characterModel);
				characterModel.parent = this.transform;
				characterModel.localPosition = Vector3.zero;
				characterModel.localRotation = Quaternion.identity;

			}
			
			return characterModel;
		}
	}

	private Transform boneLItem;
	public Transform BoneLItem
	{
		get
		{
			if (boneLItem == null)
				boneLItem = ToolBox.FindChildTransform("bone_l_item", CharacterModel);
			
			return boneLItem;
		}
	}

	private Transform boneRItem;
	public Transform BoneRItem
	{
		get
		{
			if (boneRItem == null)
				boneRItem = ToolBox.FindChildTransform("bone_r_item", CharacterModel);
			
			return boneRItem;
		}
	}

	Animation characterAnimation;
	Animation CharacterAnimation
	{
		get
		{
			if (characterAnimation == null)
				characterAnimation = CharacterModel.GetComponent<Animation>();
			
			return characterAnimation;
		}
	}

	List<Collider> colliders;
	public List<Collider> Colliders
	{
		get
		{
			if (colliders == null)
				colliders = new List<Collider>();

			return colliders;
		}
	}

	CharacterController characterController;
	CharacterController CharacterController
	{
		get
		{
			if (characterController == null)
				characterController = GetComponent<CharacterController>();
			
			return characterController;
		}
	}

	CameraOrbit cameraOrbit;
	CameraOrbit CameraOrbit
	{
		get
		{
			if (cameraOrbit == null)
			{
				cameraOrbit = Camera.main.gameObject.AddComponent<CameraOrbit>();
				cameraOrbit.GetComponent<Camera>().fieldOfView = 60.0f;
				cameraOrbit.GetComponent<Camera>().nearClipPlane = 0.1f;

				GameObject go = new GameObject("CameraSubject");
				CameraSubject cs = go.AddComponent<CameraSubject>();
				cs.target = transform;
				cameraOrbit.Subject = go.transform;
				go.transform.position = transform.position;
			}
			
			return cameraOrbit;
		}
	}

	// Resources
	Texture2D guiCrosshair;
	Texture2D GuiCrosshair
	{
		get
		{
			if (guiCrosshair == null)
				guiCrosshair = Resources.Load<Texture2D>("materials/gui_crosshair");
			
			return guiCrosshair;
		}
	}
	
	DamageSweep damageSweep;
	DamageSweep DamageSweep
	{
		get
		{
			if (damageSweep == null)
				damageSweep = Resources.Load<DamageSweep>("prefabs/DamageSweep");
			
			return damageSweep;
		}
	}

	// Booleans
	float nextMoveTime = 0.0f;
	float nextRollTime = 0.0f;
	float nextAttackTime = 0.0f;

	bool AllowInput { get { return !Cursor.visible; } }
	bool AllowMove { get { return AllowInput && Time.time >= nextMoveTime; } }
	bool AllowRoll { get { return AllowInput && Time.time >= nextRollTime; } }
	bool AllowAttack { get { return AllowInput && Time.time >= nextAttackTime; } }

	void Start()
	{
		// LocalPlayer Setup
		if (hasAuthority)
			CharacterController.enabled = true;

		// Hit-collider Setup
		HitColliderSetup(CharacterModel);
		ToolBox.ChangeLayerRecursive(CharacterModel, "Characters");

		// Character Animation
		CharacterAnimation["idle"].layer = 0;
		CharacterAnimation["run_forward"].layer = 1;
		CharacterAnimation["roll"].layer = 2;

		foreach (AnimationState animState in CharacterAnimation)
		{
			if (animState.name.Contains("attack"))
			{
				animState.layer = 2;
				animState.weight = 0;
			}
		}

		// Add first state to network interpolation
		SyncState(new State(false, transform.localPosition, (int)transform.localEulerAngles.y));
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		
		// Player already has weaponMain according to SyncVar
		if (weaponMain)
		{
			weaponMain.transform.parent = BoneRItem;
			weaponMain.transform.localPosition = Vector3.zero;
			weaponMain.transform.localRotation = Quaternion.identity;
			ToolBox.EnableTriggers(false, weaponMain);
		}
	}

	void Update()
	{
		if (hasAuthority)
		{
			Look();
			Move();
			Sprint();
			Roll();
			Attack();
			Interact();

			MoveLogic();
		}

		Animate();
		NetUpdate();
	}

	void Look()
	{
		if (AllowInput)
		{
			Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

			Vector2 avgLook = mouseInput;
			avgLook += prevMouseInput;
			avgLook *= 0.5f;

			avgLook.x *= lookXSpeed;
			avgLook.y *= lookYSpeed;

			CameraOrbit.InputLook(avgLook);

			prevMouseInput = mouseInput;
		}
	}

	void Move()
	{
		if (AllowInput)
		{
			// Camera relative forward vector
			Vector3 camForward = CameraOrbit.transform.TransformDirection(Vector3.forward);
			camForward.y = 0;
			camForward = camForward.normalized;
			
			// Camera relative right vector
			Vector3 camRight = new Vector3(camForward.z, 0, -camForward.x);
			
			// Target direction relative to the camera
			inputVector = Input.GetAxisRaw("Horizontal") * camRight + Input.GetAxisRaw("Vertical") * camForward;
			
			if (inputVector.sqrMagnitude > 1)
				inputVector = inputVector.normalized;
			else if (inputVector.sqrMagnitude < 0.25f)
				inputVector = Vector3.zero;
		}
		else
		{
			// Reset inputVector
			inputVector = new Vector3(0, 0, 0);
		}
	}

	void MoveLogic()
	{
		if (CharacterController.isGrounded)
		{
			// Prepare moveVector
			Vector3 targetVector;
			if (overrideMovement != Vector3.zero)
			{
				targetVector = overrideMovement;
			}
			else if (AllowMove)
			{
				targetVector = inputVector;

				if (stance == Stances.Sprint)
					targetVector *= moveSpeedSprint;
				else if (stance == Stances.Parry)
					targetVector *= moveSpeedWalk;
				else
					targetVector *= moveSpeed;
			}
			else
			{
				targetVector = Vector3.zero;
			}

			lerpVector = Vector3.Lerp(lerpVector, targetVector, Time.deltaTime * accSpeed);
			moveVector = lerpVector;
			
			// Slope Correction
			float pushDownOffset = Mathf.Max(CharacterController.stepOffset, new Vector3(moveVector.x, 0, moveVector.z).magnitude);
			moveVector -= pushDownOffset * Vector3.up;
		}
		
		// Set and Rotate towards rotationVector
		if (overrideRotation != Vector3.zero)
		{
			rotationVector = overrideRotation;
		}
		else if (lerpVector.sqrMagnitude > 0.05f)
		{
			rotationVector = new Vector3(lerpVector.x, 0, lerpVector.z);
		}
		
		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rotationVector), Time.deltaTime * turnSpeed);
		
		// Apply moveVector
		moveVector.y -= gravity * Time.deltaTime;
		CharacterController.Move(moveVector * Time.deltaTime);
	}
	
	IEnumerator MomentaryInput(Vector3 vector, float time, float wait)
	{
		yield return new WaitForSeconds(wait);
		overrideMovement = vector;
		yield return new WaitForSeconds(time);
		overrideMovement = Vector3.zero;
	}

	void Sprint()
	{
		if (!AllowMove)
			return;

		if (Input.GetKey(KeyCode.LeftShift) && stance != Stances.Sprint)
			stance = Stances.Sprint;
		else if (!Input.GetKey(KeyCode.LeftShift) && stance == Stances.Sprint)
			stance = Stances.Stand;
	}

	void Roll()
	{
		if (!AllowRoll)
			return;

		if (Input.GetKeyDown(KeyCode.Space) && inputVector != Vector3.zero)
		{
			if (weaponMain != null)
				weaponMain.GetComponent<WeaponMelee>().weaponTrail.SetActive(false);

			prevAttack = Attacks.Right;
			stance = Stances.Roll;

			overrideRotation = inputVector.normalized;
			overrideMovement = overrideRotation * moveSpeedRoll;

			nextMoveTime = Time.time + (1f / 30f) * 20;
			nextRollTime = Time.time + (1f / 30f) * 20;
			nextAttackTime = Time.time + (1f / 30f) * 20;
			nextParryTime = Time.time + (1f / 30f) * 20;

			CmdAnimRoll();

			StopAllCoroutines();
			StartCoroutine(RollRoutine());
		}
	}

	IEnumerator RollRoutine()
	{
		yield return new WaitForSeconds((1f / 30f) * 15);

		overrideMovement = Vector3.zero;
		overrideRotation = Vector3.zero;

		stance = Stances.Stand;
	}

	[Command] void CmdAnimRoll() { RpcAnimRoll(); }
	[ClientRpc] void RpcAnimRoll()
	{
		StopCoroutine("AnimRollRoutine");
		StartCoroutine(AnimRollRoutine());
	}
	IEnumerator AnimRollRoutine()
	{
		if (!hasAuthority)
			yield return new WaitForSeconds(syncInterval * 2);

		string animName = "roll";
		CharacterAnimation.Rewind(animName);
		ToolBox.BlendLayerAnim(animName, CharacterAnimation, (1f / 30f) * 2);
		
		yield return new WaitForSeconds(CharacterAnimation[animName].length - (1f / 30f) * 10);
		
		CharacterAnimation.Blend(animName, 0.0f, (1f / 30f) * 10);
	}

	void Attack()
	{
		if (!AllowAttack)
			return;

		if (weaponMain == null)
			return;

		if (Input.GetMouseButtonDown(0))
		{
			StopAllCoroutines();
			
			overrideMovement = Vector3.zero;
			overrideRotation = (inputVector == Vector3.zero) ? transform.forward : inputVector.normalized;
			
			if (Vector3.Angle(overrideRotation, transform.forward) > 50)
			{
				if (Vector3.Dot(overrideRotation, transform.right) < 0)
					AttackStarter(Attacks.Left);
				else
					AttackStarter(Attacks.Right);
			}
			else
			{
				if (prevAttack == Attacks.Right)
					AttackStarter(Attacks.Left);
				else
					AttackStarter(Attacks.Right);
			}
			
			StartCoroutine(MomentaryInput(overrideRotation * 3.0f, (1f / 30f) * 5, 0));
		}
	}

	void AttackStarter(Attacks attack)
	{
		bool clockwise = false;
		switch (attack)
		{
			case Attacks.Left:
				clockwise = false;
				break;
			case Attacks.Right:
				clockwise = true;
				break;
		}

		StartCoroutine(AttackRoutine(attack, 25, 10, 10, 17));
		StartCoroutine(DamageRoutine(4, 1.4f, 1.8f, 160, 3, clockwise));
	}

	IEnumerator AttackRoutine(Attacks attack, int animMoveFrame, int animRollFrame, int animAttackFrame, int animParryFrame)
	{
		prevAttack = attack;
		stance = Stances.Stand;

		nextMoveTime = Time.time + (1f / 30f) * animMoveFrame;
		nextRollTime = Time.time + (1f / 30f) * animRollFrame;
		nextAttackTime = Time.time + (1f / 30f) * animAttackFrame;
		nextParryTime = Time.time + (1f / 30f) * animParryFrame;

		CmdAnimAttack((int)attack);
		yield return new WaitForSeconds((1f / 30f) * animMoveFrame);
		
		prevAttack = Attacks.Right;
		overrideRotation = Vector3.zero;
	}

	[Command] void CmdAnimAttack(int number) { RpcAnimAttack(number); }
	[ClientRpc] void RpcAnimAttack(int number)
	{
		StopCoroutine("AnimAttackRoutine");
		StopCoroutine("WeaponTrailRoutine");
		StartCoroutine(AnimAttackRoutine(number));
	}
	IEnumerator AnimAttackRoutine(int number)
	{
		StartCoroutine(WeaponTrailRoutine(10));

		string animName = "attack_" + ((Attacks)number).ToString().ToLower();
		CharacterAnimation.Rewind(animName);
		ToolBox.BlendLayerAnim(animName, CharacterAnimation, (1f / 30f) * 2);
		
		yield return new WaitForSeconds(CharacterAnimation[animName].length - (1f / 30f) * 10);
		
		CharacterAnimation.Blend(animName, 0.0f, (1f / 30f) * 10);
	}

	IEnumerator WeaponTrailRoutine(int trailEndFrame)
	{
		WeaponMelee wpn = weaponMain.GetComponent<WeaponMelee>();

		wpn.weaponTrail.SetActive(true);
		yield return new WaitForSeconds((1f / 30f) * trailEndFrame);
		wpn.weaponTrail.SetActive(false);
	}

	IEnumerator DamageRoutine(int delayFrames, float radius, float height, int cone, int frames, bool clockwise)
	{
		yield return new WaitForSeconds((1f / 30f) * delayFrames);
		
		DamageSweep ds = Instantiate<DamageSweep>(DamageSweep);
		ds.radius = radius;
		ds.height = height;
		ds.cone = cone;
		ds.frames = frames;
		ds.clockwise = clockwise;
		ds.player = this;

		ds.transform.parent = this.transform;
		ds.transform.localPosition = Vector3.zero;
		ds.transform.localRotation = Quaternion.identity;
	}

	[ClientCallback] public void DamageHit(GameObject go) { CmdDamageHit(go); }
	[Command] void CmdDamageHit(GameObject go) { RpcDamgeHit(go); }
	[ClientRpc]	void RpcDamgeHit(GameObject go)	{ go.GetComponent<IDamageable>().TakeDamage(); }

	public void TakeDamage()
	{
		// Insert damage code here
	}

	void Interact()
	{
		if (!AllowMove)
			return;
		
		if (Input.GetKeyDown(KeyCode.E))
		{
			IInteractable interactable = interactables.ConsumeFirst;
			
			if (interactable != null)
				interactable.OnInteraction(this);
		}
	}
	
	[ClientCallback] public void PickupWeapon(GameObject go) { CmdPickupWeapon(go); }
	[Command] void CmdPickupWeapon(GameObject go) { RpcPickupWeapon(go); }
	
	[ClientRpc]
	void RpcPickupWeapon(GameObject go)
	{
		// Replace picked up weapon
		if (weaponMain != null)
		{
			weaponMain.transform.parent = go.transform.parent;
			weaponMain.transform.localPosition = go.transform.localPosition;
			weaponMain.transform.localRotation = go.transform.localRotation;
			ToolBox.EnableTriggers(true, weaponMain);
		}
		
		weaponMain = go;
		go.transform.parent = BoneRItem;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		ToolBox.EnableTriggers(false, go);
	}

	void Animate()
	{
		Vector3 flatPrev = new Vector3(prevPos.x, 0, prevPos.z);
		Vector3 flatNow = new Vector3(transform.position.x, 0, transform.position.z);
		float flatSpeed = Vector3.Distance(flatNow, flatPrev) / Time.deltaTime;

		float tempSpeed = flatSpeed / moveSpeed;
		ToolBox.BlendLayerAnim("run_forward", CharacterAnimation, 0.1f, tempSpeed);
		ToolBox.BlendLayerAnim("idle", CharacterAnimation, 0.1f, 1.0f);

		prevPos = transform.position;
	}

	void HitColliderSetup(Transform t)
	{
		foreach (Transform tr in t)
		{
			if (tr.name == "bone_body")
			{
				CapsuleCollider c = tr.gameObject.AddComponent<CapsuleCollider>();
				c.center = new Vector3(0f, 0.2f, 0f);
				c.radius = 0.2f;
				c.height = 0.8f;
				c.isTrigger = true;

				Colliders.Add(c);
			}
			else if (tr.name == "bone_head")
			{
				SphereCollider c = tr.gameObject.AddComponent<SphereCollider>();
				c.radius = 0.25f;
				c.isTrigger = true;

				Colliders.Add(c);
			}

			HitColliderSetup(tr);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		IInteractable interactable = other.GetComponent<IInteractable>();
		
		if (interactable != null)
			interactables.Add(interactable);
	}
	
	void OnTriggerExit(Collider other)
	{
		IInteractable interactable = other.GetComponent<IInteractable>();
		
		if (interactable != null)
			interactables.Remove(interactable);
	}

	void OnGUI()
	{
		if (!hasAuthority)
			return;
		
		if (interactables.ReadFirst != null)
		{
			GUIStyle centeredTextStyle = new GUIStyle("Label");
			centeredTextStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(0, Screen.height * 0.5f, Screen.width, Screen.height * 0.5f), interactables.ReadFirst, centeredTextStyle);
		}
	}

	// --------------------------
	// NETWORK
	// --------------------------
	internal struct State
	{
		internal bool dirtyBit;
		internal Vector3 pos;
		internal int yaw;

		internal State(bool dirtyBit, Vector3 pos, int yaw)
		{
			this.dirtyBit = dirtyBit;
			this.pos = pos;
			this.yaw = yaw;
		}
	}

	internal struct BufferedState
	{
		internal double time;
		internal State state;

		internal BufferedState(double time, State state)
		{
			this.time = time;
			this.state = state;
		}
	}

	BufferedState[] stateBuffer = new BufferedState[20];
	int stateBufferCount;

	[SyncVar(hook="SyncState")]
	State syncState;
	float syncTime;
	const float syncInterval = 0.05f;

	void NetUpdate()
	{
		if (hasAuthority)
			NetUpdateLocal();
		else
			NetUpdateRemote();
	}

	[ClientCallback]
	void NetUpdateLocal()
	{
		if (Time.time < syncTime)
			return;
		
		CmdSyncState(transform.localPosition, (int)transform.localEulerAngles.y);
		syncTime = Time.time + syncInterval;
	}

	void NetUpdateRemote()
	{
		float renderTime = (float)(Network.time - syncInterval * 2);
		Vector3 newPos = transform.localPosition;
		float newYaw = transform.localEulerAngles.y;
		
		if (stateBuffer[0].time > renderTime)
		{
			for (int i = 0; i < stateBufferCount; i++)
			{
				if (stateBuffer[i].time <= renderTime || i == stateBufferCount - 1)
				{
					BufferedState lhs = stateBuffer[i];
					BufferedState rhs = stateBuffer[Mathf.Max(i - 1, 0)];
					
					double timeDiff = rhs.time - lhs.time;
					float t = 0.0f;
					
					if (timeDiff != 0.0001)
						t = (float)((renderTime - lhs.time) / timeDiff);
					
					newPos = Vector3.Lerp(lhs.state.pos, rhs.state.pos, t);
					newYaw = Mathf.LerpAngle(lhs.state.yaw, rhs.state.yaw, t);
					
					transform.localPosition = newPos;
					transform.localEulerAngles = new Vector3(0, newYaw, 0);
					return;
				}
			}
		}
		else
		{
			float xTimeDiff = (float)(renderTime - stateBuffer[0].time);
			
			BufferedState s0 = stateBuffer[0];
			BufferedState s1 = stateBuffer[1];
			newPos = s0.state.pos;
			newYaw = s0.state.yaw;

			float timeSpent = (float)(s0.time - s1.time);
			if (xTimeDiff < syncInterval * 5 && timeSpent > 0.0f)
			{
				newPos += ((s0.state.pos - s1.state.pos) / timeSpent) * xTimeDiff;
				newYaw += ((s0.state.yaw - s1.state.yaw) / timeSpent) * xTimeDiff;
			}
			
			transform.localPosition = newPos;
			transform.localEulerAngles = new Vector3(0, newYaw, 0);
		}
	}

	[Command(channel=1)]
	void CmdSyncState(Vector3 pos, int yaw)
	{
		syncState = new State(!syncState.dirtyBit, pos, yaw);
	}
	
	void SyncState(State state)
	{
		if (hasAuthority)
			return;
		
		for (int i = stateBuffer.Length - 1; i >= 1; i--)
			stateBuffer[i] = stateBuffer[i - 1];
		
		syncState = state;
		
		stateBuffer[0] = new BufferedState(Network.time, syncState);
		stateBufferCount = Mathf.Min(stateBufferCount + 1, stateBuffer.Length);
	}
}
