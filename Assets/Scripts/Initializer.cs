using System.Collections;
using UnityEngine;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [SerializeField] private RenderTexture world;

        private void Update()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            #region Initialize

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

            if (texture == null)
                yield break;

            for (int j = 0; j < world.width; j++)
            {
                for (int i = 0; i < world.height; i++)
                {
                    texture.SetPixel(i, j, Color.white);
                }
            }
            
            for (int j = Random.Range(0, world.width); j < world.width; j++)
            {
                for (int i = Random.Range(0, world.height); i < world.height; i++)
                {
                    texture.SetPixel(i, j, Color.black);
                }
            }
            
            #endregion
            
            texture.Apply();
            Graphics.Blit(texture, world);
            Destroy(texture);
        }
    }
    // public class Environment
    // {
    //     public Environment()
    //     {
    //     }
    // }
    // public class Tree
    // {
    //     public Tree()
    //     {
    //     }
    // }
    // public class Root
    // {
    //     public Root()
    //     {
    //     }
    // }
    // public class RootTip
    // {
    //     public RootTip()
    //     {
    //     }
    // }
}