//Put this script on your blue cube.

using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


public class PushAgentBasic_pusher : Agent
{
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    public GameObject area;
    public bool is_training = false;
    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;

    PushBlockSettings m_PushBlockSettings;
    public float[] last_action;
    /// <summary>
    /// The goal to push the block to.
    /// </summary>
    public GameObject goal;

    /// <summary>
    /// The block to be pushed to the goal.
    /// </summary>
    public GameObject block;

    /// <summary>
    /// Detects when the block touches the goal.
    /// </summary>
    [HideInInspector]
    public GoalDetect_pusher goalDetect;

    public bool useVectorObs;

    Rigidbody m_BlockRb;  //cached on initialization
    Rigidbody m_AgentRb;  //cached on initialization
    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    EnvironmentParameters m_ResetParams;

    Vector3 InitialLocation; 
    public override void CollectObservations(VectorSensor sensor)
    {
        float x = (goal.transform.position.x - transform.position.x);
        float z = (goal.transform.position.z - transform.position.z);
        float r = Mathf.Sqrt(x * x + z * z);

        sensor.AddObservation(x/r);
        sensor.AddObservation(z/r);
        //float dis = (goal.transform.position - block.transform.position).magnitude;
        // Debug.Log(dis);
        //AddReward(-dis/10000);
    }
    void Awake()
    {
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
    }

    public override void Initialize()
    {
        
        if (is_training){
            goalDetect = block.GetComponent<GoalDetect_pusher>();
            goalDetect.agent = this;
            // Cache the agent rigidbody
            m_AgentRb = GetComponent<Rigidbody>();
            // Cache the block rigidbody
            m_BlockRb = block.GetComponent<Rigidbody>();
            // Get the ground's bounds
            areaBounds = ground.GetComponent<Collider>().bounds;
            // Get the ground renderer so we can change the material when a goal is scored
            m_GroundRenderer = ground.GetComponent<Renderer>();
            // Starting material
            m_GroundMaterial = m_GroundRenderer.material;

            m_ResetParams = Academy.Instance.EnvironmentParameters;

            InitialLocation = transform.position;

            SetResetParameters();
        }
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Called when the agent moves the block into the goal.
    /// </summary>
    public void ScoredAGoal()
    {
        if(is_training){
            // We use a reward of 5.
            AddReward(5f);

            // By marking an agent as done AgentReset() will be called automatically.
            EndEpisode();

            // Swap ground material for a bit to indicate we scored.
            StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));
        }
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(float[] act)
    {   
        // Debug.Log("pusher move");
        if (is_training){
            var dirToGo = Vector3.zero;
            var rotateDir = Vector3.zero;

            var action = Mathf.FloorToInt(act[0]);

            switch (action)
            {
                case 1:
                    dirToGo = transform.forward * 1f;
                    break;
                case 2:
                    dirToGo = transform.forward * -1f;
                    break;
                case 3:
                    rotateDir = transform.up * 1f;
                    break;
                case 4:
                    rotateDir = transform.up * -1f;
                    break;
                case 5:
                    dirToGo = transform.right * -0.75f;
                    break;
                case 6:
                    dirToGo = transform.right * 0.75f;
                    break;
            }
            transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
            m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
                ForceMode.VelocityChange);
        }
        last_action = act;
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(float[] vectorAction)
    {
        // Move the agent using the action.
        MoveAgent(vectorAction);

        // Penalty given each step to encourage agent to finish task quickly.
        if (is_training){
            AddReward(-1f / MaxStep);
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = 0;
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            actionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            actionsOut[0] = 2;
        }
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();

        // Reset block velocity back to zero.
        m_BlockRb.velocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        m_BlockRb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if(is_training){
            var rotation = Random.Range(0, 4);
            var rotationAngle = rotation * 90f;
            //area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

            ResetBlock();
            var randomSpawnPos = Vector3.zero;
            var found = false;
            while (!found){
                var x_det = Random.Range(-5,5);
                var y_det = Random.Range(-5,5);  
                randomSpawnPos = block.transform.position + new Vector3(x_det,0,y_det);
                if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
                    {
                        found = true;
                    }
            }
            Debug.Log(is_training);
            transform.position = randomSpawnPos;
            m_AgentRb.velocity = Vector3.zero;
            m_AgentRb.angularVelocity = Vector3.zero;

            SetResetParameters();
        }
    }

    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = m_ResetParams.GetWithDefault("dynamic_friction", 0);
        groundCollider.material.staticFriction = m_ResetParams.GetWithDefault("static_friction", 0);
    }

    public void SetBlockProperties()
    {
        var scale = m_ResetParams.GetWithDefault("block_scale", 2);
        //Set the scale of the block
        m_BlockRb.transform.localScale = new Vector3(scale, 0.75f, scale);

        // Set the drag of the block
        m_BlockRb.drag = m_ResetParams.GetWithDefault("block_drag", 0.5f);
    }

    void SetResetParameters()
    {
        SetGroundMaterialFriction();
        SetBlockProperties();
    }
}
