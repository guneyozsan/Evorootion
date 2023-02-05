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
        [SerializeField] private float rootDecay;
        [SerializeField] private float waterDiffusionProbability;
        [SerializeField] private float waterDecay;
        [SerializeField] private float controllerBiasMultiplier;
        
        [Header("Palette")]
        [SerializeField] private Color borderColor;
        [SerializeField] private Color earthColor;
        [SerializeField] private Color rootColor;
        [SerializeField] private Color rootTipColor;
        [SerializeField] private Color skyColor;
        [SerializeField] private Color treeColor;
        [SerializeField] private Color treeTipColor;
        [SerializeField] private Color waterColor;
        
        [Header("References")]
        [SerializeField] private RenderTexture world;
        [SerializeField] private AudioSource idleMusic;
        [SerializeField] private AudioSource gameplayMusic;

        private void Awake()
        {
            idleMusic.Play();
        }

        private void Start()
        {
            borderColor = GetRoundingSafeColor(borderColor);
            earthColor = GetRoundingSafeColor(earthColor);
            rootColor = GetRoundingSafeColor(rootColor);
            rootTipColor = GetRoundingSafeColor(rootTipColor);
            skyColor = GetRoundingSafeColor(skyColor);
            treeColor = GetRoundingSafeColor(treeColor);
            treeTipColor = GetRoundingSafeColor(treeTipColor);
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
            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
                yield break;

            // Draw the sky.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = landscapeY; j < world.height; j++)
                {
                    texture.SetPixel(i, j, skyColor);
                    texture.Apply();
                    Graphics.Blit(texture, world);
                }
            }
            
            while (true)
            {
                bool isSuccess = false;
                
                yield return new WaitForEndOfFrame();

                #region Initialize

                texture = ScreenCapture.CaptureScreenshotAsTexture();

                if (texture == null)
                    yield break;

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
                texture.SetPixel(treePositionX, landscapeY, treeColor);
                texture.SetPixel(treePositionX, landscapeY + 1, treeTipColor);
                texture.SetPixel(treePositionX, landscapeY - 1, rootColor);
                texture.SetPixel(treePositionX, landscapeY - 2, rootTipColor);

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
                int rootTipCount = int.MaxValue;
            
                idleMusic.Stop();
                gameplayMusic.Play();
            
                while (!isSuccess && rootTipCount != 0)
                {
                    rootTipCount = 0;
                
                    #region Input Controller
                
                    int xNegativeBias = 0;
                    int xPositiveBias = 0;
                    int yNegativeBias = 0;
                    int yPositiveBias = 0;

                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        xNegativeBias = 1;
                        xPositiveBias = 0;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        xNegativeBias = 0;
                        xPositiveBias = 1;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow))
                    {
                        xNegativeBias = 0;
                        xPositiveBias = 0;
                    }
                
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        yNegativeBias = 1;
                        yPositiveBias = 0;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        yNegativeBias = 0;
                        yPositiveBias = 1;
                    }
                    if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.UpArrow))
                    {
                        yNegativeBias = 0;
                        yPositiveBias = 0;
                    }

                    #endregion

                    setPixelQueue.Clear();
                    yield return new WaitForEndOfFrame();

                    texture = ScreenCapture.CaptureScreenshotAsTexture();
                
                    Color rootAge = texture.GetPixel(treePositionX, landscapeY - 1);
                    Color.RGBToHSV(rootAge, out float remainingLifeH, out float remainingLifeS, out float remainingLifeV);

                    // Update root age.
                    Color nextRootAge = Color.HSVToRGB(remainingLifeH, remainingLifeS, remainingLifeV - rootDecay);
                    setPixelQueue.Add((treePositionX, landscapeY - 1, nextRootAge));
                
                    for (int x = 0; x < world.width; x++)
                    {
                        for (int y = 0; y < world.height; y++)
                        {
                            Color currentPixel = texture.GetPixel(x, y);
                        
                            #region Root Growth

                            if (currentPixel == rootTipColor)
                            {
                                rootTipCount++;
                                
                                Color GetNormalizedWaterColor(Color color)
                                {
                                    Color.RGBToHSV(color, out float h, out float s, out float v);
                                    return Color.HSVToRGB(h, 1, v);
                                }

                                if (GetNormalizedWaterColor(texture.GetPixel(x, y + 1)) == waterColor ||
                                    GetNormalizedWaterColor(texture.GetPixel(x, y - 1)) == waterColor ||
                                    GetNormalizedWaterColor(texture.GetPixel(x + 1, y)) == waterColor ||
                                    GetNormalizedWaterColor(texture.GetPixel(x - 1, y)) == waterColor)
                                {
                                    isSuccess = true;
                                    Debug.Log("Success!");
                                }
                                else if (texture.GetPixel(x, y + 1) != earthColor &&
                                         texture.GetPixel(x, y - 1) != earthColor && 
                                         texture.GetPixel(x + 1, y) != earthColor && 
                                         texture.GetPixel(x - 1, y) != earthColor)
                                {
                                    // Root tip is dead.
                                    setPixelQueue.Add((x, y, rootAge));
                                }
                                else if (remainingLifeV <= 0)
                                {
                                    setPixelQueue.Add((x, y, rootAge));
                                }
                                else
                                {
                                    float weightXNegative = 1 + controllerBiasMultiplier * xNegativeBias;
                                    float weightXPositive = 1 + controllerBiasMultiplier * xPositiveBias;
                                    float weightYNegative = 1 + controllerBiasMultiplier * yNegativeBias;
                                    float weightYPositive = 1 + controllerBiasMultiplier * yPositiveBias;
                                    float weightSum = weightXNegative + weightXPositive + weightYNegative + weightYPositive;
                                    
                                    float directionDice = Random.Range(0, weightSum);
                                
                                    int deltaX;
                                    int deltaY;
                                
                                    if (directionDice < weightXNegative)
                                    {
                                        deltaX = -1;
                                        deltaY = 0;
                                    }
                                    else if (directionDice < weightXNegative + weightXPositive)
                                    {
                                        deltaX = 1;
                                        deltaY = 0;
                                    }
                                    else if (directionDice < weightXNegative + weightXPositive + weightYNegative)
                                    {
                                        deltaX = 0;
                                        deltaY = -1;
                                    }
                                    else
                                    {
                                        deltaX = 0;
                                        deltaY = 1;
                                    }
                            
                                    int xNext = x + deltaX;
                                    int yNext = y + deltaY;
                            
                                    Color targetColor = texture.GetPixel(xNext, yNext);
                            
                                    if (targetColor == borderColor)
                                    {
                                        setPixelQueue.Add((x, y, rootAge));
                                    }
                                    else
                                    {
                                        if (targetColor == earthColor)
                                        {
                                            setPixelQueue.Add((x, y, rootAge));
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
                                        setPixelQueue.Add((target.x, target.y, Color.HSVToRGB(h, s - waterDecay, v)));
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
                }
                
                setPixelQueue.Clear();
                
                yield return null;

                if (isSuccess)
                {
                    Debug.Log("TREEEE");
                    for (int x = 0; x < world.width; x++)
                    {
                        for (int y = 0; y < world.height; y++)
                        {
                            Color currentPixel = texture.GetPixel(x, y);

                            if (currentPixel == treeTipColor)
                            {
                                int weightUp = 4;
                                int weightSide = 1;
                                int direction = Random.Range(0, weightUp + 2 * weightSide);

                                int dX = 0;
                                int dY = 0;
                                
                                if (direction == 0)
                                {
                                    dX = -1;
                                }
                                else if (direction == 1)
                                {
                                    dX = 1;
                                }
                                else
                                {
                                    dY = 1;
                                }
                                
                                setPixelQueue.Add((x + dX, y + dY, treeTipColor));
                                setPixelQueue.Add((x, y, treeColor));
                            }
                        }
                    }
                    
                    foreach ((int x, int y, Color color) parameters in setPixelQueue)
                    {
                        texture.SetPixel(parameters.x, parameters.y, parameters.color);
                    }
                
                    texture.Apply();
                    Graphics.Blit(texture, world);
                }
                
                Destroy(texture);
                
                gameplayMusic.Stop();
                idleMusic.Play();

                while (!Input.GetKey(KeyCode.Space))
                {
                    yield return null;
                    if (!isSuccess)
                    {
                        idleMusic.Play();
                    }
                }
            }
        }
    }
}