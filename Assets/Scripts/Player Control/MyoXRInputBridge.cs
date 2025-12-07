using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Thalmic.Myo;

using MyoPose = Thalmic.Myo.Pose;
using UnityVector3 = UnityEngine.Vector3;

public class MyoXRInputBridge : MonoBehaviour
{
    [Header("MYO")]
    public ThalmicMyo myo;

    [Header("Interactor da Mão Direita (NÃO o Right Controller!)")]
    public XRDirectInteractor rightDirectInteractor;

    [Header("Interactor da Mão Esquerda (NÃO o Left Controller!)")]
    public XRDirectInteractor leftDirectInteractor;

    [Header("Cooldown entre gestos (segurança)")]
    public float poseCooldown = 0.35f;

    // Último objeto que a mão "tocou" via hover
    private IXRSelectInteractable lastHoverCandidate = null;

    // Objeto atual segurado
    private IXRSelectInteractable currentHeld = null;

    // Último objeto que soltamos
    private IXRSelectInteractable lastReleased = null;

    private MyoPose lastPose = MyoPose.Rest;
    private float lastPoseTime = -999f;

    // Distância máxima pra re-pegar sem hover (em metros)
    public float reacquireDistance = 0.2f;

    void Update()
    {
        UpdateLastHoverCandidate();

        // Debug teclado
        if (Input.GetKeyDown(KeyCode.G)) ForceGrab();
        if (Input.GetKeyDown(KeyCode.H)) ForceRelease();
        if (Input.GetKeyDown(KeyCode.P)) ForcePull();
        if (Input.GetKeyDown(KeyCode.O)) ForcePush();

        // Gestos do Myo
        if (myo != null)
        {
            MyoPose currentPose = myo.pose;

            if (currentPose != lastPose && Time.time - lastPoseTime > poseCooldown)
            {
                switch (currentPose)
                {
                    case MyoPose.Fist:
                        Debug.Log("MYO -> Fist (Pegar)");
                        ForceGrab();
                        break;

                    case MyoPose.FingersSpread:
                    case MyoPose.Rest:
                        Debug.Log("MYO -> Soltar");
                        ForceRelease();
                        break;

                    case MyoPose.WaveIn:
                        Debug.Log("MYO -> WaveIn (Puxar)");
                        ForcePull();
                        break;

                    case MyoPose.WaveOut:
                        Debug.Log("MYO -> WaveOut (Empurrar)");
                        ForcePush();
                        break;
                }

                lastPose = currentPose;
                lastPoseTime = Time.time;
            }
        }
    }

    // Atualiza último candidato baseado no hover atual
    void UpdateLastHoverCandidate()
    {
        if (!rightDirectInteractor) return;

        // Se já estou segurando alguma coisa, não preciso atualizar hover
        if (rightDirectInteractor.firstInteractableSelected != null)
            return;

        // Procura objetos atualmente hovered
        foreach (var hovered in rightDirectInteractor.interactablesHovered)
        {
            var selectable = hovered as IXRSelectInteractable;
            if (selectable != null)
            {
                lastHoverCandidate = selectable;
                return;
            }
        }

        // Se nenhum hovered nesse frame, não forço limpar.
        // Isso deixa o último candidato vivo por alguns frames.
    }

