using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TOBI.Core;
using TOBI.Damage;

namespace TOBI.Characters.Boyd.States
{
    public class BoydDeathState : BoydState
    {
        private float timer = 0;
        private const float timerLength = 1;

        private Quaternion goalRot;

        public override void Enter(BoydTransitionInfo info = null)
        {
            boyd.gameObject.layer = LayerMask.NameToLayer("NoLukeCollision");

            boyd.animation.SetTrigger("Death");
            boyd.soundEffects.Death();

            BoydDeathTransitionInfo bdti = (BoydDeathTransitionInfo) info;
            boyd.rigidbody.velocity = Vector3.Slerp(bdti.damage.direction, Vector3.up, 0.25f) * 15f;
            boyd.rigidbody.useGravity = true;

            Vector3 look = Vector3.Scale(bdti.damage.direction, new Vector3(-1, 0, -1));
            goalRot = Quaternion.LookRotation(look);
        }

        public override void RunAnimation(BoydInput input)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, Mathf.Sqrt(timer)) * 1800f;
            Quaternion rot = Quaternion.Euler(-t, 0, 0);
            boyd.animation.transform.localRotation = rot;
        }

        public override bool RunLogic(BoydInput input)
        {
            Quaternion g = Quaternion.Slerp(boyd.rigidbody.rotation, goalRot, 0.25f);
            boyd.rigidbody.MoveRotation(g);

            if (timer > 1)
            {
                Game.effects.DeathPoof(transform.position, 5);
                Destroy(gameObject);
            }

            return false;
        }

        public override void Exit()
        {

        }
    }

    public class BoydDeathTransitionInfo : BoydTransitionInfo
    {
        public DamageQuery damage;
    }
}