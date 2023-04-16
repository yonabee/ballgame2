using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class MovingSphere : MonoBehaviour 
{
    [SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 100f)]
	float jumpPower = 20f;

	[SerializeField, Range(0f, 100f)]
	float wallJumpPower = 30f;

	[SerializeField, Range(0, 10)]
	int airJumpMaxFrames = 10;

    [SerializeField, Range(0f, 90f)]
	float maxGroundAngle = 25f;

	[SerializeField, Range(0, 90)]
	float wallJumpVerticalCompensation = 75;

    [SerializeField, Range(0f, 100f)]
	float maxSnapSpeed = 100f;

    [SerializeField, Min(0f)]
	float probeDistance = 1f;

    [SerializeField]
	LayerMask probeMask = -1, waterMask = 0;

	[SerializeField]
	Material normalMaterial = default, swimmingMaterial = default;

	[SerializeField]
	Transform playerInputSpace = default;

	[SerializeField, Range(0f, 10f)]
	float gravityAssist = 1f;

	[SerializeField, Range(1f, 10f)]
	float boostFactor = 1.6f;	

	[SerializeField]
	float submergenceOffset = 0.5f;

	[SerializeField, Min(0.1f)]
	float submergenceRange = 1f;

	[SerializeField, Range(0f, 10f)]
	float waterDrag = 1f;

	[SerializeField, Min(0f)]
	float buoyancy = 1f;

	bool desiredBoost;
    bool desiredJump;
    int jumpFrame = 0;
    float minGroundDotProduct;
    int groundContactCount, wallContactCount;
    int stepsSinceLastGrounded, stepsSinceLastJump;

	Vector3 upAxis, rightAxis, forwardAxis;
    Vector3 velocity, desiredVelocity;
    Vector3 contactNormal, steepNormal;
    Rigidbody body;
	MeshRenderer meshRenderer;
	Vector2 playerInput;

	bool OnGround => groundContactCount > 0;
    bool OnWall => wallContactCount > 0;
	bool Grounded => OnGround || OnWall;
	bool InWater => submergence > 0f;

	float submergence;

	void OnValidate()
    {
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
	}

	void Awake() 
    {
		body = GetComponent<Rigidbody>();
		meshRenderer = GetComponent<MeshRenderer>();
		body.useGravity = false;
        OnValidate();
	}

	void OnMove(InputValue value)
	{
		playerInput = value.Get<Vector2>();
	}

	void OnJump() 
	{
		desiredJump = true;
		jumpFrame = 0;
	}

	void OnBoost()
	{
		desiredBoost = true;
	}

    void Update() 
    {
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		if (playerInputSpace)
		{
			rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
		}
		else 
		{
			rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
		}

		desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        meshRenderer.material = InWater ? swimmingMaterial : normalMaterial;
	}  

    void FixedUpdate() 
    {
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
	
		UpdateState();

		if (InWater) 
		{
			velocity *= 1f - waterDrag * submergence * Time.deltaTime;
		}

        AdjustVelocity();

        if (desiredJump) 
		{
			Jump(gravity);
		}

		if (desiredBoost)
		{
			if (desiredJump)
			{
				velocity.y *= boostFactor;
			}
			else if (OnGround)
			{
				velocity.x *= boostFactor;
				velocity.z *= boostFactor;
			}
		}

		if (InWater)
		{
			velocity += gravity * ((1f - buoyancy * submergence) * Time.deltaTime);
		}
		else if (OnGround && velocity.sqrMagnitude < 0.01f) 
		{
			velocity +=
				contactNormal *
				(Vector3.Dot(gravity, contactNormal) * Time.deltaTime);
		}
		else
		{
			velocity += gravity * Time.deltaTime;
		}

		body.velocity = velocity;

        ClearState();
	}

    void UpdateState() 
    {
		velocity = body.velocity;
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
    
		if (OnGround || SnapToGround() || CheckSteepContacts()) 
        {
            stepsSinceLastGrounded = 0;
    
			if (groundContactCount > 1) 
            {
				contactNormal.Normalize();
			}
		}
        else 
        {
			contactNormal = upAxis;
		}
	}

    void ClearState() 
    {
		groundContactCount = wallContactCount = 0;
		contactNormal = steepNormal = Vector3.zero;
		submergence = 0f;
	}

    void Jump(Vector3 gravity) 
    {
		if (!Grounded) {
			jumpFrame += 1;
			if (jumpFrame > airJumpMaxFrames) {
				jumpFrame = 0;
				desiredJump = false;
			}
			if (!desiredBoost) {
				return;
			}		
		}

		Vector3 jumpDirection;

		jumpDirection = OnGround
			? contactNormal
			: Vector3.RotateTowards(steepNormal, upAxis, Mathf.Deg2Rad * wallJumpVerticalCompensation, 0.0f);
		desiredJump = false;

        stepsSinceLastJump = 0;
        jumpDirection = (jumpDirection + upAxis).normalized;
    
		float power = OnGround ? jumpPower : wallJumpPower;
		if (desiredBoost) {
			power *= boostFactor;
			desiredBoost = false;
		}
		float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * power);
		float alignedSpeed = Vector3.Dot(velocity, jumpDirection);

		if (alignedSpeed > 0f)  
        {
			jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
		}

		velocity += jumpDirection * jumpSpeed;
	}

    bool SnapToGround()
    {
		if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2 || InWater) 
        {
			return false;
		}
        float speed = velocity.magnitude;
		if (speed > maxSnapSpeed) 
        {
			return false;
		}
        if (!Physics.Raycast(
			body.position,
			-upAxis,
			out RaycastHit hit,
			probeDistance,
			probeMask, 
			QueryTriggerInteraction.Ignore))
        {
			return false;
		}
		float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < minGroundDotProduct) 
        {
			return false;
		}
		groundContactCount = 1;
		contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f) 
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
		return true;
	}

    bool CheckSteepContacts() 
    {
		if (wallContactCount > 1) {
			steepNormal.Normalize();
			float upDot = Vector3.Dot(upAxis, steepNormal);
			if (upDot >= minGroundDotProduct) {
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}

    void AdjustVelocity() 
    {
		bool moving = desiredVelocity.x > 0 || desiredVelocity.z > 0;
		Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
		Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);
		Vector3 yAxis;
		if (InWater) 
		{
			yAxis = Vector3.zero;
		}
		else if (OnGround && moving) 
		{
			yAxis = -upAxis * gravityAssist;
		}
		else 
		{
			yAxis = -upAxis * gravityAssist * 2;
		}

		if (OnGround && moving && desiredBoost)
		{
			desiredVelocity.x *= boostFactor;
			desiredVelocity.z *= boostFactor;
			desiredBoost = false;
		}

        float currentX = Vector3.Dot(velocity, xAxis);
		float currentZ = Vector3.Dot(velocity, zAxis);

		float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
		float maxSpeedChange = acceleration * Time.deltaTime;

		float newX =
			Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
		float newZ =
			Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + yAxis + zAxis * (newZ - currentZ);
	}

	void OnCollisionEnter(Collision collision) {
        EvaluateCollision(collision);
	}

    void OnCollisionStay(Collision collision) {
        EvaluateCollision(collision);
	}

    void EvaluateCollision (Collision collision) 
	{
		for (int i = 0; i < collision.contactCount; i++) 
		{
			Vector3 normal = collision.GetContact(i).normal;
    
			float upDot = Vector3.Dot(upAxis, normal);
            if (upDot >= minGroundDotProduct) 
            {
				groundContactCount += 1;
				contactNormal += normal;
			} 
            else if (upDot > -0.01f) 
            {
				wallContactCount += 1;
				steepNormal += normal;
			}
		}
	}

	void OnTriggerEnter (Collider other)
	{
		if ((waterMask & (1 << other.gameObject.layer)) != 0)
		{
			EvaluateSubmergence();
		}
	}

	void OnTriggerStay (Collider other)
	{
		if ((waterMask & (1 << other.gameObject.layer)) != 0)
		{
			EvaluateSubmergence();
		}
	}

	void EvaluateSubmergence () 
	{
		if (Physics.Raycast(
			body.position + upAxis * submergenceOffset,
			-upAxis, out RaycastHit hit, submergenceRange + 1f,
			waterMask, QueryTriggerInteraction.Collide
		)) {
			submergence = 1f - hit.distance / submergenceRange;
		} else {
			submergence = 1f;
		}
	}

    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
	{
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
	}
}