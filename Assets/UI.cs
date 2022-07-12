using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class UI : MonoBehaviour
{
    public GameObject optionsMenu;

    public GameObject vehicle;
    public Text slipText;
    float rpm;
    float speed;
    bool FLSlip;
    bool FRSlip;
    bool BLSlip;
    bool BRSlip;

    string gear;

    string FLString;
    string FRString;
    string BLString;
    string BRString;

    void Update()
    {
        if (vehicle.GetComponent<Vehicle>().isNeutral)
        {
            gear = "N";
            //if (vehicle.GetComponent<Vehicle>().reverse)
            //{
            //    gear = "||" + "R" + (Mathf.Abs(vehicle.GetComponent<Vehicle>().gearSelection + 1)).ToString() + "||";
            //}
            //else
            //{
            //    gear = "||" + (vehicle.GetComponent<Vehicle>().gearSelection + 1).ToString() + "||";
            //}
        }
        else if (!vehicle.GetComponent<Vehicle>().isNeutral)
        {
            if (vehicle.GetComponent<Vehicle>().reverse)
            {
                gear = "R" + (Mathf.Abs(vehicle.GetComponent<Vehicle>().gearSelection + 1)).ToString();
            }
            else
            {
                gear = (vehicle.GetComponent<Vehicle>().gearSelection + 1).ToString();
            }
        }

        if (vehicle.GetComponent<Vehicle>().optionsMenu && vehicle.GetComponent<Vehicle>().mine)
        {
            optionsMenu.SetActive(true);
        }
        else if ((vehicle.GetComponent<Vehicle>().optionsMenu == false) && vehicle.GetComponent<Vehicle>().mine)
        {
            optionsMenu.SetActive(false);
        }

        rpm = Mathf.Round(Mathf.Clamp(vehicle.GetComponent<Vehicle>().currentEngineRPM, 800, vehicle.GetComponent<Vehicle>().maxEngineRPM) / 100) / 10;

        speed = Mathf.Round(vehicle.GetComponent<Vehicle>().speed * 3.6f);

        bool FRSlip = vehicle.GetComponent<Vehicle>().FLSlip;
        bool FLSlip = vehicle.GetComponent<Vehicle>().FRSlip;
        bool BLSlip = vehicle.GetComponent<Vehicle>().BLSlip;
        bool BRSlip = vehicle.GetComponent<Vehicle>().BRSlip;

        if (FLSlip) { FLString = "."; } else { FLString = "|"; }
        if (FRSlip) { FRString = "."; } else { FRString = "|"; }
        if (BLSlip) { BLString = "."; } else { BLString = "|"; }
        if (BRSlip) { BRString = "."; } else { BRString = "|"; }

        if (vehicle.GetComponent<Vehicle>().mine && (vehicle.GetComponent<Vehicle>().optionsMenu == false))
        {
            slipText.text = gear + Environment.NewLine + rpm + Environment.NewLine + speed + Environment.NewLine + FLString + FRString + Environment.NewLine + BLString + BRString;
        }
        else
        {
            slipText.text = " ";
        }
    }

    //for the two input buttons
    public void Continue()
    {
        vehicle.GetComponent<Vehicle>().optionsMenu = false;
    }

    public void Exit()
    {
        SceneManager.LoadScene("MainMenu");
        //Application.LoadLevel(0);
        PhotonNetwork.Disconnect();
    }
}
