using UnityEngine;
using System.Collections;

namespace GradientSkybox
{
    [HelpURL("https://assetstore.unity.com/packages/slug/225546")]
    public class SkyboxManager : MonoBehaviour
    {
        [SerializeField]
        private Material[] materials;

        [SerializeField]
        private float delay;

        [SerializeField]
        private int skyboxIndex = 0;

        /// <summary>
        /// Changing the skybox every N seconds
        /// </summary>
        private IEnumerator Start()
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(delay);
            while (true)
            {
                yield return waitForSeconds;
                skyboxIndex++;
                RenderSettings.skybox = materials[skyboxIndex % materials.Length];
            }
        }
    }
}