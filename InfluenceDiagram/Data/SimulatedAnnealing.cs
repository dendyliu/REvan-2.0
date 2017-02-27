using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.Data
{
    // function that returns 0 when state = targetState, or nonnegative otherwise (tipically Abs(F(x)-targetValue)
    public delegate double EnergyFunction(double x);   
    public delegate void NotifyProgressFunction(int iteration);
    
    public class SimulatedAnnealingParams
    {
        public double stepSize;    // maximum step size in random walk
        public double initialTemp;
        public int maxIteration;
        public int freezeIteration = 0;  // how many last steps when temperature reach freezing point (epsilon)
        public double boltzmanConstant = 1.0;   // default is 1.0

    }

    public class SimulatedAnnealing
    {
        static Random rand = new Random();
        const double EPSILON = 1e-4;
        const double MIN_TEMPERATURE = 1e-10;

        // function that returns random neighbor state given current state and step size
        static double NeighborFunction(double x, double stepSize)
        {
            // noSwitchCount starts at 0, so must add +1
            double r = rand.NextDouble();
            return x + (2.0 * r - 1.0) * stepSize;
        }

        static double AcceptanceProbability(double energy, double newEnergy, double T, double K)
        {
            if (newEnergy < energy)
            {
                return 1.0;
            }
            else
            {
                return Math.Exp((energy - newEnergy) / (K * T));
            }
        }

        // formula: INIT*(factor^(maxIter-freezeIter)) = MIN
        // so factor = (MIN/INIT)^(1/(maxIter-freezeIter))
        static double CalculateCoolingFactor(double initialTemp, int maxIteration, int freezeIteration)
        {
            return Math.Pow(MIN_TEMPERATURE / initialTemp, 1.0 / (maxIteration - freezeIteration));
        }

        // find state that has minimum energy
        public static double FindMinimumState(double initialState, EnergyFunction energyFunc, SimulatedAnnealingParams param, NotifyProgressFunction notifyFunc)
        {
            double currentState = initialState;
            double currentEnergy = energyFunc(currentState);
            double bestState = currentState;
            double bestEnergy = currentEnergy;
            double T = param.initialTemp;
            double coolingFactor = CalculateCoolingFactor(param.initialTemp, param.maxIteration, param.freezeIteration);
            
            Console.WriteLine("state {0} energy {1} T {2} cooling {3} ", currentState, currentEnergy, T, coolingFactor);

            int i = 0;
            notifyFunc(i);
            // count how many times in a row the random walk has met a higher energy neighbour and not switch state
            // the search area should be wider as the count is higher
            int noSwitchCount = 0;
            double stepModifier = 1.0;
            while (i < param.maxIteration)
            {
                if (bestEnergy <= EPSILON && stepModifier <= EPSILON)
                {
                    break;
                }

                double newState = NeighborFunction(currentState, param.stepSize * stepModifier);
                if (newState != currentState)
                {
                    double newEnergy = energyFunc(newState);

                    Console.WriteLine("iteration {0} modifier {1} state {2} energy {3} newstate {4} energy {5} ", i, stepModifier, currentState, currentEnergy, newState, newEnergy);

                    bool takeStep = false;
                    if (Double.IsNaN(newEnergy))
                    {
                        // new energy has bad value, skip
                    }
                    else
                    {
                        if (newEnergy < bestEnergy)
                        {
                            bestState = newState;
                            bestEnergy = newEnergy;
                        }

                        if (newEnergy < currentEnergy)
                        {
                            takeStep = true;
                        }
                        else
                        {
                            double r = rand.NextDouble();
                            double prob = AcceptanceProbability(currentEnergy, newEnergy, T, param.boltzmanConstant);
                            takeStep = r < prob;
                            Console.WriteLine("energy {0} newenergy {1} T {2} prob {3}", currentEnergy, newEnergy, T, prob);
                        }

                        // use local gradient to estimate step modifier bounds
                        double gradient = Math.Min(Math.Max(Math.Abs((newEnergy - currentEnergy) / (newState - currentState)), EPSILON), 1.0e10);
                        if (takeStep)
                        {
                            noSwitchCount = 0;
                            currentState = newState;
                            currentEnergy = newEnergy;
                        }
                        else
                        {
                            ++noSwitchCount;
                        }

                        if (currentEnergy > T)
                        {
                            stepModifier = Math.Max(Math.Min(stepModifier * 2.0, bestEnergy / gradient), EPSILON);
                        }
                        else
                        {
                            stepModifier = Math.Max(stepModifier / 2.0, currentEnergy / gradient);
                        }
                    }
                }
                else
                {
                    // if newState == currentState skip
                }

                T = Math.Max(T * coolingFactor, MIN_TEMPERATURE);
                ++i;
                notifyFunc(i);
            }
            // rounding for cleaner result
            bestState = Math.Round(bestState, 9);

            return bestState;
        }
    }
}
