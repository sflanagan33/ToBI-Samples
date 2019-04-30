using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Boyd.States
{
    public class BoydFlyState : BoydAliveState
    {
        private int flapTimer = 0;
        private float slapTimer = 2f;

        public override void Enter(BoydTransitionInfo info = null)
        {
            
        }

        public override void RunAnimation(BoydInput input)
        {
            // Play sounds

            int t = (int) boyd.animation.GetCurrentAnimatorStateInfo(0).normalizedTime;

            if (flapTimer != t)
            {
                boyd.soundEffects.Flap();
                flapTimer = t;
            }
        }

        public override bool RunLogic(BoydInput input)
        {
            if (base.RunLogic(input))
                return true;

            if (input.target != null)
            {
                // Turn to face the target

                Vector3 look = input.target.position - transform.position;
                look.Scale(new Vector3(1, 0, 1));

                Quaternion goalRot = Quaternion.LookRotation(look);
                boyd.rigidbody.MoveRotation(Quaternion.Slerp(boyd.rigidbody.rotation, goalRot, 0.05f));

                //Vector3 headLook = input.target.position - boyd.head.position;
                //Quaternion goalLookRot = Quaternion.LookRotation(headLook) * Quaternion.Euler(90, 0, -90);
                //boyd.head.rotation = Quaternion.Slerp(boyd.head.rotation, goalLookRot, 1f);

                // Move towards the target

                float followDist = 4f;
                float followHeight = 2.5f;

                float fullAwareness = 25f;
                float zeroAwareness = 30f;

                Vector3 diff = transform.position - input.target.position;
                float dist = diff.magnitude;

                diff.Scale(new Vector3(1, 0, 1));
                diff = Quaternion.Euler(0, 0.1f, 0) * diff;

                Vector3 goalPos = input.target.position + diff.normalized * followDist;
                goalPos.y += followHeight;

                float awareness = Mathf.Clamp01(Mathf.InverseLerp(zeroAwareness, fullAwareness, dist));
                float force = awareness * 40f;

                Vector3 f = (goalPos - boyd.rigidbody.position) * force;
                f = Vector3.ClampMagnitude(f, 200);

                boyd.rigidbody.AddForce(f);

                // If the target is close enough and the cooldown is up, slap

                bool close = (transform.position - input.target.position).magnitude < 3f;
                slapTimer = Mathf.Max(slapTimer - Time.deltaTime, 0);

                if (close && slapTimer == 0)
                {
                    boyd.NewState<BoydSlapState>();
                    return true;
                }
            }
            
            boyd.rigidbody.velocity *= 0.5f;

            return false;
        }

        public override void Exit()
        {

        }
    }
}