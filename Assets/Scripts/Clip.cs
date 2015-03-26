using UnityEngine;
using System.Collections;

namespace Demo {
	public class Clip : MonoBehaviour {

		private TerrainController controller;

		// Use this for initialization
		void Start () {
			controller = GameObject.Find("Terrain").GetComponent<TerrainController>();
		}
		
		// Update is called once per frame
		void Update () {
			if(Input.GetMouseButtonDown(0)){
				Vector3 mouseP = Input.mousePosition;
				mouseP.z = -transform.position.z;
				controller.Clip(camera.ScreenToWorldPoint(mouseP), 30f);
			}
		}
	}
}