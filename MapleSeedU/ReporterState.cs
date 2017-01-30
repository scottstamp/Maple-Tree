// Project: MapleSeedU
// File: ReporterState.cs
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

using XInputDotNetPure;

namespace MapleSeedU
{
    public class ReporterState
    {
        static PlayerIndex[] playerIndices = new PlayerIndex[] { PlayerIndex.One, PlayerIndex.Two, PlayerIndex.Three, PlayerIndex.Four };

        GamePadState[] gamePadStates = new GamePadState[4];
        uint[] lastPacketNumbers = new uint[4];
        int lastActivePlayerIndex = 0;

        public GamePadDeadZone DeadZone { get; set; }
        public int LastActiveIndex { get { return lastActivePlayerIndex; } }
        public GamePadState LastActiveState { get { return gamePadStates[lastActivePlayerIndex]; } }
        public bool LinkTriggersToVibration { get; set; }

        public ReporterState()
        {
            DeadZone = GamePadDeadZone.IndependentAxes;
        }

        public bool Poll()
        {
            for (int i = 0; i < 4; i++)
            {
                gamePadStates[i] = GamePad.GetState(playerIndices[i], DeadZone);
            }

            bool changed = true;

            int activePlayerIndex = lastActivePlayerIndex;
            for (int i = 0; i < 4; i++)
            {
                if (gamePadStates[i].PacketNumber != lastPacketNumbers[i])
                {
                    activePlayerIndex = i;
                    lastPacketNumbers[i] = gamePadStates[i].PacketNumber;
                    changed = true;
                }
            }

            lastActivePlayerIndex = activePlayerIndex;

            if (LinkTriggersToVibration)
            {
                GamePad.SetVibration(playerIndices[lastActivePlayerIndex], LastActiveState.Triggers.Left, LastActiveState.Triggers.Right);
            }
            else
            {
                GamePad.SetVibration(playerIndices[lastActivePlayerIndex], 0.0f, 0.0f);
            }

            return changed;
        }
    }
}
