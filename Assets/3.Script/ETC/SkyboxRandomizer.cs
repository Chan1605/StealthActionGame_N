using UnityEngine;

public class SkyboxRandomizer : MonoBehaviour
{
    [SerializeField] private Material[] skyboxMaterials;

    private void Awake()
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0) return;

        int index = Random.Range(0, skyboxMaterials.Length);
        Material selectedSkybox = skyboxMaterials[index];

        Debug.Log("Selected Skybox: " + selectedSkybox.name);

        RenderSettings.skybox = selectedSkybox;
        DynamicGI.UpdateEnvironment();
    }
}
