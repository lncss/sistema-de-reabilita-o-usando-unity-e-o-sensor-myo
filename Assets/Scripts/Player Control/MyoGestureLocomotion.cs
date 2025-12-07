using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

using UnityVector3 = UnityEngine.Vector3;
using MyoPose = Thalmic.Myo.Pose;

[RequireComponent(typeof(CharacterController))]
public class MyoGestureLocomotion : MonoBehaviour
{
    [Header("Referências")]
    public ThalmicMyo myo;                 // O dispositivo MYO na cena
    public XROrigin xrOrigin;              // XR Origin (para pegar a direção da câmera)
    
    [Header("Configurações de Movimento")]
    public float moveSpeed = 2f;
    public float rotationSmooth = 5f;

    private CharacterController characterController;
    private MyoPose currentPose = MyoPose.Rest;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (myo == null || xrOrigin == null) return;

        // Atualiza o gesto atual do MYO
        currentPose = myo.pose;

        Vector3 moveDir = Vector3.zero;

        // Define o movimento baseado no gesto
        switch (currentPose)
        {
            case Thalmic.Myo.Pose.FingersSpread:   // Ex: Mão aberta → anda para frente
                moveDir = xrOrigin.Camera.transform.forward;
                break;

            case Thalmic.Myo.Pose.Fist:            // Ex: Punho fechado → anda para trás
                moveDir = -xrOrigin.Camera.transform.forward;
                break;

            case Thalmic.Myo.Pose.DoubleTap:       // Ex: Toque duplo → parar
                moveDir = Vector3.zero;
                break;

            default:
                moveDir = Vector3.zero;
                break;
        }

        // Impede movimento vertical (mantém no plano do chão)
        moveDir.y = 0f;

        // Aplica movimento
        if (moveDir != UnityVector3.zero)
        {
            UnityVector3 motion = moveDir.normalized * moveSpeed * Time.deltaTime;
            characterController.Move(motion);
        }

        // Ajusta a rotação do jogador para acompanhar a direção da câmera
        Quaternion targetRot = Quaternion.Euler(0, xrOrigin.Camera.transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
    }
}

