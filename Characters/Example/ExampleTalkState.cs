using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Example.States
{
    // The scripts in this folder show how to set up a new character for TOBI!

    // Here's a talk state belonging to the Example character. Note how this state has
    // transition info: specifically, what the character should say when the state is entered.

    public class ExampleTalkState : ExampleState
    {
        // Hey look, states can hold data and act on it when animation or logic is run.

        private float talkTime;
        private const float talkTimeMax = 5f;

        public override void Enter(ExampleTransitionInfo info = null)
        {
            // Note that the cast below assumes we received the right info, even though it's optional.
            // Since WalkState always passes info when calling NewState<ExampleTalkState>, this is a safe assumption,
            // but something you need to design when you create your own states / transitions.

            ExampleTalkTransitionInfo i = (ExampleTalkTransitionInfo) info;

            // We got what to say, so say it (or etc)

            Debug.Log(i.whatToSay);

            // Play talking animation, etc.
        }

        public override void RunAnimation(ExampleInput input)
        {
            // Alter talking animation, play talking sounds, etc.
        }

        public override bool RunLogic(ExampleInput input)
        {
            // If we've talked for long enough, go back to walking

            if (talkTime < talkTimeMax)
                talkTime += Time.deltaTime;

            else
            {
                example.NewState<ExampleWalkState>();
                return true;
            }

            // Otherwise, we're still talking

            return false;
        }

        public override void Exit()
        {
            // Do cleanup, etc.
        }
    }

    // The transition info that this specific state requires: a string of what to talk about.

    public class ExampleTalkTransitionInfo : ExampleTransitionInfo
    {
        public string whatToSay;
    }
}