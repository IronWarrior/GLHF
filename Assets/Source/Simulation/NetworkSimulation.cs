using UnityEngine;

namespace GLHF.Network
{
    public class NetworkSimulation
    {            
        private readonly float deltaTime;
        private readonly JitterTimescale jitterTimescale;
        private readonly MessageBuffer<ServerInputMessage> states;

        private float confirmedTime;
        private float predictedTime;

        private float lastPredictedTimeAdjustement;

        public NetworkSimulation(float deltaTime, JitterTimescale jitterTimescale)
        {
            this.deltaTime = deltaTime;
            this.jitterTimescale = jitterTimescale;

            states = new MessageBuffer<ServerInputMessage>(deltaTime);
        }

        public void Insert(ServerInputMessage serverInputMessage, float time)
        {
            states.Insert(serverInputMessage, time);
        }

        public void Integrate(float currentTime, float additionalTime)
        {
            float error = states.CalculateError(deltaTime, currentTime, confirmedTime);
            float timescale = jitterTimescale.CalculateTimescale(error);

            confirmedTime += additionalTime * timescale;
        }       
        
        public bool TryPop(int tick, float currentTime, out ServerInputMessage serverInputMessage)
        {
            if (states.TryPop(tick, confirmedTime, deltaTime, out serverInputMessage))
            {
                if (currentTime > lastPredictedTimeAdjustement + 2f)
                {
                    predictedTime = Mathf.Max(0, predictedTime + serverInputMessage.RequestedInputTimingDelta);

                    lastPredictedTimeAdjustement = currentTime;
                }

                return true;
            }

            return false;
        }

        public int GetPredictedTickCount()
        {
            return Mathf.CeilToInt(predictedTime / deltaTime);
        }

        public int NextTickToEnter(int currentTick)
        {
            return states.NewestTick != -1 ? states.NewestTick + 1 : currentTick;
        }

        public void SetConfirmedTime(float time)
        {
            confirmedTime = time;
        }
    }
}
