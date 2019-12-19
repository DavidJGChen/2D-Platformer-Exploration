using System.Collections;
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
    public float minJumpHeight = 2f;
    public float timeToJumpApex = 0.4f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.5f;
    public float bounceToJumpBuffer = 0.2f;
    #endregion
    #region Private float settings
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private float accelTime;
    #endregion
    #region Timers/Countdowns
    private float currCoyoteTime;
    private float currJumpBuffer;
    private float currBounceToJumpBuffer;
    #endregion
    #region States
    // private string[] states = {"Idle", "Run", "Jump", "Fall", "SlopeSlide", "Bounce"};
    private string[] states = {"Idle", "Run", "Jump", "Fall", "Bounce"};
    #endregion

    private Vector2 dirInput;
    private bool jumpButtonDown;
    private bool jumpButtonUp;
    private bool walkInput;
    private bool bounceUp;
    private bool lateBounceJump;

    private Vector2 velocity;
    private float velocitySmoothingX;

    private CollisionDelegate onCollideH;
    private CollisionDelegate onCollideV;

    void Awake() {
        actorController = GetComponent<ActorController>();
        stateMachine = new PlayerStateMachine(new List<string>(states), "Idle", this);
        DELETEDIS = GetComponent<SpriteRenderer>();

        onCollideH = OnCollideH;
        onCollideV = OnCollideV;
    }

    void Start() {
        gravity = (2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = gravity * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * gravity * minJumpHeight);
    }

    void FixedUpdate() {

        stateMachine.Run();

        CalcGravity();

        bounceUp = false;
        lateBounceJump = false;

        actorController.Move(velocity * Time.deltaTime, onCollideH, onCollideV);

        jumpButtonDown = false; // Should probably move these
        jumpButtonUp = false; // Should probably move this
    }

    #region Collision delegates
    private void OnCollideV(RaycastHit2D hit) {
        if (hit.collider.CompareTag("Bouncy") && hit.normal.y > 0 && velocity.y < -10f) {
            bounceUp = true;
        }
    }
    private void OnCollideH(RaycastHit2D hit) {
        // velocity.x = 0;
        DELETEDIS.color = Color.magenta;
    }
    #endregion

    #region Public Methods
    public void ChangeColor(Color c) {
        DELETEDIS.color = c;
    }
    #endregion

    #region Movement
    private void UpdateInputVelocity() {
        float walkModifier = walkInput && stateMachine.CurrState == "Run" ? 0.5f : 1f;
        float targetVelocityX = dirInput.x * moveSpeed * walkModifier;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocitySmoothingX, accelTime);
    }
    private void CalcGravity() {
        velocity.y -= gravity * Time.deltaTime;
    }
    #endregion

    #region Countdowns/Timers
    private void RefreshCountdowns() {
        if (jumpButtonDown) {
            currJumpBuffer = jumpBuffer;
        }
    }
    private void Countdowns() {
        if (currJumpBuffer > 0) {
            currJumpBuffer -= Time.deltaTime;
        }
    }
    private void RefreshGroundedCountdowns() {
        currCoyoteTime = coyoteTime;
    }

    private void AirborneCountdowns() {
        if (currCoyoteTime > 0) {
            currCoyoteTime -= Time.deltaTime;
        }
    }

    private void RefreshAirborneCountdowns() {}

    private void GroundedCountdowns() {}
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
    public bool JumpButtonUp {
        get {
            return jumpButtonUp;
        }
        set {
            jumpButtonUp = value;
        }
    }

    public bool WalkInput {
        get {
            return walkInput;
        }
        set {
            walkInput = value;
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
    // private bool MaxSlope {
    //     get {
    //         return actorController.CollInfo.maxSlope;
    //     }
    // }
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
            // case "SlopeSlide":
            //     newState = new SlopeSlide(player);
            //     break;
            case "Bounce":
                newState = new Bounce(player);
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
                player.GroundedCountdowns();
                player.RefreshGroundedCountdowns();
            }
            else {
                player.AirborneCountdowns();
                player.RefreshAirborneCountdowns();
            }
            player.Countdowns();
            player.RefreshCountdowns();
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
            player.velocitySmoothingX = 0;
            player.velocity.y = 0;
        }

        public override string Change() {
            string next = "";
            if (player.dirInput.x != 0) {
                next = "Run";
            }
            // if (player.MaxSlope) {
            //     next = "SlopeSlide";
            // }
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

        private float timeToAccel = 0.04f;
        private float timeToDecel = 0.015f;
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
            // if (player.MaxSlope) {
            //     next = "SlopeSlide";
            // }
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

        private float timeToAccel = 0.3f;

        public Jump(Player player) : base(player) {
            groundedState = false;
            name = "Jump";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Jump");
            player.currJumpBuffer = 0;

            float jumpVelocity = player.bounceUp ? player.maxJumpVelocity * 1.25f : player.maxJumpVelocity;
            if (player.lateBounceJump) {
                jumpVelocity = player.maxJumpVelocity * 1.15f;
            }

            // if (player.MaxSlope) {
            //     Debug.Log("max slope detected");
            //     var slopeNormal = player.actorController.CollInfo.slopeNormal;
            //     if (player.DirInput.x != 0 && player.DirInput.x != Mathf.Sign(slopeNormal.x)) {
            //         player.velocity = (slopeNormal + Vector2.up).normalized * jumpVelocity;
            //     }
            //     else {
            //         player.velocity = slopeNormal * jumpVelocity;
            //     }
            // }
            // else {
            //     Debug.Log("no slope detected");
            //     player.velocity.y = jumpVelocity;
            // }

            player.velocity.y = jumpVelocity;
            // Play jump animation
            player.DELETEDIS.color = Color.green;
        }

        public override void Execute() {
            base.Execute();
            if (player.CollidingAbove) {
                player.velocity.y = 0;
            }
            if (player.JumpButtonUp && player.velocity.y > player.minJumpVelocity) {
                player.velocity.y = player.minJumpVelocity;
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

        private float timeToAccel = 0.6f;

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
                if (player.bounceUp && player.velocity.y < -10f) {
                    next = "Bounce";
                }
                if (player.currJumpBuffer > 0) {
                    next = "Jump";
                }
            }
            if (player.JumpButtonDown && player.currCoyoteTime > 0) {
                next = "Jump";
            }
            // if (player.MaxSlope) {
            //     next = "SlopeSlide";
            // }
            return next;
        }

        public override void Exit() {
        }

        private void FallFaster() {
            player.velocity.y -= Time.deltaTime * player.gravity;
        }
    }
    // class SlopeSlide : PlayerState {

    //     public SlopeSlide(Player player) : base(player) {
    //         groundedState = true;
    //         name = "SlopeSlide";
    //     }

    //     public override void Enter() {
    //         base.Enter();
    //         Debug.Log("SlopeSlide");
    //         player.velocity.x = 0;
    //         // player.velocity.y = 0;
    //         // Play jump animation
    //         player.DELETEDIS.color = Color.gray;
    //     }

    //     public override void Execute() {
    //         base.Execute();
    //         player.velocity.y -= (1 - player.actorController.CollInfo.slopeNormal.y) * player.gravity * Time.deltaTime;
    //         // player.accelTime = timeToAccel;
    //         // player.UpdateInputVelocity();
    //     }

    //     public override string Change() {
    //         string next = "";
    //         if (player.currJumpBuffer > 0 && player.IsGrounded) {
    //             next = "Jump";
    //         }
    //         // if (!player.MaxSlope) {
    //         //     if (player.velocity.x != 0) {
    //         //         next = "Run";
    //         //     }
    //         //     else {
    //         //         next = "Idle";
    //         //     }
    //         //     if (!player.IsGrounded) {
    //         //         next = "Fall";
    //         //     }
    //         // }
    //         if (player.velocity.x != 0) {
    //                 next = "Run";
    //             }
    //             else {
    //                 next = "Idle";
    //             }
    //             if (!player.IsGrounded) {
    //                 next = "Fall";
    //             }
    //         return next;
    //     }

    //     public override void Exit() {
    //     }
    // }

    class Bounce : PlayerState {

        private float timeToAccel = 0.3f;

        public Bounce(Player player) : base(player) {
            groundedState = false;
            name = "Bounce";
        }

        public override void Enter() {
            base.Enter();
            Debug.Log("Bounce");
            player.currJumpBuffer = 0; // TODO delete?
            player.velocity.y = -1f * player.velocity.y * 0.5f;
            player.currBounceToJumpBuffer = player.bounceToJumpBuffer;
            // Play bounce animation
            player.DELETEDIS.color = Color.yellow;
        }

        public override void Execute() {
            base.Execute();
            if (player.CollidingAbove) {
                player.velocity.y = 0;
                player.currBounceToJumpBuffer = 0;
            }
            if (player.currBounceToJumpBuffer > 0) {
                player.currBounceToJumpBuffer -= Time.deltaTime;
            }
            player.accelTime = timeToAccel;
            player.UpdateInputVelocity();
        }

        public override string Change() {
            string next = "";
            if (player.currBounceToJumpBuffer > 0 && player.currJumpBuffer > 0) {
                player.lateBounceJump = true; // Remove when implementing state machine stack
                next = "Jump"; // TODO fix
            }
            if (player.velocity.y < 0) {
                next = "Fall";
            }
            return next;
        }

        public override void Exit() {
        }
    }

    #endregion
}
}