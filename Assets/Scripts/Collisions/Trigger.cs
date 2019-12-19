using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
public class Trigger : MonoBehaviour
{
    private bool triggered;

    public bool Triggered {
        get {
            return triggered;
        }
        set {
            triggered = value;
        }
    }

    public void OnEnter(Player player) {
        Debug.Log("Entered");
    }

    public void OnExit(Player player) {
        Debug.Log("Exited");
    }

    public void OnStay(Player player) {
        Debug.Log("inside?");
        player.ChangeColor(Color.magenta);
    }

    void Start() {
        triggered = false;   
    }
}
}