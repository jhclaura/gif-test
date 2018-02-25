using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickerSpawner : MonoBehaviour {

    public GameObject stickerPrefab;
    public float spawnRate;
    public int maxStickers = 100;

    private int counter = 0;

	void Start () {
        StartCoroutine(Spawn());
	}
	
	IEnumerator Spawn()
    {
        ++counter;

        if (counter < maxStickers)
        {
            Instantiate(stickerPrefab, new Vector3(Random.Range(-1f, 1f), transform.position.y + Random.Range(-1f, 1f), Random.Range(-1f, 1f)), RandomQuaternion());
            yield return new WaitForSeconds(1f / spawnRate);
            yield return Spawn();
        }
    }

    Quaternion RandomQuaternion()
    {
        return new Quaternion(Random.Range(-1, 1), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    }
}
