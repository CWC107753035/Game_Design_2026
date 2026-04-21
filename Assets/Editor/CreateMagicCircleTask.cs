using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class CreateMagicCircleTask
{
    static CreateMagicCircleTask()
    {
        EditorApplication.delayCall += CheckAndRun;
    }

    private static void CheckAndRun()
    {
        string flagFile = "Assets/Editor/RunMagicCircle.flag";
        if (File.Exists(flagFile))
        {
            File.Delete(flagFile);
            Execute();
        }
    }

    [MenuItem("Tools/Generate Magic Circle")]
    public static void Execute()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        string targetScenePath = "Assets/Scenes/sample_test.unity";
        
        // Open the designated scene if not already open
        if (currentScenePath.Replace("\\", "/") != targetScenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(targetScenePath);
            }
            else
            {
                Debug.LogWarning("Aborted Magic Circle Creation: Please save your current scene first.");
                return;
            }
        }

        string[] guids = AssetDatabase.FindAssets("4261531 t:Texture2D");
        if (guids == null || guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("magic array brilliant t:Texture2D");
        }
        
        Texture2D tex = null;
        if (guids != null && guids.Length > 0)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }
        else
        {
            // Ultimate fallback to literal path
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Texture/-Pngtree-magic array brilliant elements_4261531.png");
        }

        if (tex == null)
        {
            Debug.LogError("Could not load magic circle texture. If you moved or renamed it, please verify.");
            return;
        }

        // Setup material
        string matPath = "Assets/Texture/Materials/MagicCircleGlow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // Trying multiple shader paths depending on Unity configuration
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                         ?? Shader.Find("Particles/Standard Unlit") 
                         ?? Shader.Find("Mobile/Particles/Additive")
                         ?? Shader.Find("Unlit/Transparent");
            
            mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            
            // Magic glow color: Cyan with high emission multiplier
            Color emColor = new Color(0.1f, 0.8f, 1.0f, 1.0f) * 4f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", emColor);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", emColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", emColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emColor);
            }

            if (!AssetDatabase.IsValidFolder("Assets/Texture/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/Texture", "Materials");
            }
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Check if there's already one in the scene to avoid duplicates
        GameObject magicCircle = GameObject.Find("Glowing_Magic_Circle");
        if (magicCircle == null)
        {
            magicCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            magicCircle.name = "Glowing_Magic_Circle";
            
            // Remove the default collider for a purely visual effect
            Object.DestroyImmediate(magicCircle.GetComponent<MeshCollider>());

            // Set rotation to lie flat on the floor
            magicCircle.transform.position = new Vector3(0, 0.05f, 0); 
            magicCircle.transform.rotation = Quaternion.Euler(90, 0, 0); 
            magicCircle.transform.localScale = new Vector3(5, 5, 1); 

            MeshRenderer mr = magicCircle.GetComponent<MeshRenderer>();
            mr.material = mat;
        }

        // Prefab creation
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        string prefabPath = "Assets/Prefabs/Glowing_Magic_Circle.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(magicCircle, prefabPath, InteractionMode.UserAction);
        }

        // Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Magic Circle successfully created in sample_test scene!");
    }
}
