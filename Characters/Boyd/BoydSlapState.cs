using UnityEngine;
using System.Collections;

using TOBI.Damage;

namespace TOBI.Characters.Boyd.States
{
    public class BoydSlapState : BoydAliveState
    {
        private int time = 0;
        private int damageStart = 9;
        private int damageEnd = 18;
        private int attackEnd = 45;

        public override void Enter(BoydTransitionInfo info = null)
        {
            boyd.animation.SetTrigger("Slap");
            boyd.soundEffects.Slap();
        }

        public override void RunAnimation(BoydInput input)
        {

        }

        public override bool RunLogic(BoydInput input)
        {
            if (base.RunLogic(input))
                return true;

            boyd.rigidbody.velocity *= 0.5f;

            time++;

            if (time == damageStart)
            {
                foreach (BoxCollider b in boyd.hitboxes)
                    b.enabled = true;
            }

            else if (time == damageEnd)
            {
                foreach (BoxCollider b in boyd.hitboxes)
                    b.enabled = false;
            }

            else if (time == attackEnd)
            {
                boyd.NewState<BoydFlyState>();
                return true;
            }

            return false;
        }

        public override void Exit()
        {
            foreach (BoxCollider b in boyd.hitboxes)
                b.enabled = false;
        }
    }
}