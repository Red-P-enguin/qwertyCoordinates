using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wiggle : MonoBehaviour {

    Vector3 originalPosition;
	// Use this for initialization
	void Start () {
        originalPosition = gameObject.transform.localPosition;
        StartCoroutine(Wobble());
    }
	
	IEnumerator Wobble()
    {
        while(true)
        {
            yield return new WaitForSeconds(.05f);
            if(Random.Range(0,10) == 0)
            {
                gameObject.transform.localPosition = new Vector3(originalPosition.x, originalPosition.y + (Random.Range(-1,2) * .01f), originalPosition.z);
            }
            else
            {
                gameObject.transform.localPosition = originalPosition;
            }
        }
    }
}
