﻿//https://github.com/miguel12345/UnityWireframeRenderer
//with AMG's modifications in both the renderer and the shader

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WireframeRenderer : MonoBehaviour
{
	public bool ShowBackFaces;
	public Color LineColor = Color.black;
	public bool Shaded;
	public float Scale = 1f;
	public Vector3 Center = new Vector3(0f,0f,0f);
	
	[SerializeField,HideInInspector]
	private Renderer originalRenderer;
	[SerializeField,HideInInspector]
	private Mesh processedMesh;
	[SerializeField,HideInInspector]
	private Renderer wireframeRenderer;

	[SerializeField,HideInInspector]
	private Material wireframeMaterialCull;
	[SerializeField,HideInInspector]
	private Material wireframeMaterialNoCull;

	[SerializeField,HideInInspector]
	private RendererType originalRendererType;
	
	enum RendererType
	{	
		MeshRenderer,
		SkinnedMeshRenderer
	}

	//this was Awake(), but then it didn't accept the LineColor input
	void Start()
	{
		Validate();
	}

	private void OnDestroy()
	{
		if (wireframeRenderer != null)
		{
			if (Application.isPlaying)
			{
				Destroy(wireframeRenderer.gameObject);
			}
			else
			{
				DestroyImmediate(wireframeRenderer.gameObject);	
			}
			
			wireframeRenderer = null;
		}
	}

	void Validate()
	{
		if (wireframeRenderer == null)
		{	
			Mesh originalMesh = null;

			var meshFilter = GetComponentInChildren<MeshFilter>();
			if (meshFilter != null)
			{
				originalMesh = meshFilter.sharedMesh;
				originalRendererType = RendererType.MeshRenderer;
				originalRenderer = meshFilter.GetComponent<Renderer>();
			}

			if (originalMesh == null)
			{
				var skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

				if (skinnedMeshRenderer != null)
				{
					originalMesh = skinnedMeshRenderer.sharedMesh;
					originalRendererType = RendererType.SkinnedMeshRenderer;
					originalRenderer = skinnedMeshRenderer;
				}
			}

			if (originalMesh == null)
			{	
				Debug.Log("Wireframe renderer requires a MeshRenderer or a SkinnedMeshRenderer in the same gameobject or in one of its children");
				enabled = false;
				return;
			}

			processedMesh = GetProcessedMesh(originalMesh);

			if (processedMesh == null)
			{
				return;
			}
			
			CreateWireframeRenderer();
			CreateMaterials();
		}
		
		OnValidate();
	}

	void CreateWireframeRenderer()
	{
		var wireframeGO = new GameObject("Wireframe renderer");
		wireframeGO.transform.SetParent(originalRenderer.transform);
		wireframeGO.transform.localPosition = Vector3.zero;
		wireframeGO.transform.localRotation = Quaternion.identity;

		if (originalRendererType == RendererType.MeshRenderer)
		{
			wireframeGO.AddComponent<MeshFilter>().mesh = processedMesh;
			wireframeRenderer = wireframeGO.AddComponent<MeshRenderer>();
		}
		else
		{
			var originalSkinnedMeshRenderer = (SkinnedMeshRenderer) originalRenderer;
			var wireframeSkinnedMeshRenderer = wireframeGO.AddComponent<SkinnedMeshRenderer>();
			wireframeSkinnedMeshRenderer.bones = originalSkinnedMeshRenderer.bones;
			wireframeSkinnedMeshRenderer.sharedMesh = processedMesh;
			wireframeRenderer = wireframeSkinnedMeshRenderer;
		}
	}

	void OnValidate()
	{
		if (wireframeRenderer == null) return;
		UpdateWireframeRendererMaterial();
		UpdateLineColor();
		UpateShaded();
	}

	void CreateMaterials()
	{
		if (wireframeMaterialNoCull == null)
		{
			wireframeMaterialNoCull = CreateWireframeMaterial(false);
			wireframeMaterialCull = CreateWireframeMaterial(true);
		}
	}

	void UpdateWireframeRendererMaterial()
	{
		wireframeRenderer.material = ShowBackFaces ? wireframeMaterialNoCull:wireframeMaterialCull;
	}

	void UpdateLineColor()
	{	
		wireframeRenderer.sharedMaterial.SetColor("_LineColor",LineColor);
		wireframeRenderer.sharedMaterial.SetFloat("_ScaleSize",Scale);
		wireframeRenderer.sharedMaterial.SetVector("_Center",Center);
	}

	void UpateShaded()
	{
		originalRenderer.enabled = Shaded;
	}

	Material CreateWireframeMaterial(bool cull)
	{
		var shaderLastName = cull ? "Cull" : "NoCull";
		var shader = Shader.Find("Wireframe/"+shaderLastName);
		var material = new Material(shader);
		return material;
	}

	private void OnEnable()
	{
		if (wireframeRenderer != null)
		{
			originalRenderer.enabled = false;
			wireframeRenderer.enabled = true;
			
			OnValidate();
		}
	}

	private void OnDisable()
	{
		if (wireframeRenderer != null)
		{
			originalRenderer.enabled = true;
			wireframeRenderer.enabled = false;
		}
	}

	Mesh GetProcessedMesh(Mesh mesh)
	{
		var maximumNumberOfVertices = 65534; //Since unity uses a 16-bit indices, not sure if this is still the case. http://answers.unity3d.com/questions/255405/vertex-limit.html
		var meshTriangles = mesh.triangles;
		var meshVertices = mesh.vertices;
		var meshNormals = mesh.normals;
		var boneWeights = mesh.boneWeights;
		
		var numberOfVerticesRequiredForTheProcessedMesh = meshTriangles.Length;
		if (numberOfVerticesRequiredForTheProcessedMesh > maximumNumberOfVertices)
		{	
			Debug.LogError("Wireframe renderer can't safely create the processed mesh it needs because the resulting number of vertices would surpass unity vertex limit!");
			return null;
		}

		var processedMesh = new Mesh();
		
		var processedVertices = new Vector3[numberOfVerticesRequiredForTheProcessedMesh];
		var processedUVs = new Vector2[numberOfVerticesRequiredForTheProcessedMesh];
		var processedTriangles = new int[meshTriangles.Length];
		var processedNormals = new Vector3[numberOfVerticesRequiredForTheProcessedMesh];
		var processedBoneWeigths = new BoneWeight[numberOfVerticesRequiredForTheProcessedMesh]; //The size of the array is either the same as vertexCount or empty.
		
		for (var i = 0; i < meshTriangles.Length; i+=3)
		{
			processedVertices[i] = meshVertices[meshTriangles[i]]*Scale;
			processedVertices[i+1] = meshVertices[meshTriangles[i+1]]*Scale;
			processedVertices[i+2] = meshVertices[meshTriangles[i+2]]*Scale;
			
			processedUVs[i] = new Vector2(0f,0f);
			processedUVs[i+1] = new Vector2(1f,0f);
			processedUVs[i+2] = new Vector2(0f,1f);

			processedTriangles[i] = i;
			processedTriangles[i+1] = i+1;
			processedTriangles[i+2] = i+2;

			processedNormals[i] = meshNormals[meshTriangles[i]];
			processedNormals[i+1] = meshNormals[meshTriangles[i+1]];
			processedNormals[i+2] = meshNormals[meshTriangles[i+2]];

			//if (processedBoneWeigths.Length > 0)
			if (boneWeights.Length > 0)
			{
				processedBoneWeigths[i] = boneWeights[meshTriangles[i]];
				processedBoneWeigths[i+1] = boneWeights[meshTriangles[i+1]];
				processedBoneWeigths[i+2] = boneWeights[meshTriangles[i+2]];
			}
		}

		processedMesh.vertices = processedVertices;
		processedMesh.uv = processedUVs;
		processedMesh.triangles = processedTriangles;
		processedMesh.normals = processedNormals;
		processedMesh.bindposes = mesh.bindposes;
		processedMesh.boneWeights = processedBoneWeigths;

		return processedMesh;
	}

}
