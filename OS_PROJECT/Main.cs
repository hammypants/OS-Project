using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Phase01
{
    class Program
    {
        static void Main(string[] args)
        {

            string line;

            //Read the file
            System.IO.StreamReader file =
				new System.IO.StreamReader(@"/Users/admin/Projects/Phase01/Datafile2.txt");
               //new System.IO.StreamReader(@"H:\Phase01Code\DataFile1.txt");
           
            while ((line = file.ReadLine()) != null)
            {

                string[] words = line.Split(' ');


                string job = "JOB"; // switch ????
                string end = "END";
                string data = "Data";
				
                string fwdSlashes = words[0];
                string jobDataEnd = words[1];
				
	            if (jobDataEnd.Equals(end))

                Console.WriteLine(jobDataEnd);

                // if job do ...
                uint jobNum;
                uint numWords;
                uint priNum;

                if (string.Compare(jobDataEnd, job) == 0)   //jobDataEnd.Equals(job)
                {
					jobNum = Convert.ToUInt32(words[2], 16); //Convert.ToUInt32(words[2]);
                    numWords = Convert.ToUInt32(words[3], 16); //Convert.ToUInt32(words[3]);
                    priNum = Convert.ToUInt32(words[4], 16); //Convert.ToUInt32(words[4]);
					
					Console.WriteLine("Job#" + jobNum);
                    //read then send numWords to screen/disk
                    for (int i = 1; i <= numWords; i++)
                    {
                            Console.WriteLine(file.ReadLine());  //Pass it here
                    }
                }

                // if data do ...
                uint otptBuffSize;
                uint tempBuffSize;

                if (string.Compare(jobDataEnd, data) == 0)
                {
					numWords = 44; //one;
                    otptBuffSize = Convert.ToUInt32(words[3], 16);
                    tempBuffSize = Convert.ToUInt32(words[4], 16);
					
					Console.WriteLine("Data#");

                    for (int i = 1; i <= numWords; i++)
                    {
                        Console.WriteLine(file.ReadLine());    //pass it here

                    }
					file.ReadLine();
                }
            }
            file.Close();
            Console.ReadLine();
        }
    }
}

