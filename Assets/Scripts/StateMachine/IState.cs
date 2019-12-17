using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
public interface IState
{
    string Name();
    void Enter();
    void Execute();
    string Change();
    void Exit();
}
}