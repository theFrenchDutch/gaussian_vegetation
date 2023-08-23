using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

[ExecuteInEditMode]
public class ProceduralTree : MonoBehaviour
{
	// Variables
	public static ProceduralTree Instance;
	public Camera gameCamera;
	public bool resetButton = false;
	public bool randSeed = false;
	public int seed = 123;
	public int treeDepth = 4;
	public int treeNAryMin = 2;
	public int treeNAryMax = 4;
	public float rootLength = 10.0f;
	public float lengthFactor = 0.66f;
	public float directionInfluenceFactor = 1.0f;
	public int maxBranchRenderDepth = 5;
	public float trunkSize = 1.0f;
	public float leafSizeFactor = 4.0f;
	public bool doDynamicLOD = false;
	public float desiredPixelSize = 4.0f;
	public bool doDynamicWind = false;
	public float baseWindPower = 3.0f;
	public float windDepthFactor = 1.0f; 
	public GameObject branchPrefab;
	public GameObject leafPrefab;
	private TreeNode root;
	private Camera currentCamera;
	private float tanFOV;




	// Unity Callbacks
	void Awake()
	{
		Instance = this;
	}

	void OnEnable()
	{
		if (Application.isPlaying == false && SceneView.GetAllSceneCameras().Length > 0)
		{
			currentCamera = SceneView.GetAllSceneCameras()[0];
			tanFOV = 2.0f * Mathf.Tan(currentCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		}
		else
		{
			currentCamera = gameCamera;
			tanFOV = 2.0f * Mathf.Tan(currentCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		}
		InitTree();
	}

	void OnDisable()
	{
		if (root != null && root.nodeObject != null)
			DestroyImmediate(root.nodeObject);
	}

	void Update()
	{
		if (Application.isPlaying == false && SceneView.GetAllSceneCameras().Length > 0)
		{
			currentCamera = SceneView.GetAllSceneCameras()[0];
			tanFOV = 2.0f * Mathf.Tan(currentCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		}
		else
		{
			currentCamera = gameCamera;
			tanFOV = 2.0f * Mathf.Tan(currentCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		}

		if (resetButton == true)
		{
			resetButton = false;
			if (root != null && root.nodeObject != null)
				DestroyImmediate(root.nodeObject);
			InitTree();
		}
		if (doDynamicLOD == true)
		{
			TreeDynamicLOD();
		}
		if (doDynamicWind == true)
		{
			ProceduralWindWholeTreeRecursive(root);
		}
	} 




	// Tree Handling
	public void InitTree()
	{
		if (Instance == null)
			Instance = this;

		if (randSeed == true)
		{
			UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
			seed = (int)(UnityEngine.Random.value * 10000.0f);
		}

		root = new TreeNode(0, seed, null, Vector3.up, rootLength);
		root.UpdateNodeObject();
		root.nodeObject.transform.parent = this.transform;
		if (doDynamicLOD == false)
			for (int i = 0; i < treeDepth; i++)
				SubdivideWholeTreeOnce();

		if (currentCamera.GetComponent<OrbitCamera>() != null)
			currentCamera.GetComponent<OrbitCamera>().offset = new Vector3(0, rootLength * 1.5f, 0);
	}

	public List<TreeNode> FindLeafNodes()
	{
		List<TreeNode> leafNodes = new List<TreeNode>();
		if (root == null)
			return leafNodes;

		FindLeafNodesRecursive(ref leafNodes, root);
		return leafNodes;
	}

	public void FindLeafNodesRecursive(ref List<TreeNode> leaves, TreeNode current)
	{
		if (current.children.Count == 0)
			leaves.Add(current);
		else
		{
			for (int i = 0; i < current.children.Count; i++)
				FindLeafNodesRecursive(ref leaves, current.children[i]);
		}
	}

	public void SubdivideWholeTreeOnce()
	{
		List<TreeNode> leafNodes = FindLeafNodes();
		for (int i = 0; i < leafNodes.Count; i++)
			SubdivideNode(leafNodes[i]);
	}

	public void MergeWholeTreeOnce()
	{
		List<TreeNode> leafNodes = FindLeafNodes();
		for (int i = 0; i < leafNodes.Count; i++)
		{
			if (leafNodes[i] != null)
				MergeNode(leafNodes[i].parent);
		}
	}

	public void SubdivideNode(TreeNode node)
	{
		UnityEngine.Random.InitState(node.seed);
		node.children = new List<TreeNode>();
		int randN = treeNAryMin + (int)(UnityEngine.Random.value * treeNAryMax);
		for (int i = 0; i < randN; i++)
		{
			Vector3 newDirection = Vector3.Normalize(node.direction * directionInfluenceFactor + UnityEngine.Random.onUnitSphere);
			if (i == 0 && UnityEngine.Random.value > 0.5f)
				newDirection = node.direction;
			int newSeed = (int)(UnityEngine.Random.value * int.MaxValue);
			TreeNode child = new TreeNode(node.depth + 1, newSeed, node, newDirection, node.length * lengthFactor);
			node.children.Add(child);
			child.UpdateNodeObject();
		}
		node.UpdateNodeObject();
	}

	public void MergeNode(TreeNode node)
	{
		for (int i = 0; i < node.children.Count; i++)
		{
			DestroyImmediate(node.children[i].nodeObject);
		}
		node.children = new List<TreeNode>();
		node.UpdateNodeObject();
	}

	public void TreeDynamicLOD()
	{
		List<TreeNode> leafNodes = FindLeafNodes();
		for (int i = 0; i < leafNodes.Count; i++)
		{
			if (leafNodes[i] == null || leafNodes[i].nodeObject == null || leafNodes[i].leafRenderer == null)
				continue;

			float pixelSize = ComputeNodePixelSize(leafNodes[i]);
			if (pixelSize > desiredPixelSize && leafNodes[i].depth < treeDepth)
				SubdivideNode(leafNodes[i]);

			if (leafNodes[i].parent != null)
			{
				float parentPixelSize = ComputeNodePixelSize(leafNodes[i].parent);
				if (parentPixelSize < desiredPixelSize)
					MergeNode(leafNodes[i].parent);
			}
		}
	}

	public float ComputeNodePixelSize(TreeNode node)
	{
		float pixelSize = (node.leafRenderer.transform.localScale.x * currentCamera.pixelHeight) / (Vector3.Distance(currentCamera.transform.position, node.leafRenderer.transform.position) * tanFOV);
		return pixelSize;
	}

	float hash11(float p)
	{
		p = math.frac(p * 0.1031f);
		p *= p + 19.19f;
		p *= p + p;
		return math.frac(p);
	}

	public void ProceduralWindWholeTreeRecursive(TreeNode current)
	{
		Vector3 tipPosition = current.nodeObject.transform.position + current.direction * current.length;
		float prob = hash11(9375.264f * current.seed);
		float prob2 = hash11(1212.958f * current.seed);
		float prob3 = hash11(4985.163f * current.seed);
		float angleWind = baseWindPower
			* (math.sin(Time.realtimeSinceStartup * 2.78f * (1.0f + current.depth) * prob))
			* (1.0f + current.depth * windDepthFactor)
			* math.cos(Time.realtimeSinceStartup * 0.46f + tipPosition.x)
			* math.sin(Time.realtimeSinceStartup * 0.52f + tipPosition.y)
			* math.sin(Time.realtimeSinceStartup * 0.75f + tipPosition.z)
			* math.pow(tipPosition.y, 1.0f) / (rootLength * 1.0f);
		float angleWind2 = baseWindPower
			* (math.cos(Time.realtimeSinceStartup * 2.35f * (1.0f + current.depth) * prob2))
			* (1.0f + current.depth * windDepthFactor)
			* math.sin(Time.realtimeSinceStartup * 0.56f + tipPosition.x)
			* math.cos(Time.realtimeSinceStartup * 0.34f + tipPosition.y + 0.5f)
			* math.cos(Time.realtimeSinceStartup * 0.89f + tipPosition.z)
			* math.pow(tipPosition.y, 1.0f) / (rootLength * 1.0f);
		float angleWind3 = baseWindPower
			* (math.cos(Time.realtimeSinceStartup * 1.65f * (1.0f + current.depth) * prob3))
			* (1.0f + current.depth * windDepthFactor)
			* math.cos(Time.realtimeSinceStartup * 0.36f + tipPosition.x)
			* math.sin(Time.realtimeSinceStartup * 0.25f + tipPosition.y)
			* math.sin(Time.realtimeSinceStartup * 0.45f + tipPosition.z)
			* math.pow(tipPosition.y, 1.0f) / (rootLength * 1.0f);
		Vector3 baseEuler = current.baseLocalRotation.eulerAngles;
		baseEuler.x += angleWind;
		baseEuler.y += angleWind2;
		baseEuler.z += angleWind3;
		current.nodeObject.transform.localRotation = Quaternion.Euler(baseEuler);

		if (current.children.Count > 0)
		{
			for (int i = 0; i < current.children.Count; i++)
				ProceduralWindWholeTreeRecursive(current.children[i]);
		}
	}


	// Structures
	public class TreeNode
	{
		public int depth;
		public int seed;
		public TreeNode parent;
		public List<TreeNode> children;

		public float length;
		public Vector3 direction;
		public GameObject nodeObject;
		public GameObject branchRenderer;
		public GameObject leafRenderer;
		public Quaternion baseLocalRotation;

		public TreeNode(int d, int s, TreeNode p, Vector3 dir, float l)
		{
			depth = d;
			seed = s;
			parent = p;
			direction = dir;
			length = l;
			children = new List<TreeNode>();
		}

		public void UpdateNodeObject()
		{
			if (nodeObject == null)
				nodeObject = new GameObject("TreeNode");
			if (parent != null)
				nodeObject.transform.parent = parent.nodeObject.transform;
			nodeObject.isStatic = true;
			nodeObject.hideFlags = HideFlags.DontSaveInEditor;
			if (branchRenderer == null)
			{
				branchRenderer = Instantiate(ProceduralTree.Instance.branchPrefab);
				branchRenderer.transform.parent = nodeObject.transform;
				branchRenderer.isStatic = true;
				branchRenderer.hideFlags = HideFlags.DontSaveInEditor;
			}
			if (leafRenderer == null)
			{
				leafRenderer = Instantiate(ProceduralTree.Instance.leafPrefab);
				leafRenderer.transform.parent = nodeObject.transform;
				leafRenderer.isStatic = true;
				leafRenderer.hideFlags = HideFlags.DontSaveInEditor;
			}

			// Is Leaf
			if (children.Count == 0)
			{
				leafRenderer.SetActive(true);
			}
			else // Is Branch
			{
				leafRenderer.SetActive(false);
			}

			if (depth > ProceduralTree.Instance.maxBranchRenderDepth)
				branchRenderer.SetActive(false);

			UpdateNodeObjectTransform();
		}

		public void UpdateNodeObjectTransform()
		{
			if (parent != null)
				nodeObject.transform.position = parent.nodeObject.transform.position + parent.length * parent.direction;
			else
				nodeObject.transform.position = ProceduralTree.Instance.transform.position;
			nodeObject.transform.LookAt(nodeObject.transform.position + direction);
			baseLocalRotation = nodeObject.transform.localRotation;

			leafRenderer.transform.position = nodeObject.transform.position + direction * length;
			leafRenderer.transform.localScale = Vector3.one * ProceduralTree.Instance.leafSizeFactor / math.pow(2.0f, depth);

			branchRenderer.transform.localRotation = Quaternion.Euler(90, 0, 0);
			branchRenderer.transform.localScale = new Vector3(ProceduralTree.Instance.trunkSize / math.pow(2.0f, depth), length, ProceduralTree.Instance.trunkSize / math.pow(2.0f, depth));
			branchRenderer.transform.position = nodeObject.transform.position + direction * length / 2.0f;
			if (depth == ProceduralTree.Instance.maxBranchRenderDepth && ProceduralTree.Instance.maxBranchRenderDepth < ProceduralTree.Instance.treeDepth)
			{
				branchRenderer.transform.localScale = new Vector3(ProceduralTree.Instance.trunkSize / math.pow(2.0f, depth), length * 1.5f, ProceduralTree.Instance.trunkSize / math.pow(2.0f, depth));
				branchRenderer.transform.position = nodeObject.transform.position + direction * length * 0.75f;
			}
		}
	}
}
