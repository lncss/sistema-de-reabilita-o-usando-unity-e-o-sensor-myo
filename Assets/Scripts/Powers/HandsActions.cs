using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandActions : MonoBehaviour
{
    public void StartGrab()
    {
        Debug.Log($"{gameObject.name} começou a pegar algo!");
        // Aqui você pode adicionar animação de fechar a mão ou lógica de pegar
    }

    public void StartPush()
    {
        Debug.Log($"{gameObject.name} empurrou!");
        // Aqui pode adicionar força, por exemplo
    }

    public void StartPull()
    {
        Debug.Log($"{gameObject.name} puxou!");
        // Pode usar AddForce negativa, ou movimento em direção ao player
    }
}

