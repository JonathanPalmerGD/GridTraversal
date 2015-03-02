using UnityEngine;
using System.Collections;


public class Archmod : MonoBehaviour
{
	public FlockManager flockManager = null;
	public int maxFlockers = 50;
	public int minFlockers = 1;
	public bool guiEnabled = true;
	private float vertAdjust = .10f;
#if UNITY_ANDROID
	private float buttonHeight = .09f;
#else
	private float buttonHeight = .05f;
#endif

	void OnGUI()
	{

#if !UNITY_ANDROID
		if (!guiEnabled){
			if(Input.GetMouseButton(0)){
				guiEnabled = true;
			}
		}
#endif

#if UNITY_WEBPLAYER
			GUI.skin.box.wordWrap = true;
			GUI.skin.button.wordWrap = true;
			GUI.skin.label.wordWrap = true;
			GUI.skin.button.fontSize = 14;
			GUI.skin.label.fontSize = 16;
			GUI.skin.box.fontSize = 26;
#elif UNITY_ANDROID
			GUI.skin.box.wordWrap = true;
			GUI.skin.button.wordWrap = true;
			GUI.skin.label.wordWrap = true;
			GUI.skin.button.fontSize = 24;
			GUI.skin.label.fontSize = 20;
			GUI.skin.box.fontSize = 26;
#endif

			if (guiEnabled)
			{
				
				GUI.Box(new Rect(Screen.width * .05f, Screen.height * (.625f + vertAdjust), Screen.width * .90f, Screen.height * .275f), "Grid Traversal");

			#region Flocker Speed
#if UNITY_WEBPLAYER
			GUI.Label(new Rect(Screen.width * .09f, Screen.height * (.760f + vertAdjust), Screen.width * .05f, Screen.height * buttonHeight), flockManager.flockerMaxSpeed.ToString());
#elif UNITY_ANDROID
			GUI.Label(new Rect(Screen.width * .09f, Screen.height * (.775f + vertAdjust), Screen.width * .05f, Screen.height * buttonHeight), flockManager.flockerMaxSpeed.ToString());
#endif
			if (GUI.Button(new Rect(Screen.width * .15f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Speed Up"))
			{
				if (flockManager.flockerMaxSpeed < 14)
				{
					flockManager.ChangeFlockerSpeed(flockManager.flockerMaxSpeed + 1);
				}
			}

			if (GUI.Button(new Rect(Screen.width * .15f, Screen.height * (.80f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Speed Down"))
			{
				if (flockManager.flockerMaxSpeed > 6)
				{
					flockManager.ChangeFlockerSpeed(flockManager.flockerMaxSpeed - 1);
				}
			}
			#endregion

			#region Add/Remove Flockers
			#if UNITY_WEBPLAYER
			GUI.Label(new Rect(Screen.width * .29f, Screen.height * (.760f + vertAdjust), Screen.width * .05f, Screen.height * buttonHeight), flockManager.Flockers.Count.ToString());
#elif UNITY_ANDROID
			GUI.Label(new Rect(Screen.width * .29f, Screen.height * (.775f + vertAdjust), Screen.width * .05f, Screen.height * buttonHeight), flockManager.Flockers.Count.ToString());
#endif
			if (flockManager.Flockers.Count < maxFlockers)
			{
				if (GUI.Button(new Rect(Screen.width * .35f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Add Flocker"))
				{
					flockManager.AddFlocker();
				}
			}
			else
			{
				GUI.skin.box.fontSize = 20;
				GUI.Box(new Rect(Screen.width * .35f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Max Flockers (" + maxFlockers + ")");
			}

			if (flockManager.Flockers.Count > minFlockers)
			{
				if (GUI.Button(new Rect(Screen.width * .35f, Screen.height * (.80f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Minus Flocker"))
				{
					flockManager.RemoveFlocker();
				}
			}
			else
			{
				GUI.skin.box.fontSize = 20;
				GUI.Box(new Rect(Screen.width * .35f, Screen.height * (.80f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Min Flockers (" + minFlockers + ")");
			}


			#endregion

			#region Column 4
			if (GUI.Button(new Rect(Screen.width * .75f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Hide UI"))
			{
				guiEnabled = false;
			}

			if (GUI.Button(new Rect(Screen.width * .75f, Screen.height * (.80f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Reset All"))
			{
				//flockManager.ResetFlockers();
				Application.LoadLevel(Application.loadedLevel);
			}
			#endregion

			#region Column 3

			if (GUI.Button(new Rect(Screen.width * .55f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Random"))
			{
				flockManager.MassSetWant();
				flockManager.ResetFlockerPath();
			}
			if (GUI.Button(new Rect(Screen.width * .55f, Screen.height * (.80f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Recolor"))
			{
				RegenerateNodes();
			}
			#endregion
			

		}
		else
		{

			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUI.skin.label.fontSize = 26;
			GUI.Label(new Rect(Screen.width * .05f, Screen.height * (.625f + vertAdjust), Screen.width * .90f, Screen.height * .0625f), "Grid Traversal");
			GUI.skin.label.fontSize = 20;
			if (GUI.Button(new Rect(Screen.width * .75f, Screen.height * (.70f + vertAdjust), Screen.width * .15f, Screen.height * buttonHeight), "Show UI"))
			{
				guiEnabled = true;
			}
		}


		GUI.skin.label.alignment = TextAnchor.UpperRight;
		GUI.Label(new Rect(Screen.width * .60f, Screen.height * .00f, Screen.width * .35f, Screen.height * .15f), "www.JonathanPalmerGD.com\nBy Jonathan Palmer");
	}

	private void RegenerateNodes()
	{
		flockManager.ResetNodeColors();
		flockManager.AssignWantedNodes();
		flockManager.ResetFlockerPath();
	}
}

