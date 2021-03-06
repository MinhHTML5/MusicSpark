﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SCR_Action : MonoBehaviour {
#if UNITY_WEBGL
	public const float 			MUSIC_OFFSET = 0.1f;
#else
	public const float 			MUSIC_OFFSET = 0.0f;
#endif

	public const float 			SCROLL_SPEED = 15;
	public const float 			COLOR_CHANGE_SPEED = 1.0f;
	public const float 			PEACE_TIME = 5.0f;
	public const float 			BRICK_SPAWN_LATENCY = 2.0f;
	public const float 			BRICK_BLOCK_LATENCY = 0.06f;
	public const float 			MID_BRICK_CHANCE = 15;
	
	public static SCR_Action 	instance;
	
	public GameObject 			PFB_Cube;
	public GameObject 			PFB_Brick;
	public GameObject 			PFB_CubeExplosion;
	
	public GameObject 			SPR_GlowLeft;
	public GameObject 			SPR_GlowMiddle;
	public GameObject 			SPR_GlowRight;
	public GameObject 			MDL_DiscoBall;
	public GameObject 			CTN_Replay;
	public GameObject 			IMG_Tutorial;
	public AudioSource 			SND_Music;
	public Text 				TXT_Score;
	
	public GameObject 			ball;
	
	public Color[] 				majorGlobalColor;
	public Color[] 				minorGlobalColor;
	public int					oldColorId;
	public int					currentColorId;
	public Color 				majorColor;
	public Color				minorColor;
	
	
	public  int   				score = 0;
	public  bool  				lose = false;
	public  float 				loseDelay = 1;
	
	private int 				laneHighlight = 0;
	private float 				brickCount = 0;
	private float 				spawnCount = 0;
	private int   				spawnIndex = 0;
	private float 				musicDelay = 0;
	private float 				endDelay = 6.0f;
	
	private float 				colorShiftCount = 0;
	private float 				colorShiftInterval = 0;
	
	
	
	
	private void Start() {
		// Init system things
		instance = this;
		SCR_Pool.Flush();
		Application.targetFrameRate = 60;
#if UNITY_STANDALONE_WIN
		Screen.SetResolution(540, 960, false);
#endif
		
		spawnCount = 0;
		spawnIndex = 0;
		musicDelay = SCR_Cube.SPAWN_Z / SCROLL_SPEED - SCR_MusicData.instance.GetData()[0] + MUSIC_OFFSET;
		brickCount = PEACE_TIME;
		
		colorShiftInterval = 0.5f;
		
		PickRandomColor(true);
		
		SPR_GlowLeft.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		SPR_GlowMiddle.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		SPR_GlowRight.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		SPR_GlowMiddle.GetComponent<SCR_Lane>().SetHighlight (true);
		
		ApplyColor();
		
		CTN_Replay.SetActive (false);
    }

    private void Update() {
		float dt = Time.deltaTime;
		
		// =========================================================================================================================
		// Spawn cubes
		if (musicDelay > 0) {
			musicDelay -= dt;
			if (musicDelay <= 0) {
				SND_Music.Play();
				IMG_Tutorial.SetActive (false);
			}
		}
		if (spawnIndex < SCR_MusicData.instance.GetData().Length) {
			spawnCount += dt;
			if (musicDelay <= 0) {
				spawnCount = SND_Music.time + SCR_Cube.SPAWN_Z / SCROLL_SPEED - SCR_MusicData.instance.GetData()[0] + MUSIC_OFFSET;
			}
			if (spawnCount >= SCR_MusicData.instance.GetData()[spawnIndex]) {
				spawnIndex ++;
				GameObject tempCube = SCR_Pool.GetFreeObject (PFB_Cube);
				if (spawnIndex <= 3) {
					tempCube.GetComponent<SCR_Cube>().Spawn(true);
				}
				else {
					tempCube.GetComponent<SCR_Cube>().Spawn(false);
				}
				SCR_ProgressBar.instance.SetProgress (1.0f * spawnIndex / SCR_MusicData.instance.GetData().Length);
			}
		
			brickCount -= dt;
			if (brickCount <= 0) {
				float brickX = 0;
				int random = Random.Range (0, 99);
				if (random % 3 == 0) {
					brickX = -SCR_Brick.SPAWN_X;
				}
				else if (random % 3 == 1) {
					brickX = 0;
				}
				else if (random % 3 == 2) {
					brickX = SCR_Brick.SPAWN_X;
				}
				
				bool spawn = true;
				float warning = 0;
				List<GameObject> cubes = SCR_Pool.GetObjectList(SCR_Action.instance.PFB_Cube);
				for (int i=0; i<cubes.Count; i++) {
					if (cubes[i].activeSelf) {
						if (cubes[i].transform.position.z < SCR_Brick.SPAWN_Z + SCR_Cube.SIZE_Z * 2
						&&  cubes[i].transform.position.z > SCR_Brick.SPAWN_Z - SCR_Cube.SIZE_Z * 2) {
							if (brickX == 0) {
								if (warning == 0) {
									warning = Mathf.Sign(cubes[i].transform.position.x);
								}
								else if (Mathf.Sign(cubes[i].transform.position.x) != warning) {
									spawn = false;
									break;
								}
							}
							else {
								if (Mathf.Sign(brickX) == Mathf.Sign(cubes[i].transform.position.x)) {
									spawn = false;
									break;
								}
							}
						}
					}
				}
				
				if (spawn == true) {
					GameObject tempBrick = SCR_Pool.GetFreeObject (PFB_Brick);
					tempBrick.GetComponent<SCR_Brick>().Spawn(brickX);
					
					float difficulty = 1.0f * spawnIndex / SCR_MusicData.instance.GetData().Length;
					difficulty = 1 - difficulty * 0.75f;
					brickCount = Random.Range(1, 2) * BRICK_SPAWN_LATENCY * difficulty;
				}
			}
		}
		else {
			endDelay -= dt;
			if (endDelay <= 0) {
				CTN_Replay.SetActive (true);
			}
		}
		
		// =========================================================================================================================
		// Change global color
		if (oldColorId != currentColorId) {
			colorShiftCount += dt * COLOR_CHANGE_SPEED;
			if (colorShiftCount > 1) {
				colorShiftCount = 0;
				majorColor = majorGlobalColor[currentColorId];
				minorColor = minorGlobalColor[currentColorId];
				oldColorId = currentColorId;
			}
			else {
				majorColor = SCR_Helper.ColorTween (majorGlobalColor[oldColorId], majorGlobalColor[currentColorId], colorShiftCount);
				minorColor = SCR_Helper.ColorTween (minorGlobalColor[oldColorId], minorGlobalColor[currentColorId], colorShiftCount);
			}
		}
		ApplyColor();
		
		if (musicDelay <= 0) {
			colorShiftInterval += dt;
			if (colorShiftInterval > 18.28f) {
				colorShiftInterval -= 18.28f;
				PickRandomColor(false);
			}
		}
		
		// =========================================================================================================================
		// Handle losing
		if (lose && loseDelay > 0) {
			loseDelay -= dt;
			SND_Music.volume = loseDelay;
			if (loseDelay <= 0) {
				loseDelay = 0;
				CTN_Replay.SetActive (true);
			}
		}
		// =========================================================================================================================
    }
	
	private void PickRandomColor(bool applyNow) {
		int choose = 0;
		do {
			choose = Random.Range (0, majorGlobalColor.Length);
		}
		while (choose == currentColorId);
		
		oldColorId = currentColorId;
		currentColorId = choose;
		colorShiftCount = 0;
		
		if (applyNow) {
			oldColorId = choose;
			majorColor = majorGlobalColor[currentColorId];
			minorColor = minorGlobalColor[currentColorId];
		}
	}
	
	private void ApplyColor() {
		SCR_Plane.SetColor (majorColor);
		SCR_Cube.SetColor (majorColor);
		
		ball.GetComponent<SCR_Ball>().SetColor (majorColor);
		
		List<GameObject> explosions = SCR_Pool.GetObjectList(PFB_CubeExplosion);
		for (int i=0; i<explosions.Count; i++) {
			explosions[i].GetComponent<SCR_CubeExplosion>().SetColor (majorColor);
		}
		
		SPR_GlowLeft.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		SPR_GlowMiddle.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		SPR_GlowRight.GetComponent<SCR_Lane>().SetColor (majorColor, minorColor);
		
		SCR_LightSpawner.SetColor (majorColor);
		//MDL_DiscoBall.GetComponent<SCR_DiscoBall>().SetColor (minorColor);
		MDL_DiscoBall.GetComponent<SCR_DiscoBall>().SetColor (majorColor);
		
		SCR_Cue.SetColor (majorColor);
	}
	
	
	
	public void Replay() {
		SceneManager.LoadScene("GSAction/SCN_Action");
	}
	
	public void Score() {
		score ++;
		TXT_Score.text = "" + score;
	}
	
	public void HighlightLane (int lane) {
		if (laneHighlight != lane) {
			if (laneHighlight == -1) {
				SPR_GlowLeft.GetComponent<SCR_Lane>().SetHighlight(false);
			}
			else if (laneHighlight == 0) {
				SPR_GlowMiddle.GetComponent<SCR_Lane>().SetHighlight(false);
			}
			else if (laneHighlight == 1) {
				SPR_GlowRight.GetComponent<SCR_Lane>().SetHighlight(false);
			}
			
			if (lane == -1) {
				SPR_GlowLeft.GetComponent<SCR_Lane>().SetHighlight(true);
			}
			else if (lane == 0) {
				SPR_GlowMiddle.GetComponent<SCR_Lane>().SetHighlight(true);
			}
			else if (lane == 1) {
				SPR_GlowRight.GetComponent<SCR_Lane>().SetHighlight(true);
			}
			
			laneHighlight = lane;
		}
	}
}