    // -------------- GRAB --------------
    void ForceGrab()
    {
        if (!rightDirectInteractor) return;

        // Se já estou segurando, só loga
        if (rightDirectInteractor.firstInteractableSelected != null)
        {
            var already = rightDirectInteractor.firstInteractableSelected.transform;
            currentHeld = rightDirectInteractor.firstInteractableSelected;
            Debug.Log("[GRAB] Já estou segurando " + already.name +
                      " na posição " + already.position.ToString("F3"));
            return;
        }

        // fallback 1: se não tenho candidato de hover mas acabei de soltar algo,
        // tenta reutilizar esse objeto
        if (lastHoverCandidate == null && lastReleased != null)
        {
            // checa a distância entre a mão e o último solto
            float dist = UnityVector3.Distance(
                rightDirectInteractor.transform.position,
                lastReleased.transform.position
            );

            if (dist <= reacquireDistance)
            {
                Debug.Log("[GRAB][fallback-dist] Reusando último solto " +
                          lastReleased.transform.name + " dist=" + dist.ToString("F3"));
                lastHoverCandidate = lastReleased;
            }
        }

        // ainda nada? então não tem o que agarrar
        if (lastHoverCandidate == null)
        {
            Debug.Log("[GRAB] Nenhum candidato salvo pra pegar");
            return;
        }

        // Só pra debug: onde está o attach da mão?
        Transform attach = null;
        var xrBaseInteractor = rightDirectInteractor as XRBaseInteractor;
        if (xrBaseInteractor != null)
            attach = xrBaseInteractor.attachTransform;

        Debug.Log("[GRAB] Vou pegar " + lastHoverCandidate.transform.name +
                  " | attach da mão = " +
                  (attach ? attach.position.ToString("F3") : "SEM attach"));

        // Pede pro XR Interaction Toolkit iniciar a interação manual
        rightDirectInteractor.StartManualInteraction(lastHoverCandidate);

        // Confirmou?
        if (rightDirectInteractor.firstInteractableSelected != null)
        {
            currentHeld = rightDirectInteractor.firstInteractableSelected;
            var held = currentHeld.transform;
            Debug.Log("[GRAB] Agora segurando " + held.name +
                      " na posição " + held.position.ToString("F3"));

            // Se pegou de volta, já não precisamos mais considerar isso "solto"
            lastReleased = null;
        }
        else
        {
            Debug.Log("[GRAB] Tentei pegar mas ainda não estou segurando nada");
        }
    }

    // -------------- RELEASE --------------
    void ForceRelease()
    {
        if (!rightDirectInteractor) return;

        if (rightDirectInteractor.firstInteractableSelected != null)
        {
            var toRelease = rightDirectInteractor.firstInteractableSelected;
            Debug.Log("[RELEASE] Soltando " + toRelease.transform.name);

            // guarda quem eu soltei
            lastReleased = toRelease;

            // termina a interação manual
            rightDirectInteractor.EndManualInteraction();

            // atualiza estado
            currentHeld = null;

            // deixa esse objeto como candidato imediato,
            // porque visualmente ele está bem na frente da mão
            lastHoverCandidate = lastReleased;
        }
        else
        {
            Debug.Log("[RELEASE] Não estou segurando nada pra soltar");
        }
    }

    // -------------- PULL (WaveIn) --------------
    void ForcePull()
    {
        if (!rightDirectInteractor) return;

        var held = rightDirectInteractor.firstInteractableSelected?.transform;
        if (held)
        {
            held.position = UnityVector3.MoveTowards(
                held.position,
                rightDirectInteractor.transform.position,
                0.05f
            );
            Debug.Log("[PULL] Puxando " + held.name);
        }
        else
        {
            Debug.Log("[PULL] Nada segurado pra puxar");
        }
    }

    // -------------- PUSH (WaveOut) --------------
    void ForcePush()
    {
        if (!rightDirectInteractor) return;

        var held = rightDirectInteractor.firstInteractableSelected?.transform;
        if (held)
        {
            held.position += rightDirectInteractor.transform.forward * 0.05f;
            Debug.Log("[PUSH] Empurrando " + held.name);
        }
        else
        {
            Debug.Log("[PUSH] Nada segurado pra empurrar");
        }
    }
}


// public class MyoXRInputBridge : MonoBehaviour
// {
//     [Header("MYO")]
//     public ThalmicMyo myo;

//     [Header("Interactor da Mão Direita (NÃO o Right Controller!)")]
//     public XRDirectInteractor rightDirectInteractor;

//     [Header("Cooldown entre gestos (segurança)")]
//     public float poseCooldown = 0.35f;

//     private IXRSelectInteractable lastHoverCandidate = null;
//     private MyoPose lastPose = MyoPose.Rest;
//     private float lastPoseTime = -999f;

//     void Update()
//     {
//         // 1. Atualiza quem é o último objeto tocado pela mão
//         UpdateLastHoverCandidate();

//         // 2. Debug manual teclado
//         if (Input.GetKeyDown(KeyCode.G)) ForceGrab();
//         if (Input.GetKeyDown(KeyCode.H)) ForceRelease();
//         if (Input.GetKeyDown(KeyCode.P)) ForcePull();
//         if (Input.GetKeyDown(KeyCode.O)) ForcePush();

//         // 3. Gestos reais do MYO
//         if (myo != null)
//         {
//             MyoPose currentPose = myo.pose;

//             if (currentPose != lastPose && Time.time - lastPoseTime > poseCooldown)
//             {
//                 switch (currentPose)
//                 {
//                     case MyoPose.Fist:
//                         Debug.Log("MYO -> Fist (Pegar)");
//                         ForceGrab();
//                         break;

