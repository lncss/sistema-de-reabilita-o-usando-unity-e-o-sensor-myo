using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DirectInteractorDebugger : MonoBehaviour
{
    public XRDirectInteractor interactor;

    void Update()
    {
        if (interactor == null) return;

        // 1. Mostra quantos objetos a mão está "hovering"
        int hoveredCount = interactor.interactablesHovered.Count;
        if (hoveredCount > 0)
        {
            foreach (var hovered in interactor.interactablesHovered)
            {
                Debug.Log("[HOVER] Estou encostando em: " + hovered.transform.name);
            }
        }

        // 2. Testa pegar manualmente com a tecla G
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("[TESTE] Apertei G -> tentar pegar");

            if (interactor.firstInteractableSelected != null)
            {
                Debug.Log("[TESTE] Já estou segurando: " + interactor.firstInteractableSelected.transform.name);
            }
            else if (hoveredCount > 0)
            {
                // pega o primeiro hovered
                var candidate = interactor.interactablesHovered[0] as IXRSelectInteractable;
                if (candidate != null)
                {
                    interactor.StartManualInteraction(candidate);
                    Debug.Log("[TESTE] StartManualInteraction em: " + candidate.transform.name);
                }
            }
            else
            {
                Debug.Log("[TESTE] Não tem nada em alcance pra pegar 😢");
            }
        }

        // coloca isto dentro do Update(), logo antes do "if (Input.GetKeyDown(KeyCode.G)) { ... }"
        if (interactor != null && interactor.firstInteractableSelected == null)
        {
            foreach (var hovered in interactor.interactablesHovered)
            {
                Debug.Log("[HOVER AO VIVO] " + hovered.transform.name);
            }
        }

        // 3. Testa soltar com H
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (interactor.firstInteractableSelected != null)
            {
                Debug.Log("[TESTE] Soltando " + interactor.firstInteractableSelected.transform.name);
                interactor.EndManualInteraction();
            }
            else
            {
                Debug.Log("[TESTE] Não estou segurando nada pra soltar");
            }
        }

        

    }
}

