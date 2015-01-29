using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RelationsInspector.Backend.ObjectDependency
{
	public static class YamlClassIdDictionary
	{
		public const int ClassIdGameObject = 1;
		public const int ClassIdTransform = 4;
		public const int ClassIdScene = 29;

		public static string GetClassName(int id)
		{
			string name = null;
			dict.TryGetValue(id, out name);
			return name;
		}

		// http://docs.unity3d.com/Manual/ClassIDReference.html
		static Dictionary<int, string> dict = new Dictionary<int, string>()
		{
			{1, "GameObject"},
			{2, "Component"},
			{3, "LevelGameManager"},
			{4, "Transform"},
			{5, "TimeManager"},
			{6, "GlobalGameManager"},
			{8, "Behaviour"},
			{9, "GameManager"},
			{11, "AudioManager"},
			{12, "ParticleAnimator"},
			{13, "InputManager"},
			{15, "EllipsoidParticleEmitter"},
			{17, "Pipeline"},
			{18, "EditorExtension"},
			{20, "Camera"},
			{21, "Material"},
			{23, "MeshRenderer"},
			{25, "Renderer"},
			{26, "ParticleRenderer"},
			{27, "Texture"},
			{28, "Texture2D"},
			{29, "Scene"},
			{30, "RenderManager"},
			{33, "MeshFilter"},
			{41, "OcclusionPortal"},
			{43, "Mesh"},
			{45, "Skybox"},
			{47, "QualitySettings"},
			{48, "Shader"},
			{49, "TextAsset"},
			{52, "NotificationManager"},
			{54, "Rigidbody"},
			{55, "PhysicsManager"},
			{56, "Collider"},
			{57, "Joint"},
			{59, "HingeJoint"},
			{64, "MeshCollider"},
			{65, "BoxCollider"},
			{71, "AnimationManager"},
			{74, "AnimationClip"},
			{75, "ConstantForce"},
			{76, "WorldParticleCollider"},
			{78, "TagManager"},
			{81, "AudioListener"},
			{82, "AudioSource"},
			{83, "AudioClip"},
			{84, "RenderTexture"},
			{87, "MeshParticleEmitter"},
			{88, "ParticleEmitter"},
			{89, "Cubemap"},
			{92, "GUILayer"},
			{94, "ScriptMapper"},
			{96, "TrailRenderer"},
			{98, "DelayedCallManager"},
			{102, "TextMesh"},
			{104, "RenderSettings"},
			{108, "Light"},
			{109, "CGProgram"},
			{111, "Animation"},
			{114, "MonoBehaviour"},
			{115, "MonoScript"},
			{116, "MonoManager"},
			{117, "Texture3D"},
			{119, "Projector"},
			{120, "LineRenderer"},
			{121, "Flare"},
			{122, "Halo"},
			{123, "LensFlare"},
			{124, "FlareLayer"},
			{125, "HaloLayer"},
			{126, "NavMeshLayers"},
			{127, "HaloManager"},
			{128, "Font"},
			{129, "PlayerSettings"},
			{130, "NamedObject"},
			{131, "GUITexture"},
			{132, "GUIText"},
			{133, "GUIElement"},
			{134, "PhysicMaterial"},
			{135, "SphereCollider"},
			{136, "CapsuleCollider"},
			{137, "SkinnedMeshRenderer"},
			{138, "FixedJoint"},
			{140, "RaycastCollider"},
			{141, "BuildSettings"},
			{142, "AssetBundle"},
			{143, "CharacterController"},
			{144, "CharacterJoint"},
			{145, "SpringJoint"},
			{146, "WheelCollider"},
			{147, "ResourceManager"},
			{148, "NetworkView"},
			{149, "NetworkManager"},
			{150, "PreloadData"},
			{152, "MovieTexture"},
			{153, "ConfigurableJoint"},
			{154, "TerrainCollider"},
			{155, "MasterServerInterface"},
			{156, "TerrainData"},
			{157, "LightmapSettings"},
			{158, "WebCamTexture"},
			{159, "EditorSettings"},
			{160, "InteractiveCloth"},
			{161, "ClothRenderer"},
			{163, "SkinnedCloth"},
			{164, "AudioReverbFilter"},
			{165, "AudioHighPassFilter"},
			{166, "AudioChorusFilter"},
			{167, "AudioReverbZone"},
			{168, "AudioEchoFilter"},
			{169, "AudioLowPassFilter"},
			{170, "AudioDistortionFilter"},
			{180, "AudioBehaviour"},
			{181, "AudioFilter"},
			{182, "WindZone"},
			{183, "Cloth"},
			{184, "SubstanceArchive"},
			{185, "ProceduralMaterial"},
			{186, "ProceduralTexture"},
			{191, "OffMeshLink"},
			{192, "OcclusionArea"},
			{193, "Tree"},
			{194, "NavMesh"},
			{195, "NavMeshAgent"},
			{196, "NavMeshSettings"},
			{197, "LightProbeCloud"},
			{198, "ParticleSystem"},
			{199, "ParticleSystemRenderer"},
			{205, "LODGroup"},
			{220, "LightProbeGroup"},
			{1001, "Prefab"},
			{1002, "EditorExtensionImpl"},
			{1003, "AssetImporter"},
			{1004, "AssetDatabase"},
			{1005, "Mesh3DSImporter"},
			{1006, "TextureImporter"},
			{1007, "ShaderImporter"},
			{1020, "AudioImporter"},
			{1026, "HierarchyState"},
			{1027, "GUIDSerializer"},
			{1028, "AssetMetaData"},
			{1029, "DefaultAsset"},
			{1030, "DefaultImporter"},
			{1031, "TextScriptImporter"},
			{1034, "NativeFormatImporter"},
			{1035, "MonoImporter"},
			{1037, "AssetServerCache"},
			{1038, "LibraryAssetImporter"},
			{1040, "ModelImporter"},
			{1041, "FBXImporter"},
			{1042, "TrueTypeFontImporter"},
			{1044, "MovieImporter"},
			{1045, "EditorBuildSettings"},
			{1046, "DDSImporter"},
			{1048, "InspectorExpandedState"},
			{1049, "AnnotationManager"},
			{1050, "MonoAssemblyImporter"},
			{1051, "EditorUserBuildSettings"},
			{1052, "PVRImporter"},
			{1112, "SubstanceImporter"}
		};
	}
}