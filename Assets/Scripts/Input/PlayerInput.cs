using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour
{
    private Player player;
    private Vector2 oldInput;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        oldInput = Vector2.zero;
    }
    // Update is called once per frame
    void Update()
    {
        Vector2 currInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetButtonDown("Jump")) {
            player.JumpButtonDown = true;
        }
        if (Input.GetButtonUp("Jump")) {
            player.JumpButtonUp = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            player.WalkInput = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift)) {
            player.WalkInput = false;
        }
        if (currInput.y != 0) {
            if (currInput.y < 0 && currInput.y != oldInput.y) {
                player.DownButtonDown = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TestScene");
        }

        oldInput = currInput;
        player.DirInput = currInput;
    }
}
}