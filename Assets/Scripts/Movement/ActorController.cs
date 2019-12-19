using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dawid {
[RequireComponent(typeof(BoxCollider2D))]
public class ActorController : MonoBehaviour
{
    private Player player;

    public float maxSlopeAngle = 45f;
    private BoxCollider2D coll;
    private RayOrigins rayOrigins;
    private CollisionInfo collInfo;
    private List<Collider2D> currCollidersH;
    private List<Collider2D> currCollidersV;
    private Stack<RaycastHit2D> currRaycastHitsH;
    private Stack<RaycastHit2D> currRaycastHitsV;
    
    public LayerMask collisionMask;
    public LayerMask passthroughMask;

    #region Public Items
    public CollisionInfo CollInfo {
        get {
            return collInfo;
        }
    }
    #endregion

    #region Raycast Section
    [Range(0.01f, 0.50f)]
    public float skinWidth = 0.02f;
    [Range(0.01f, 1.00f)]
    public float maxDistanceBetweenRays = 0.25f;
    private int horizontalRayCount;
    private int verticalRayCount;
    private float horizontalRaySpacing;
    private float verticalRaySpacing;

    private void CalculateRaySpacing() {
        var bounds = coll.bounds;
        bounds.Expand(skinWidth * -2);

        float width = bounds.size.x;
        float height = bounds.size.y;

        horizontalRayCount = Mathf.CeilToInt(height / maxDistanceBetweenRays);
        verticalRayCount = Mathf.CeilToInt(width / maxDistanceBetweenRays);

        horizontalRaySpacing = height / (horizontalRayCount - 1);
        verticalRaySpacing = width / (verticalRayCount - 1);
    }

    private void UpdateRaycastOrigins() {
        var bounds = coll.bounds;
        bounds.Expand(skinWidth * -2);

        rayOrigins.bl = bounds.min;
        rayOrigins.br = new Vector2(bounds.max.x, bounds.min.y);
        rayOrigins.tr = bounds.max;
        rayOrigins.tl = new Vector2(bounds.min.x, bounds.max.y);
    }

    public struct RayOrigins {
        public Vector2 tl, tr;
        public Vector2 bl, br;
    }
    #endregion

    void Awake() {
        coll = GetComponent<BoxCollider2D>();
        currCollidersH = new List<Collider2D>();
        currRaycastHitsH = new Stack<RaycastHit2D>();
        currCollidersV = new List<Collider2D>();
        currRaycastHitsV = new Stack<RaycastHit2D>();
        player = GetComponent<Player>();
    }

    void Start() {
        CalculateRaySpacing();
    }

    #region Public Methods
    public void Move(Vector2 deltaMove, Player.CollisionDelegate onCollideH, Player.CollisionDelegate onCollideV) {
        UpdateRaycastOrigins();
        ResetCollisionInfo();
        currCollidersH.Clear();
        currRaycastHitsH.Clear();
        currCollidersV.Clear();
        currRaycastHitsV.Clear();

        if (deltaMove.y < 0) {
            // SlideMaxSlope(ref deltaMove);
            // if (!collInfo.maxSlope && deltaMove.x != 0) {
            //     DescendSlope(ref deltaMove);
            // }
            if (deltaMove.x != 0) DescendSlope(ref deltaMove);
        }

        HorizontalCollisions(ref deltaMove);

        if (deltaMove.y != 0) {
            VerticalCollisions(ref deltaMove);
        }

        if (collInfo.ascendingSlope) {
            AscendSlopeAdjustSteeper(ref deltaMove);
        }

        transform.Translate(deltaMove);

        // Deal with collisions here
        float minDistH = -1f;
        while (currRaycastHitsH.Count > 0) {
            var hit = currRaycastHitsH.Pop();
            if (minDistH == -1f) {
                minDistH = hit.distance;
            }
            else if (hit.distance > minDistH) {
                break;
            }
            onCollideH(hit);
        }
        float minDistV = -1f;
        while (currRaycastHitsV.Count > 0) {
            var hit = currRaycastHitsV.Pop();
            if (minDistV == -1f) {
                minDistV = hit.distance;
            }
            else if (hit.distance > minDistV) {
                break;
            }
            onCollideV(hit);
        }
    }
    #endregion

    #region Collider section
    private void AddHitH(RaycastHit2D hit) {
        if (!currCollidersH.Contains(hit.collider)) {
            if (currRaycastHitsH.Count == 0 || currRaycastHitsH.Peek().distance >= hit.distance) {
                currCollidersH.Add(hit.collider);
                currRaycastHitsH.Push(hit);
            }
        }
    }
    private void AddHitV(RaycastHit2D hit) {
        if (!currCollidersV.Contains(hit.collider)) {
            if (currRaycastHitsV.Count == 0 || currRaycastHitsV.Peek().distance >= hit.distance) {
                currCollidersV.Add(hit.collider);
                currRaycastHitsV.Push(hit);
            }
        }
    }
    #endregion

    #region Move Methods
    private void HorizontalCollisions(ref Vector2 deltaMove) {
        float dirX = Mathf.Sign(deltaMove.x);
        float rayLength = Mathf.Abs(deltaMove.x) + skinWidth;

        var rayOrigin = dirX < 0 ? rayOrigins.bl : rayOrigins.br;
        RaycastHit2D hit;

        for (int i = 0; i < horizontalRayCount; i++) {

            // Debug
            Color c = Color.green;

            hit = Physics2D.Raycast(rayOrigin, dirX * Vector2.right, rayLength, collisionMask | passthroughMask);

            if (hit) {

                if (hit.collider.isTrigger) { // Move somewhere else
                    AddHitH(hit);
                    hit = Physics2D.Raycast(rayOrigin, dirX * Vector2.right, rayLength, collisionMask);
                    if (!hit) continue;
                }

                if (hit.distance == 0) continue;

                c = Color.red;

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && deltaMove.x != 0) {
                    if (slopeAngle <= maxSlopeAngle) {
                        collInfo.descendingSlope = false;
                            
                        float distToSlope = hit.distance - skinWidth;
                        
                        deltaMove.x -= distToSlope * dirX;

                        AscendSlope(ref deltaMove, slopeAngle);

                        deltaMove.x += distToSlope * dirX;
                    }
                }

                if (!collInfo.ascendingSlope || slopeAngle > maxSlopeAngle) {
                    deltaMove.x = (hit.distance - skinWidth) * dirX;
                    rayLength = hit.distance;

                    if (collInfo.ascendingSlope) {
                        deltaMove.y = Mathf.Tan(collInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x);
                    }

                    AddHitH(hit);

                    if (dirX < 0) collInfo.left = true;
                    else collInfo.right = true;
                }
            }

            Debug.DrawRay(rayOrigin, dirX * Vector2.right, c);

            rayOrigin += Vector2.up * horizontalRaySpacing;
        }
    }
    private void VerticalCollisions(ref Vector2 deltaMove) {
        float dirY = Mathf.Sign(deltaMove.y);
        float rayLength = Mathf.Abs(deltaMove.y) + skinWidth;

        var rayOrigin = (dirY < 0 ? rayOrigins.bl : rayOrigins.tl) + Vector2.right * deltaMove.x;
        RaycastHit2D hit;

        for (int i = 0; i < verticalRayCount; i++) {

            // Debug
            Color c = Color.green;

            hit = Physics2D.Raycast(rayOrigin, dirY * Vector2.up, rayLength, collisionMask | passthroughMask);

            if (hit) {

                if (hit.collider.isTrigger) {
                    AddHitV(hit);
                    hit = Physics2D.Raycast(rayOrigin, dirY * Vector2.up, rayLength, collisionMask);
                    if (!hit) continue;
                }

                if (hit.distance == 0) continue;

                AddHitV(hit);

                deltaMove.y = (hit.distance - skinWidth) * dirY;
                rayLength = hit.distance;
                // Debug
                c = Color.red;

                if (collInfo.ascendingSlope) {
                    deltaMove.x = deltaMove.y / Mathf.Tan(collInfo.slopeAngle * Mathf.Deg2Rad);
                }

                if (dirY < 0) collInfo.below = true;
                else collInfo.above = true;
            }

            Debug.DrawRay(rayOrigin, dirY * Vector2.up, c);

            rayOrigin += Vector2.right * verticalRaySpacing;
        }

    }
    private void AscendSlope(ref Vector2 deltaMove, float slopeAngle) {
        float dirX = Mathf.Sign(deltaMove.x);
        float magnitude = Mathf.Abs(deltaMove.x);

        float dY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * magnitude;

        if (deltaMove.y > dY) return;

        deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * magnitude * dirX;
        deltaMove.y = dY;

        collInfo.slopeAngle = slopeAngle;
        collInfo.ascendingSlope = true;
        collInfo.below = true;
    }

    private void AscendSlopeAdjustSteeper(ref Vector2 deltaMove) {
        float dirX = Mathf.Sign(deltaMove.x);
        float rayLength = Mathf.Abs(deltaMove.x) + skinWidth;
        var rayOrigin = (dirX < 0 ? rayOrigins.bl : rayOrigins.br) + Vector2.up * deltaMove.y;

        var hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionMask);

        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeAngle != collInfo.slopeAngle) {
                deltaMove.x = (hit.distance - skinWidth) * dirX;
                // deltaMove.y = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x);
                collInfo.slopeAngle = slopeAngle;
            }
        }
    }

    private void DescendSlope(ref Vector2 deltaMove) {
        float dirX = Mathf.Sign(deltaMove.x);
        float rayLength = Mathf.Abs(deltaMove.x) + skinWidth;
        var rayOrigin = dirX < 0 ? rayOrigins.br : rayOrigins.bl;

        var hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, collisionMask);

        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeAngle == 0 || Mathf.Sign(hit.normal.x) != dirX) return;

            float moveDist = Mathf.Abs(deltaMove.x);

            float dY = moveDist * Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

            if (hit.distance - skinWidth > dY) return;

            deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDist * dirX;
            deltaMove.y = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDist * -1;

            AddHitV(hit);

            collInfo.slopeAngle = slopeAngle;
            collInfo.descendingSlope = true;
            collInfo.below = true;
        }

    }

    // private void SlideMaxSlope(ref Vector2 deltaMove) {
    //     // float errorFloat = 0.1f;
    //     float rayLength = Mathf.Abs(deltaMove.y) + skinWidth;

    //     var leftHit = Physics2D.Raycast(rayOrigins.bl, Vector2.down, rayLength, collisionMask);
	// 	var rightHit = Physics2D.Raycast(rayOrigins.br, Vector2.down, rayLength, collisionMask);
        
    //     if (leftHit || rightHit) {

    //         var hit = rightHit ? rightHit : leftHit;

    //         if (leftHit && rightHit) {
    //             hit = rightHit.distance > leftHit.distance ? leftHit : rightHit;
    //         }
            
    //         var slopeNormal = hit.normal;
    //         float slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);

    //         if (slopeAngle > maxSlopeAngle) {
    //             float mag = Mathf.Abs(deltaMove.y) - (hit.distance - skinWidth);
    //             float dX = Mathf.Sign(hit.normal.x) * mag * Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
    //             if (Mathf.Sign(deltaMove.x) != Mathf.Sign(dX) || Mathf.Abs(deltaMove.x) < Mathf.Abs(dX)) {

    //                 AddHit(hit);

    //                 collInfo.slopeAngle = slopeAngle;
    //                 collInfo.maxSlope = true;
    //                 collInfo.slopeNormal = slopeNormal;
    //                 collInfo.below = true;

    //                 deltaMove.x = dX;
    //                 deltaMove.y = -1 * mag * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);

    //                 float dirX = Mathf.Sign(deltaMove.x);

    //                 var rayOrigin = dirX < 0 ? rayOrigins.bl : rayOrigins.br;

    //                 float skinWidthAngled = skinWidth / Mathf.Cos((90f - slopeAngle) * Mathf.Deg2Rad);

    //                 hit = Physics2D.Raycast(rayOrigin, deltaMove.normalized, mag + skinWidthAngled, collisionMask); // Math not correct here
    //                 if (hit) {
    //                     mag = hit.distance - skinWidthAngled;
    //                     deltaMove.x = dirX * mag * Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
    //                     deltaMove.y = -1 * mag * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
                        
    //                     collInfo.maxSlope = false;
    //                 }
    //             }
    //         }
    //     }
    // }
    #endregion

    #region Collision Info
    private void ResetCollisionInfo() {
        collInfo.prevSlopeAngle = collInfo.slopeAngle;

        collInfo.above = false;
        collInfo.below = false;
        collInfo.left = false;
        collInfo.right = false;
        collInfo.ascendingSlope = false;
        collInfo.descendingSlope = false;
        // collInfo.maxSlope = false;
        collInfo.slopeAngle = 0;
        collInfo.slopeNormal = Vector2.zero;
    }
    public struct CollisionInfo {
        public bool above, below;
        public bool left, right;
        public bool ascendingSlope;
        public bool descendingSlope;
        // public bool maxSlope;
        public float slopeAngle;
        public Vector2 slopeNormal;
        public float prevSlopeAngle;
    }
    #endregion
}
}