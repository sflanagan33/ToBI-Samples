using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Boyd
{
    public class BoydTail : MonoBehaviour
    {
        public Transform tailBase;
        public Transform[] tailBones;

        private const int pointCount = 5;
        public float pointDistance = 0.1f;
        public float pointMaxDistance = 0.1f;
        public float pointAccel = 0.2f;
        public float pointDecel = 0.4f;

        private Vector3[] points = new Vector3[pointCount];
        private Vector3[] velocities = new Vector3[pointCount];
        private int frame = 0;

        private void ResetPositions()
        {
            for (int i = 0; i < pointCount; i++)
                points[i] = tailBase.position - (tailBase.right * (i + 1) * pointDistance);
        }

        private void LateUpdate()
        {
            if (frame < 10)
            {
                ResetPositions();
                frame++;
                return;
            }

            float dt = Time.deltaTime * 60f;

            Vector3 twoBefore = tailBase.position + tailBase.right;
            Vector3 oneBefore = tailBase.position;

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 forward = (oneBefore - twoBefore).normalized;
                Vector3 target = oneBefore + forward * pointDistance;

                velocities[i] += (target - points[i]) * pointAccel * dt;
                velocities[i] -= Vector3.up * 0.01f;
                velocities[i] *= 1 - (pointDecel * dt);

                points[i] += velocities[i] * dt;

                Vector3 diff = points[i] - oneBefore;
                if (diff.magnitude > pointMaxDistance)
                    points[i] = oneBefore + diff.normalized * pointMaxDistance;

                tailBones[i].position = points[i];

                /*RaycastHit hit;
                if (Physics.Raycast(oneBefore, forward, out hit, pointDistance * 2))
                    points[i] += hit.normal * 0.05f;*/

                twoBefore = oneBefore;
                oneBefore = points[i];
            }
        }
    }
}