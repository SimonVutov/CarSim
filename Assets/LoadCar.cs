using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadCar : MonoBehaviour
{
    public GameObject[] carPrefabs;
    public Transform spawnPoint;
    public GameObject clone;

    void Start() {
        int selectedCar = PlayerPrefs.GetInt("selectedCar");
        GameObject prefab = carPrefabs[selectedCar];
        clone = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
    }
}
