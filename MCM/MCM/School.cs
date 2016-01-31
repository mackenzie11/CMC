using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VIKOR
{
    public class School
    {

        public int id { get; set; }
        public Dictionary<string, double> paramValues { get; set; }
        public string name { get; set; }

        public School(string schoolId, Dictionary<string, double> paramValues)
        {
            int temp = 0;
            Int32.TryParse(schoolId, out temp);

            id = temp;
            this.paramValues = paramValues;

        }




        public void calculateSandR()
        {
            double S = 0;
            List<double> temp = new List<double>();

            foreach (String s in paramValues.Keys)
            {
                double weight = Program.paramWeights[s];
                double best = Program.bestCriterion[s];
                double worst = Program.worstCriterion[s];

                double total = weight; //If the parameter is 0, that means we don't have information
                //so, we assign the worst possible score, which results in a value of weight*1. 
                if (paramValues[s] != 0)
                {
                    total = weight * (best - paramValues[s]) / (best - worst); 
                }

                temp.Add(total);
                S += total;
            }

            temp.Sort();
            double R = temp.Last();

            Program.schoolsR.Add(this, R);
            Program.schoolsS.Add(this, S);
        }


        public void calculateQ()
        {
            double SValue = Program.schoolsS[this];
            double RValue = Program.schoolsR[this];

            double firstTerm = Program.v * (SValue - Program.Smin) / (Program.Smax - Program.Smin);
            double secondTerm = (1 - Program.v) * (RValue - Program.Rmin) / (Program.Rmax - Program.Rmin);

            Program.schoolsQ.Add(this, firstTerm + secondTerm);
        }




        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == this.GetHashCode();
        }


        /// <summary>
        /// Important for use of dictionary.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return id;
        }
    }
}
