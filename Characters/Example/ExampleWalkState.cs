using UnityEngine;
using System.Collections;
using System;

namespace TOBI.Characters.Example.States
{
    // The scripts in this folder show how to set up a new character for TOBI!

    // Here's a walk state belonging to the Example character.

    public class ExampleWalkState : ExampleState
    {
        public override void Enter(ExampleTransitionInfo info = null)
        {
            // Start the walk animation, etc.
        }

        public override void RunAnimation(ExampleInput input)
        {
            // Here's where we might change the speed of the walk animation, play footstep sounds, etc.
        }

        public override bool RunLogic(ExampleInput input)
        {
            // Make this character move forward while it walks.
            // Note how we're using the "example" property - that's the instance of our owning character,
            // which we're accessing to get to its exposed rigidbody. The "<this character's name>" property
            // is defined in ExampleState, which this state is a subclass of.

            float dt = Time.deltaTime;
            example.body.AddForce(Vector3.forward * dt);

            // When this character's friend is close, it talks to them.
            
            if (input.friendIsClose)
            {
                ExampleTalkTransitionInfo info = new ExampleTalkTransitionInfo();
                info.whatToSay = "Hello, friend!";

                example.NewState<ExampleTalkState>(info);
                return true;
            }
            
            // But it occassionally talks to itself, too.

            else if (UnityEngine.Random.value < 0.01f)
            {
                ExampleTalkTransitionInfo info = new ExampleTalkTransitionInfo();
                info.whatToSay = "All by myself...";

                example.NewState<ExampleTalkState>(info);
                return true;
            }

            // Otherwise, keep on walkin'

            return false;
        }

        public override void Exit()
        {
            // Do cleanup, etc.
        }
    }
}