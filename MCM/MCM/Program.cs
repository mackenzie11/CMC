using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VIKOR
{
    class Program
    {

        //----------------------------------These are change-able constants------------------------------
        //These are the parameters we are looking at, and their weights (not sure if this is the best way to keep track).
        public static Dictionary<string, double> paramWeights = new Dictionary<string, double>(){
            { "md_earn_wne_p10", 0.16667}, {"GRAD_DEBT_MDN_SUPP", 0.16667 },  { "RET_FT4", 0.08333}, {"GRRTTOT..DRVGR2010.", 0.08333 },
            { "PCTFLOAN", 0.055556 }, {"ExpIn", 0.055556 }, {"GiftRev", 0.1111 }, {"TotalRev", 0.2222 }, {"ENDOW",0.055556 }
        };


        //public static Dictionary<string, double> paramWeights = new Dictionary<string, double>(){
        //    { "md_earn_wne_p10", 0.4}, {"GRAD_DEBT_MDN_SUPP", 0.3 },  { "RET_FT4", 0.15}, {"GRRTTOT..DRVGR2010.", 0.15 }
        //};

        //Maybe there's a better way to do this, but for now this just records whether a parameter
        //is of the type min-is-best (true) or max-is-best (false). 
        public static Dictionary<string, bool> minIsBest = new Dictionary<string, bool>()
        {
            { "md_earn_wne_p10", false}, {"GRAD_DEBT_MDN_SUPP", true}, { "RET_FT4", false }, {"GRRTTOT..DRVGR2010.", false },
          { "PCTFLOAN", true }, {"ExpIn", false }, {"GiftRev", true }, {"TotalRev", true }, {"ENDOW", true }
        };

        //public static Dictionary<string, bool> minIsBest = new Dictionary<string, bool>()
        //{
        //    { "md_earn_wne_p10", false}, {"GRAD_DEBT_MDN_SUPP", true}, { "RET_FT4", false }, {"GRRTTOT..DRVGR2010.", false },
        //};


        public static double v = 0.5; //This is the weight strategy

        public static Dictionary<string, int> eliminate = new Dictionary<string, int>()
        {
            {"HCM2", 1}
        };

        //-------------------------------------------------------------------------------------------------

        //The positions of the parameters columns in the csv file (not sure if we need this after initial reading,
        //but couuld be helpful)
        public static Dictionary<string, int> parameters;

        //The school objects, keyed by their name. 
        public static Dictionary<string, School> schools;


        //For each paramter in our parameters, calculates the best value and the worst value
        //(note that we do not need to know which school achieves these values)
        //(note 2: how do I distinguish between if a criterion is min-best or max-best??)
        public static Dictionary<string, double> bestCriterion;
        public static Dictionary<string, double> worstCriterion;


        //Filled once every school has their S and R values calculated 
        public static Dictionary<School, double> schoolsS;
        public static Dictionary<School, double> schoolsR;
        public static Dictionary<School, double> schoolsQ;


        public static double Smin;
        public static double Smax;
        public static double Rmin;
        public static double Rmax;


        static void Main(string[] args)
        {

            Console.WriteLine(readFile(paramWeights.Keys.ToList()));
            calculateMaxAndMin();

            schoolsS = new Dictionary<School, double>();
            schoolsR = new Dictionary<School, double>();
            schoolsQ = new Dictionary<School, double>();
            foreach (School school in schools.Values)
            {
                school.calculateSandR();
            }

            calculateMaxRandS();

            foreach (School school in schools.Values)
            {
                school.calculateQ();
            }
            schoolsQ = schoolsQ.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            //Now we have the schools sorted by Q, we need to check validity
            Dictionary<School, bool> check1 = new Dictionary<School, bool>();
            Dictionary<School, bool> check2 = new Dictionary<School, bool>();

            double D = 1 / (schoolsQ.Count() - 1);
            School currSchool = schoolsQ.ElementAt(0).Key;
            double currVal = schoolsQ.ElementAt(0).Value;
            for (int i = 1; i < schoolsQ.Count() - 1; i++)
            {
                School tempSchool = schoolsQ.ElementAt(i).Key;
                double tempVal = schoolsQ.ElementAt(i).Value;

                check1[currSchool] = tempVal - currVal > D;

                School bestR = schoolsR.ElementAt(0).Key;
                School bestS = schoolsS.ElementAt(0).Key;
                check2[currSchool] = (currSchool.Equals(bestR) | currSchool.Equals(bestS));

                //Not sure about this part???
                schoolsR.Remove(currSchool);
                schoolsS.Remove(currSchool);

                currSchool = tempSchool;
                currVal = tempVal;                
            }


            //Print the first 10
            for (int i = 0; i < 200; i++)
            {
                School s = schoolsQ.ElementAt(i).Key;
                double value = schoolsQ.ElementAt(i).Value;

                Console.WriteLine(s.name + "," + value + "," + check1[s] + "," + check2[s]);
            }




            //before your loop
            var csv = new StringBuilder();
            csv.AppendLine("UNITID,Rating");
            foreach (School s in schoolsQ.Keys)
            {
                csv.AppendLine(s.name + "," + schoolsQ[s]);
            }

            //after your loop
            File.WriteAllText(@"...\...\rankingNew.csv", csv.ToString());

            Console.WriteLine(schools.Count);
            Console.ReadLine();
        }


        /// <summary>
        /// Reads the file and creates all of the schools, adding them to the schools dictionary (do 
        /// we really want a dictionary?). Takes as input the names of the parameters we want to save.
        /// Returns true if we have found all the parameters, false otherwise. 
        /// </summary>
        /// <param name="paramsWeWant"></param>
        /// <returns></returns>
        public static bool readFile(List<string> paramsWeWant)
        {
            parameters = new Dictionary<string, int>();
            schools = new Dictionary<string, School>();

            var reader = new StreamReader(File.OpenRead(@"...\...\Everything.csv"));

            //First, get the positions of the columns we want
            var line = reader.ReadLine();
            var values = line.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                string currentVal = values[i];
                if (paramsWeWant.Contains(currentVal))
                {
                    parameters.Add(currentVal, i);
                    paramsWeWant.Remove(currentVal);
                }

              //  if (eliminate.Keys.Contains(currentVal))
            }



            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                values = line.Split(',');

                Dictionary<string, double> paramValues = new Dictionary<string, double>();
                foreach (string s in parameters.Keys)
                {
                    double paramValue = 0; //Do I need to put a check in, or should I assume it always works?
                    Double.TryParse(values[parameters[s]], out paramValue);
                    paramValues.Add(s, paramValue);
                }


                string schoolId = values[1]; //Not sure if this will always be four?
                School school = new School(schoolId, paramValues);
                school.name =values[7];
                schools.Add(schoolId, school);
            }


            return paramsWeWant.Count == 0;

        }


        /// <summary>
        /// Calculate the max and min for each criterion function. 
        /// Note, I need to adjust for ones where having lower is better!!! (i.e. cost!!)
        /// </summary>
        public static void calculateMaxAndMin()
        {
            bestCriterion = new Dictionary<string, double>();
            worstCriterion = new Dictionary<string, double>();

            foreach (string s in paramWeights.Keys)
            {
                List<double> temp = new List<double>();
                foreach (School school in schools.Values)
                {
                    temp.Add(school.paramValues[s]); //Add all the schools values to the list
                }

                //Sort the list, Note: not sure if lowest is first, or what? Need to check!!!
                temp.Sort();

                if (minIsBest[s])
                {
                    bestCriterion.Add(s, temp.First());
                    worstCriterion.Add(s, temp.Last());
                }
                else
                {
                    bestCriterion.Add(s, temp.Last());
                    worstCriterion.Add(s, temp.First());
                }
            }


        }

        /// <summary>
        /// Calculates the special R and S values that we need. 
        /// </summary>
        public static void calculateMaxRandS()
        {

            //List<KeyValuePair<School, double>> SList = schoolsS.ToList();

            //SList.Sort((firstPair, nextPair) =>
            //{
            //    return firstPair.Value.CompareTo(nextPair.Value);
            //}
            //);
            //schoolsS = SList.To

            //List<KeyValuePair<School, double>> RList = schoolsR.ToList();

            //RList.Sort((firstPair, nextPair) =>
            //{
            //    return firstPair.Value.CompareTo(nextPair.Value);
            //}
            //);

            //Check that this works!!
            schoolsS = schoolsS.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            schoolsR = schoolsR.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            Smin = schoolsS.ElementAt(0).Value;
            Smax = schoolsS.ElementAt(schoolsS.Count - 1).Value;
            Rmin = schoolsR.ElementAt(0).Value;
            Rmax = schoolsR.ElementAt(schoolsR.Count - 1).Value;
        }


    }
}
