using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CarSelection : MonoBehaviour
{
    public GameObject [] cars;
    public int selectedCar = 0;

    public void NextCar() {
        cars[selectedCar].SetActive(false);
        selectedCar = (selectedCar + 1) % cars.Length;
        cars[selectedCar].SetActive(true);
    }

    public void PreviousCar() {
        cars[selectedCar].SetActive(false);
        selectedCar--;
        if (selectedCar < 0) {
            selectedCar +=cars.Length;
        }
        cars[selectedCar].SetActive(true);
    }

    public void StartGame() {
        PlayerPrefs.SetInt("selectedCar", selectedCar);
        SceneManager.LoadScene("Track1", LoadSceneMode.Single);
    }
}
