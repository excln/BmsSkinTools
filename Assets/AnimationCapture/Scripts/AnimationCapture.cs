using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AnimationCapture : MonoBehaviour
{

	[SerializeField]
	string fileName;

	[SerializeField]
	int framerate = 60;

	[SerializeField]
	int frameCount = 16;

	[SerializeField]
	Vector2 sourcePosition = new Vector2(0.5f, 0.5f);

	[SerializeField]
	int sourceHeight = 256;

	[SerializeField]
	float sourceAspectRatio = 1.0f;

	[SerializeField]
	Vector2 sourcePivot = new Vector2(0.5f, 0.5f);

	[SerializeField]
	Shader shader;

	[SerializeField]
	int textureWidth = 2048;

	[SerializeField]
	int textureHeight = 128;

	[SerializeField]
	int cellWidth = 128;

	[SerializeField]
	int cellHeight = 128;

	[SerializeField]
	List<GameObject> gameObjectsToDisable;

	Camera targetCamera;
	Material material;
	RenderTexture texture;
	RenderTexture texture2;

	int _OrgTexID;
	int _MainTexID;
	int _SrcXID;
	int _SrcYID;
	int _SrcWID;
	int _SrcHID;
	int _DstXID;
	int _DstYID;
	int _DstWID;
	int _DstHID;

	int frame;
	bool recording;

	void OnEnable()
	{
		System.IO.Directory.CreateDirectory("Capture");
		Time.captureFramerate = framerate;
		recording = false;
		frame = -1;
		targetCamera = GetComponent<Camera>();
		material = new Material(shader);
		material.hideFlags = HideFlags.HideAndDontSave;
		texture = new RenderTexture(textureWidth, textureHeight, 0);
		texture.hideFlags = HideFlags.HideAndDontSave;
		texture.Create();
		texture2 = new RenderTexture(textureWidth, textureHeight, 0);
		texture2.hideFlags = HideFlags.HideAndDontSave;
		texture2.Create();

		_OrgTexID = Shader.PropertyToID("_OrgTex");
		_MainTexID = Shader.PropertyToID("_MainTex");
		_SrcXID = Shader.PropertyToID("_SrcX");
		_SrcYID = Shader.PropertyToID("_SrcY");
		_SrcWID = Shader.PropertyToID("_SrcWidth");
		_SrcHID = Shader.PropertyToID("_SrcHeight");
		_DstXID = Shader.PropertyToID("_DstX");
		_DstYID = Shader.PropertyToID("_DstY");
		_DstWID = Shader.PropertyToID("_DstWidth");
		_DstHID = Shader.PropertyToID("_DstHeight");
	}

	void OnDisable()
	{
		if (texture != null)
		{
			DestroyImmediate(texture);
		}
		if (material != null)
		{
			DestroyImmediate(material);
		}
		material = null;
	}

	public void StartRecording()
	{
		foreach (var obj in gameObjectsToDisable)
		{
			obj.SetActive(false);
		}

		Time.captureFramerate = framerate;

		// 発火まで1フレーム飛ばす
		frame = -1;
		recording = true;
		Debug.Log("Recording Start");
	}

	public void EndRecording()
	{
		RenderTexture original = RenderTexture.active;
		RenderTexture.active = texture;
		Texture2D tex2d = new Texture2D(texture.width, texture.height);
		tex2d.ReadPixels(new Rect(0, 0, tex2d.width, tex2d.height), 0, 0);
		RenderTexture.active = original;
		var file = System.IO.File.Create("Capture/" + fileName + ".png");
		var png = tex2d.EncodeToPNG();
		file.Write(png, 0, png.Length);

		recording = false;
		frame = -1;
		Debug.Log("Recording End");
		Time.captureFramerate = -1;
		foreach (var obj in gameObjectsToDisable)
		{
			obj.SetActive(true);
		}
	}

	void Update()
	{
		if (recording)
		{
			frame++;
			if (frame >= frameCount)
			{
				EndRecording();
			}
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (frame >= 0)
		{
			int columns = textureWidth / cellWidth;
			int ix = frame % columns;
			int iy = frame / columns;
			float ratioW = (float)cellWidth / textureWidth;
			float ratioH = (float)cellHeight / textureHeight;

			float dstX = (ix + 0.5f) * ratioW;
			float dstY = 1.0f - (iy + 0.5f) * ratioH;

			Graphics.Blit(texture, texture2);

			material.SetTexture(_OrgTexID, texture2);
			material.SetFloat(_SrcXID, sourcePosition.x - (sourcePivot.x - 0.5f) * sourceAspectRatio * sourceHeight / source.width);
			material.SetFloat(_SrcYID, sourcePosition.y - (sourcePivot.y - 0.5f) * sourceHeight / source.height);
			material.SetFloat(_SrcWID, sourceAspectRatio * sourceHeight / source.width);
			material.SetFloat(_SrcHID, (float)sourceHeight / source.height);
			material.SetFloat(_DstXID, dstX);
			material.SetFloat(_DstYID, dstY);
			material.SetFloat(_DstWID, ratioW);
			material.SetFloat(_DstHID, ratioH);
			
			texture.MarkRestoreExpected();

			Graphics.Blit(source, texture, material);
		}
		Graphics.Blit(source, destination);
	}

	void OnDrawGizmos()
	{
		if (recording)
			return;
		
		if (targetCamera == null)
		{
			targetCamera = GetComponent<Camera>();
		}

		Gizmos.color = Color.yellow;
		float far = targetCamera.farClipPlane;
		float near = targetCamera.nearClipPlane;
		float depth = 0.5f * (far + near);
		if (targetCamera.orthographic)
		{
			var center = targetCamera.ScreenToWorldPoint(new Vector3(
				             sourcePosition.x * targetCamera.pixelWidth - (sourcePivot.x - 0.5f) * sourceAspectRatio * sourceHeight,
				             sourcePosition.y * targetCamera.pixelHeight - (sourcePivot.y - 0.5f) * sourceHeight,
				             depth));
			var point = targetCamera.ScreenToWorldPoint(new Vector3(targetCamera.pixelWidth * 0.5f + sourceAspectRatio * sourceHeight, targetCamera.pixelHeight * 0.5f + sourceHeight, 0f));
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.DrawWireCube(center, new Vector3(point.x, point.y, (far - depth) * 2));
		}
		else
		{
			// near != 0 のときのDrawFrustumの挙動がおかしいので対策をする
			far = far - (far - near) * 0.01f;
			near = 0f;
			float heightRatio = (float)sourceHeight / targetCamera.pixelHeight;
			float fov = Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(Mathf.Deg2Rad * targetCamera.fieldOfView) * heightRatio);
			Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, new Vector3(1f, 1f, 1f));
			Gizmos.DrawFrustum(Vector3.zero, fov, far, near, sourceAspectRatio);
		}
	}
}
