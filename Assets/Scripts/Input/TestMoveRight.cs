using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
[RequireComponent(typeof(Player))]
public class TestMoveRight : MonoBehaviour
{
    private Player player;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
    }

    // Update is called once per frame
    public float executeTime = 0.6f;
    public float waitTime = 0.4f;
    void Update()
    {
        if (executeTime > waitTime)
            player.DirInput = Vector2.zero;
        else if (executeTime > 0)
            player.DirInput = Vector2.right;
        else player.DirInput = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.Alpha0)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TestScene");
            executeTime = 0.5f;
        }

        executeTime -= Time.deltaTime;
    }
}
}