//                     case MyoPose.FingersSpread:
//                     case MyoPose.Rest:
//                         Debug.Log("MYO -> Soltar");
//                         ForceRelease();
//                         break;

//                     case MyoPose.WaveIn:
//                         Debug.Log("MYO -> WaveIn (Puxar)");
//                         ForcePull();
//                         break;

//                     case MyoPose.WaveOut:
//                         Debug.Log("MYO -> WaveOut (Empurrar)");
//                         ForcePush();
//                         break;
//                 }

//                 lastPose = currentPose;
//                 lastPoseTime = Time.time;
//             }
//         }
//     }

//     // ----------------- núcleo -----------------

//     void UpdateLastHoverCandidate()
//     {
//         if (!rightDirectInteractor) return;

//         // se já está segurando algo, não atualiza candidato
//         if (rightDirectInteractor.firstInteractableSelected != null)
//             return;

//         foreach (var hovered in rightDirectInteractor.interactablesHovered)
//         {
//             var selectable = hovered as IXRSelectInteractable;
//             if (selectable != null)
//             {
//                 lastHoverCandidate = selectable;
//                 break;
//             }
//         }
//     }

//     void ForceGrab()
//     {
//         if (!rightDirectInteractor) return;

//         // já segurando algo?
//         if (rightDirectInteractor.firstInteractableSelected != null)
//         {
//             var already = rightDirectInteractor.firstInteractableSelected.transform;
//             Debug.Log("[GRAB] Já estou segurando " + already.name +
//                     " na posição " + already.position.ToString("F3"));
//             return;
//         }

//         // tentar último hover salvo
//         if (lastHoverCandidate != null)
//         {
//             Transform attach = null;

//             // tenta descobrir o attach transform do interactor
//             var xrBaseInteractor = rightDirectInteractor as XRBaseInteractor;
//             if (xrBaseInteractor != null)
//                 attach = xrBaseInteractor.attachTransform;

//             Debug.Log("[GRAB] Vou pegar " + lastHoverCandidate.transform.name +
//                     " | attach da mão = " + (attach ? attach.position.ToString("F3") : "SEM attach"));

//             rightDirectInteractor.StartManualInteraction(lastHoverCandidate);

//             // depois de tentar pegar, se deu certo:
//             if (rightDirectInteractor.firstInteractableSelected != null)
//             {
//                 var held = rightDirectInteractor.firstInteractableSelected.transform;
//                 Debug.Log("[GRAB] Agora segurando " + held.name +
//                         " na posição " + held.position.ToString("F3"));
//             }
//             else
//             {
//                 Debug.Log("[GRAB] Tentei pegar mas ainda não estou segurando nada");
//             }
//         }
//         else
//         {
//             Debug.Log("[GRAB] Nenhum candidato salvo pra pegar");
//         }
//     }

//     void ForceRelease()
//     {
//         if (!rightDirectInteractor) return;

//         if (rightDirectInteractor.firstInteractableSelected != null)
//         {
//             Debug.Log("[RELEASE] Soltando " +
//                 rightDirectInteractor.firstInteractableSelected.transform.name);

//             rightDirectInteractor.EndManualInteraction();
//         }
//         else
//         {
//             Debug.Log("[RELEASE] Não estou segurando nada pra soltar");
//         }

//         // depois que solta, deixa a mão livre pra atualizar hover de novo
//         lastHoverCandidate = null;
//     }

//     void ForcePull()
//     {
//         if (!rightDirectInteractor) return;

//         var held = rightDirectInteractor.firstInteractableSelected?.transform;
//         if (held)
//         {
//             held.position = UnityVector3.MoveTowards(
//                 held.position,
//                 rightDirectInteractor.transform.position,
//                 0.05f
//             );
//             Debug.Log("[PULL] Puxando " + held.name);
//         }
//         else
//         {
//             Debug.Log("[PULL] Nada segurado pra puxar");
//         }
//     }

//     void ForcePush()
//     {
//         if (!rightDirectInteractor) return;

//         var held = rightDirectInteractor.firstInteractableSelected?.transform;
//         if (held)
//         {
//             held.position += rightDirectInteractor.transform.forward * 0.05f;
//             Debug.Log("[PUSH] Empurrando " + held.name);
//         }
//         else
//         {
//             Debug.Log("[PUSH] Nada segurado pra empurrar");
//         }
//     }
// }




