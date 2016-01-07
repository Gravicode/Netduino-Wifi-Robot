using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace NetBot
{

    /// <summary>
    /// Class for controlling the HC-SR04 Ultrasonic Range detector
    /// Written by John E. Wilson
    /// Version 1.1 - 2012/04/03 - Corrected constructor pin documentation
    /// Free to use, please attribute credit
    /// </summary>
    public class HC_SR04
    {
        private OutputPort portOut;
        private InterruptPort interIn;
        private long beginTick;
        private long endTick;
        private long minTicks;  // System latency, subtracted off ticks to find actual sound travel time
        private double inchConversion;
        private double version;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pinTrig">Netduino pin connected to the HC-SR04 Trig pin</param>
        /// <param name="pinEcho">Netduino pin connected to the HC-SR04 Echo pin</param>
        public HC_SR04(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
        {
            portOut = new OutputPort(pinTrig, false);
            interIn = new InterruptPort(pinEcho, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            interIn.OnInterrupt += new NativeEventHandler(interIn_OnInterrupt);
            minTicks = 6200L;
            inchConversion = 1440.0;
            version = 1.1;
        }

        /// <summary>
        /// Returns the library version number
        /// </summary>
        public double Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// Trigger a sensor reading
        /// Convert ticks to distance using TicksToInches below
        /// </summary>
        /// <returns>Number of ticks it takes to get back sonic pulse</returns>
        public long Ping()
        {
            // Reset Sensor
            portOut.Write(true);
            Thread.Sleep(1);

            // Start Clock
            endTick = 0L;
            beginTick = System.DateTime.Now.Ticks;
            // Trigger Sonic Pulse
            portOut.Write(false);

            // Wait 1/20 second (this could be set as a variable instead of constant)
            Thread.Sleep(50);

            if (endTick > 0L)
            {
                // Calculate Difference
                long elapsed = endTick - beginTick;

                // Subtract out fixed overhead (interrupt lag, etc.)
                elapsed -= minTicks;
                if (elapsed < 0L)
                {
                    elapsed = 0L;
                }

                // Return elapsed ticks
                return elapsed;
            }

            // Sonic pulse wasn't detected within 1/20 second
            return -1L;
        }

        /// <summary>
        /// This interrupt will trigger when detector receives back reflected sonic pulse       
        /// </summary>
        /// <param name="data1">Not used</param>
        /// <param name="data2">Not used</param>
        /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
        void interIn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            // Save the ticks when pulse was received back
            endTick = time.Ticks;
        }

        /// <summary>
        /// Convert ticks to inches
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public double TicksToInches(long ticks)
        {
            return (double)ticks / inchConversion;
        }

        /// <summary>
        /// The ticks to inches conversion factor
        /// </summary>
        public double InchCoversionFactor
        {
            get
            {
                return inchConversion;
            }
            set
            {
                inchConversion = value;
            }
        }

        /// <summary>
        /// The system latency (minimum number of ticks)
        /// This number will be subtracted off to find actual sound travel time
        /// </summary>
        public long LatencyTicks
        {
            get
            {
                return minTicks;
            }
            set
            {
                minTicks = value;
            }
        }
    }

}
