﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
[RequireComponent(typeof(ActorController))]
public class Player : MonoBehaviour
{
    private ActorController actorController;
    private StateMachine stateMachine;
    private SpriteRenderer DELETEDIS;

    #region Float settings
    public float moveSpeed = 8f;
    public float maxJumpHeight = 4f;
    public float timeToJumpApex = 0.4f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;
    #endregion
    #region Private float settings
    private float gravity;
    private float jumpVelocity;
    private float accelTime;
    #endregion
    #region Timers/Cooldowns
    private float currCoyoteTime;
    private float currJumpBuffer;
    #endregion
    #region States
    private string[] states = {"Idle", "Run", "Jump", "Fall", "SlopeSlide"};
    #endregion

    private Vector2 dirInput;
    private bool jumpButtonDown;

    private Vector2 velocity;
    private float velocitySmoothingX;

    void Awake() {
        actorController = GetComponent<ActorController>();
        stateMachine = new PlayerStateMachine(new List<string>(states), "Idle", this);
        DELETEDIS = GetComponent<SpriteRenderer>();
    }

    void Start() {
        gravity = (2 * maxJumpHeight) / Mathf.Pow (timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void FixedUpdate() {

        stateMachine.Run();

        CalcGravity();

        actorController.Move(velocity * Time.deltaTime);

        jumpButtonDown = false; // Should probably move this
    }

    #region Movement
    private void UpdateInputVelocity() {
        float targetVelocityX = dirInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocitySmoothingX, accelTime);
    }
    private void CalcGravity() {
        velocity.y -= gravity * Time.deltaTime;
    }
    #endregion

    #region Cooldowns
    private void RefreshGroundedCountdowns() {
        currCoyoteTime = coyoteTime;
        if (jumpButtonDown) {
            currJumpBuffer = jumpBuffer;
        }
    }

    private void AirborneCountdowns() {
        if (currCoyoteTime > 0) {
            currCoyoteTime -= Time.deltaTime;
        }
        if (currJumpBuffer > 0) {
            currJumpBuffer -= Time.deltaTime;
        }
    }

    private void RefreshAirborneCountdowns() {
        if (jumpButtonDown) {
            currJumpBuffer = jumpBuffer;
        }
    }

    private void GroundedCountdowns() {
        if (currJumpBuffer > 0) {
            currJumpBuffer -= Time.deltaTime;
        }
    }
    #endregion

    #region Accessors
    public Vector2 DirInput {
        get {
            return dirInput;
        }
        set {
            dirInput = value;
        }
    }
    public bool JumpButtonDown {
        get {
            return jumpButtonDown;
        }
        set {
            jumpButtonDown = value;
        }
    }
    private bool IsGrounded {
        get {
            return actorController.CollInfo.below;
        }
    }
    private bool CollidingAbove {
        get {
            return actorController.CollInfo.above;
        }
    }
    private bool CollidingRight {
        get {
            return actorController.CollInfo.right;
        }
    }
    private bool CollidingLeft {
        get {
            return actorController.CollInfo.left;
        }
    }
    private bool MaxSlope {
        get {
            return actorController.CollInfo.maxSlope;
        }
    }
    #endregion
    #region Public Methods
    #endregion
    #region Movement Methods
    #endregion
    #region State Machine and States
    class PlayerStateMachine : StateMachine {
        public PlayerStateMachine(List<string> stateStrings, string def, Player player) : base() {
            for (int i = 0; i < stateStrings.Count; i++) {
                var state = stateStrings[i];
                states.Add(state, InitPlayerState(state, player));
            }
            states.TryGetValue(def, out currState);
        }
    }

    private static IState InitPlayerState(string state, Player player) {
        IState newState = null;
        switch (state) {
            case "Idle":
                newState = new Idle(player);
                break;
            case "Run":
                newState = new Run(player);
                break;
            case "Jump":
                newState = new Jump(player);
                break;
            case "Fall":
                newState = new Fall(player);
                break;
            case "SlopeSlide":
                newState = new SlopeSlide(player);
                break;
            default:
                Debug.Log("State not found");
                break;
        }
        if (newState == null) return null;
        return newState;
    }

    abstract class PlayerState : IState {
        protected Player player;
        protected string name;
        protected int framesActive;
        protected bool groundedState;

        public PlayerState(Player player) {
            this.player = player;
        }
        public string Name() {
            return name;
        }
        public virtual void Enter() {
            framesActive = 0;
        }
        public virtual void Execute() {
            framesActive++;
            if (groundedState) {
                player.RefreshGroundedCountdowns();
                player.GroundedCountdowns();
            }
            else {
                player.RefreshAirborneCountdowns();
                player.AirborneCountdowns();
            }
        }
        public abstract string Change();
        public abstract void Exit();
    }

    class Idle : PlayerState {

        public Idle(Player player) : base(player) {
            groundedState = true;
            name = "Idle";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Idle");
            // Play Idle animation
            player.DELETEDIS.color = Color.white;
        }

        public override void Execute() {
            base.Execute();
            // Change player velocity to 0
            player.velocity.x = 0;
            player.velocity.y = 0;
        }

        public override string Change() {
            string next = "";
            if (player.dirInput.x > 0) {
                next = "Run";
            }
            if (player.dirInput.x < 0) {
                next = "Run";
            }
            if (player.MaxSlope) {
                next = "SlopeSlide";
            }
            if (!player.IsGrounded) {
                next = "Fall";
            }
            if (player.currJumpBuffer > 0) {
                next = "Jump";
            }
            return next;
        }

        public override void Exit() {
        }

    }

    class Run : PlayerState {

        private float timeToAccel = 0.1f;
        private float timeToDecel = 0.05f;
        private float idleThreshold = 0.2f;

        public Run(Player player) : base(player) {
            groundedState = true;
            name = "Run";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Run");
            // Play run animation
            player.DELETEDIS.color = Color.blue;
        }

        public override void Execute() {
            base.Execute();
            if (player.dirInput.x == 0) {
                player.accelTime = timeToAccel;
                player.UpdateInputVelocity();
            }
            else {
                player.accelTime = timeToDecel;
                player.UpdateInputVelocity();
            }
            player.velocity.y = 0;
        }

        public override string Change() {
            string next = "";
            if (player.DirInput.x == 0 && Mathf.Abs(player.velocity.x) < idleThreshold) {
                next = "Idle";
            }
            if (player.MaxSlope) {
                next = "SlopeSlide";
            }
            if (!player.IsGrounded) {
                next = "Fall";
            }
            if (player.currJumpBuffer > 0) {
                next = "Jump";
            }
            return next;
        }

        public override void Exit() {
        }
    }

    class Jump : PlayerState {

        private float timeToAccel = 0.1f;

        public Jump(Player player) : base(player) {
            groundedState = false;
            name = "Idle";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Jump");
            player.velocity.y = player.jumpVelocity;
            player.currJumpBuffer = 0;
            // Play jump animation
            player.DELETEDIS.color = Color.green;
        }

        public override void Execute() {
            base.Execute();
            if (player.CollidingAbove) {
                player.velocity.y = 0;
            }
            player.accelTime = timeToAccel;
            player.UpdateInputVelocity();
        }

        public override string Change() {
            string next = "";
            if (player.velocity.y < 0) {
                next = "Fall";
            }
            return next;
        }

        public override void Exit() {
        }
    }

    class Fall : PlayerState {

        private float timeToAccel = 0.3f;

        public Fall(Player player) : base(player) {
            groundedState = false;
            name = "Fall";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Fall");
            // Play fall animation
            player.DELETEDIS.color = Color.red;
        }

        public override void Execute() {
            base.Execute();
            player.accelTime = timeToAccel;
            player.UpdateInputVelocity();
            FallFaster();
        }

        public override string Change() {
            string next = "";
            if (player.IsGrounded) {
                next = "Idle";
                if (player.velocity.x != 0) {
                    next = "Run";
                }
            }
            if (player.MaxSlope) {
                next = "SlopeSlide";
            }
            if (player.JumpButtonDown && player.currCoyoteTime > 0) {
                next = "Jump";
            }
            return next;
        }

        public override void Exit() {
        }

        private void FallFaster() {
            player.velocity.y -= Time.deltaTime * player.gravity;
        }
    }
    class SlopeSlide : PlayerState {

        public SlopeSlide(Player player) : base(player) {
            groundedState = true;
            name = "SlopeSlide";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("SlopeSlide");
            // player.velocity.y = 0;
            // Play jump animation
            player.DELETEDIS.color = Color.gray;
        }

        public override void Execute() {
            base.Execute();
            player.velocity.y -= (1 - player.actorController.CollInfo.slopeNormal.y) * player.gravity * Time.deltaTime;
            // player.accelTime = timeToAccel;
            // player.UpdateInputVelocity();
        }

        public override string Change() {
            string next = "";
            if (player.currJumpBuffer > 0 && player.IsGrounded) {
                next = "Jump";
            }
            if (!player.MaxSlope) {
                if (player.velocity.x != 0) {
                    next = "Run";
                }
                else {
                    next = "Idle";
                }
                if (!player.IsGrounded) {
                    next = "Fall";
                }
            }
            return next;
        }

        public override void Exit() {
        }
    }

    #endregion
}
}