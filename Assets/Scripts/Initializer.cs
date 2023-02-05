using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private int landscapeY;
        [SerializeField] private int treePositionX;
        [SerializeField] private float rootSplitProbability;
        [SerializeField] private float waterDiffusionProbability;
        [FormerlySerializedAs("waterDiffuseBuff")] [SerializeField] private float waterPermability;
        
        [Header("Palette")]
        [SerializeField] private Color borderColor;
        [SerializeField] private Color earthColor;
        [SerializeField] private Color rootColor;
        [SerializeField] private Color rootTipColor;
        [SerializeField] private Color skyColor;
        [SerializeField] private Color treeColor;
        [SerializeField] private Color waterColor;
        
        [Header("References")]
        [SerializeField] private RenderTexture world;

        private void Start()
        {
            borderColor = GetRoundingSafeColor(borderColor);
            earthColor = GetRoundingSafeColor(earthColor);
            rootColor = GetRoundingSafeColor(rootColor);
            rootTipColor = GetRoundingSafeColor(rootTipColor);
            skyColor = GetRoundingSafeColor(skyColor);
            treeColor = GetRoundingSafeColor(treeColor);
            waterColor = GetRoundingSafeColor(waterColor);

            StartCoroutine(Initialize());
        }

        private static Color GetRoundingSafeColor(Color color)
        {
            return new Color(
                (int) (color.r * 255) / 255f,
                (int) (color.g * 255) / 255f,
                (int) (color.b * 255) / 255f,
                (int) (color.a * 255) / 255f);
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            #region Initialize

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

            if (texture == null)
                yield break;

            // Draw the sky.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = landscapeY; j < world.height; j++)
                {
                    texture.SetPixel(i, j, skyColor);
                }
            }

            // Draw the earth.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = 0; j < landscapeY; j++)
                {
                    texture.SetPixel(i, j, earthColor);
                }
            }
            
            // Draw the borders.
            for (int i = 0; i < world.width; i++)
            {
                texture.SetPixel(i, 0, borderColor);
                texture.SetPixel(i, world.height - 1, borderColor);
            }
            
            for (int i = 0; i < world.height; i++)
            {
                texture.SetPixel(0, i, borderColor);
                texture.SetPixel(world.width - 1, i, borderColor);
            }

            // Draw the tree.
            // texture.SetPixel(treePositionX -1, landscapeY - 1, rootTipColor);
            texture.SetPixel(treePositionX, landscapeY - 1, rootTipColor);
            // texture.SetPixel(treePositionX + 1, landscapeY - 1, rootTipColor);

            // Draw the water.
            
            int waterX = Random.Range(0, world.width);
            int waterY = Random.Range(0, landscapeY);

            while (texture.GetPixel(waterX, waterY) != earthColor)
            {
                waterX = Random.Range(0, world.width);
                waterY = Random.Range(0, landscapeY);
            }
            
            texture.SetPixel(waterX, waterY, waterColor);
            
            // Render the texture.
            texture.Apply();
            Graphics.Blit(texture, world);
            Destroy(texture);
            yield return null;
            
            #endregion
            
            var setPixelQueue = new List<(int x, int y, Color color)>(); 
            
            while (true)
            {
                setPixelQueue.Clear();
                yield return new WaitForEndOfFrame();

                texture = ScreenCapture.CaptureScreenshotAsTexture();
                
                for (int x = 0; x < world.width; x++)
                {
                    for (int y = 0; y < world.height; y++)
                    {
                        Color currentPixel = texture.GetPixel(x, y);
                        
                        #region Root Growth

                        if (currentPixel == rootTipColor)
                        {
                            if (texture.GetPixel(x, y + 1) != earthColor &&
                                texture.GetPixel(x, y - 1) != earthColor && 
                                texture.GetPixel(x + 1, y) != earthColor && 
                                texture.GetPixel(x - 1, y) != earthColor)
                            {
                                // Root tip is dead.
                                setPixelQueue.Add((x, y, rootColor));
                            }
                            else
                            {
                                int direction = Random.Range(0, 4);
                                int deltaX;
                                int deltaY;
                            
                                switch (direction)
                                {
                                    case 0:
                                        deltaX = 1;
                                        deltaY = 0;
                                        break;
                                    case 1:
                                        deltaX = 0;
                                        deltaY = 0;
                                        break;
                                    case 2:
                                        deltaX = -1;
                                        deltaY = 0;
                                        break;
                                    case 3:
                                        deltaX = 0;
                                        deltaY = -1;
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }
                            
                                int xNext = x + deltaX;
                                int yNext = y + deltaY;
                            
                                Color targetColor = texture.GetPixel(xNext, yNext);
                            
                                if (targetColor == borderColor)
                                {
                                    setPixelQueue.Add((x, y, rootColor));
                                }
                                else
                                {
                                    if (targetColor == earthColor)
                                    {
                                        setPixelQueue.Add((x, y, rootColor));
                                        setPixelQueue.Add((xNext, yNext, rootTipColor));
                                    }
                                
                                    bool split = Random.Range(0f, 1f) < rootSplitProbability;

                                    if (split)
                                    {
                                        var splitTargets = new List<(int x, int y)>();
                                        if (texture.GetPixel(x + 1, y) == earthColor &&
                                            xNext != x + 1 &&
                                            yNext != y)
                                        {
                                            splitTargets.Add((x + 1, y));
                                        }
                                        if (texture.GetPixel(x - 1, y) == earthColor &&
                                            xNext != x - 1 &&
                                            yNext != y)
                                        {
                                            splitTargets.Add((x - 1, y));
                                        }
                                        if (texture.GetPixel(x, y + 1) == earthColor &&
                                            xNext != x &&
                                            yNext != y + 1)
                                        {
                                            splitTargets.Add((x, y + 1));
                                        }
                                        if (texture.GetPixel(x, y - 1) == earthColor &&
                                            xNext != x &&
                                            yNext != y - 1)
                                        {
                                            splitTargets.Add((x, y - 1));
                                        }
                                    
                                        if (splitTargets.Count > 0)
                                        {
                                            (int x, int y) target = splitTargets[Random.Range(0, splitTargets.Count)];
                                            setPixelQueue.Add((target.x, target.y, rootTipColor));
                                        }
                                    }
                                }
                            }
                        }
                        
                        #endregion
                        
                        #region Water Diffusion
                        
                        Color.RGBToHSV(currentPixel, out float h, out float s, out float v);
                        Color vMaxCurrentPixel = Color.HSVToRGB(h, 1, v);
                
                        if (vMaxCurrentPixel == waterColor)
                        {
                            float diffuseDice = Random.Range(0f, 1f);

                            float densityWeightedDiffusionProbability = s * waterDiffusionProbability;
                            if (diffuseDice != 0f && diffuseDice < densityWeightedDiffusionProbability)
                            {
                                var diffuseTargets = new List<(int x, int y)>();
                                
                                for (int i = 0; i < 3; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        if (i == 1 && j == 1)
                                            continue;
                                        if (texture.GetPixel(x + i - 1, y + j - 1) == earthColor)
                                        {
                                            diffuseTargets.Add((x + i - 1, y + j - 1));
                                        }
                                    }
                                }
                                
                                if (diffuseTargets.Count > 0)
                                {
                                    (int x, int y) target = diffuseTargets[Random.Range(0, diffuseTargets.Count)];
                                    setPixelQueue.Add((target.x, target.y, Color.HSVToRGB(h, s - waterPermability, v)));
                                }
                            }
                        }

                        #endregion
                    }
                }
                
                foreach ((int x, int y, Color color) parameters in setPixelQueue)
                {
                    texture.SetPixel(parameters.x, parameters.y, parameters.color);
                }
                
                texture.Apply();
                Graphics.Blit(texture, world);
                Destroy(texture);
            }
        }
    }
}