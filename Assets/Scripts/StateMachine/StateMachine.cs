using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
public abstract class StateMachine
{
    protected IState currState;
    protected Dictionary<string, IState> states;

    public StateMachine() {
        states = new Dictionary<string, IState>();
    }

    public string CurrState {
        get {
            return currState.Name();
        }
    }
    public void ForceState(string state) {
        ChangeState(state);
    }

    private void ChangeState(string state) {
        if (currState != null) {
            currState.Exit();
        }
        if (states.ContainsKey(state)) {
            currState = states[state];
            currState.Enter();
        }
        else Debug.Log($"State {state} does not exist");
    }

    public void Run() {
        if (currState == null) return;

        var newState = currState.Change();
        if (newState.Length != 0) {
            ChangeState(newState);
        }

        currState.Execute();
    }
}
}