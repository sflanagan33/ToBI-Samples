using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TOBI.Core;
using TOBI.Damage;

namespace TOBI.Characters.Boyd
{
    public class Boyd : Character<BoydState, BoydInput, BoydTransitionInfo>, IDamageReceiver
    {
        [SerializeField]
        private Animator anim;
        public new Animator animation { get { return anim; } }

        [SerializeField]
        private BoydSoundEffects sfx;
        public BoydSoundEffects soundEffects { get { return sfx; } }

        [SerializeField]
        private Rigidbody rb;
        public new Rigidbody rigidbody { get { return rb; } }

        [SerializeField]
        private BoxCollider[] hb;
        public BoxCollider[] hitboxes { get { return hb; } }
        
        private void Start()
        {
            NewState<States.BoydFlyState>();
        }

        protected override void ReadInput()
        {
            if (Game.objects.luke)
                input.target = Game.objects.luke.transform;
        }

        void IDamageReceiver.TakeDamage(DamageQuery damage)
        {
            input.damage = damage;
        }

        bool IDamageReceiver.IsVulnerableToPlayer()
        {
            return true;
        }
    }
    
    public class BoydInput : CharacterInput
    {
        public Transform target;
        public DamageQuery? damage;

        public override void Clean()
        {
            damage = null;
        }
    }

    public abstract class BoydTransitionInfo : CharacterTransitionInfo
    {

    }
}