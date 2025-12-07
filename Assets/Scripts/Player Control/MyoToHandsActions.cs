using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Thalmic.Myo;

public class MyoToHandsActions : MonoBehaviour
{
    public ThalmicMyo myo;
    public GameObject rightHand;

    private Thalmic.Myo.Pose lastPose = Thalmic.Myo.Pose.Rest;
    private float poseCooldown = 0.5f; // tempo mínimo entre detecções (meio segundo)
    private float lastPoseTime = 0f;

    void Update()
    {
        if (myo == null) return;

        Thalmic.Myo.Pose currentPose = myo.pose;

        // evita repetições rápidas
        if (currentPose != lastPose && Time.time - lastPoseTime > poseCooldown)
        {
            switch (currentPose)
            {
                case Thalmic.Myo.Pose.Fist:
                    Debug.Log("MYO -> Fist (Pegar)");
                    rightHand.GetComponent<HandActions>().StartGrab();
                    break;

                case Thalmic.Myo.Pose.WaveOut:
                    Debug.Log("MYO -> WaveOut (Empurrar)");
                    rightHand.GetComponent<HandActions>().StartPush();
                    break;

                case Thalmic.Myo.Pose.WaveIn:
                    Debug.Log("MYO -> WaveIn (Puxar)");
                    rightHand.GetComponent<HandActions>().StartPull();
                    break;
            }

            lastPose = currentPose;
            lastPoseTime = Time.time;
        }
    }
}



// public class MyoToHandsActions : MonoBehaviour
// {
//     public ThalmicMyo myo; // seu Myo conectado
//     public GameObject leftHand;
//     public GameObject rightHand;


//     void Update()
//     {
//         if (myo == null) return;

//         Thalmic.Myo.Pose currentPose = myo.pose;


//         switch (currentPose)
//         {
//             case Thalmic.Myo.Pose.Fist:
//                 rightHand.GetComponent<HandActions>().StartGrab();
//                 break;

//             case Thalmic.Myo.Pose.WaveOut:
//                 rightHand.GetComponent<HandActions>().StartPush();
//                 break;

//             case Thalmic.Myo.Pose.WaveIn:
//                 rightHand.GetComponent<HandActions>().StartPull();
//                 break;

//                 // case MyoPose.Rest:
//                 // se quiser, aqui pode resetar estados ou parar ações
//                 // break;

//         }
//     }
// }
