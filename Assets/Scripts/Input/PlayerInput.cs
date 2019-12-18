using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour
{
    private Player player;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        player.DirInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetButtonDown("Jump")) {
            player.JumpButtonDown = true;
        }
        if (Input.GetButtonUp("Jump")) {
            player.JumpButtonUp = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TestScene");
        }
    }
}
}