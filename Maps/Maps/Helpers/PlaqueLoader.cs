using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using System.Collections.Generic;

using Maps.Helpers;

namespace Maps.Helpers
{
    public class PlaqueLoader
    {
        const int NUM_FIELDS = 16;
        
        public PlaqueLoader()
        {
        }

        public List<PlaqueInfo> Load()
        {
            Random random = new Random();

            List<PlaqueInfo> list = new List<PlaqueInfo>();
            List<string> parsedData = CommaSeperatedValueParser();

            // strip the gumf from the beginning
            int count = 0;
            for (int i = 0; i < parsedData.Count; i++)
            {
                string s = parsedData[i];

                if (s == "1") // finding the first one
                {
                    count = i;
                    break;
                }
            }             
   
            //delete everything up until the first entry
            parsedData.RemoveRange(0, count);

            int counter = 0;
            while (counter < parsedData.Count)
            {
                List<string> s = new List<string>();

                for (int j = counter; j < counter + NUM_FIELDS; j++)
                {
                    s.Add(parsedData[j]);
                };
                PlaqueInfo p = new PlaqueInfo(s);

                list.Add(p);

                counter += NUM_FIELDS;
            }
            return list;
        }

        public List<string> CommaSeperatedValueParser()
        {
            List<string> parsedData = new List<string>();
            //uint total_comments = 0;
            uint total_blankLines = 0;
            bool tokenInQuotes = false;
            bool tokenContinued = true;
            uint total_tokens = 0;
            string temp_println = "";

            var rs = Application.GetResourceStream(new Uri("Text/plaques.csv", UriKind.Relative));
           // using (StreamReader readFile = new StreamReader(rs.Stream))

            StreamReader readFile = new StreamReader(rs.Stream);
            string readLine = null;
            string printLine = null;

            while ((readLine = readFile.ReadLine()) != null)
            {
/*
                // Ignore Any Lines Starting With ';'
                if (readLine.StartsWith(";"))
                {
                    printLine = null;
                    total_comments = total_comments + 1;
                }
                */
                // If line is not comment line check if its blank
                if (readLine.Trim() == null || readLine.Length == 0)
                {
                    printLine = null;
                    total_blankLines = total_blankLines + 1;
                }

                // Check For Any Other Characters (Default Action)
                else if ((readLine.Trim() != null))
                {
                    // Cycle Each Character
                    foreach (char character in readLine)
                    {
                        if (tokenContinued == true)
                        {
                            temp_println = printLine;
                            printLine = temp_println;
                        }
                        // Split Tokens At The Commas
                        if (character == ',')
                        {
                            if (tokenInQuotes == false)
                            {
                                total_tokens = total_tokens + 1;
                                parsedData.Add(printLine);
                                printLine = null;
                                tokenContinued = false;
                                temp_println = null;
                            }
                            else if (tokenInQuotes == true)
                            {
                                total_tokens = total_tokens - 0;
                                printLine += character;
                                tokenContinued = true;
                            }
                            continue;
                        }

                        if (character == '\"')
                        {
                            // Check For Start Of Quotation
                            if (character == '\"' && tokenInQuotes == false)
                            {
                                tokenInQuotes = true;
                                //printLine += character;
                                continue;
                            }

                            // Check for end of Quotations
                            else if (tokenInQuotes == true && character == '\"')
                            {
                                tokenInQuotes = false;
                                //printLine += character;
                                continue;
                            }
                        }
                        /*
                        // Check For Internal Comments
                        if (character == ';')
                        {
                            total_comments = total_comments + 1;
                            temp_println = printLine;
                            printLine = null;
                            printLine = temp_println;
                            break;
                        }
                        */
                        // Handle all other characters
                        if (/*character != ';' && */character != '\"' && character != ',')
                        {
                            printLine += character;
                            continue;
                        }
                    }
                    // Print tokens at the end of the line
                    if (tokenContinued == false)
                    {
                        total_tokens = total_tokens + 1;
                        printLine = null;
                        temp_println = null;
                    }
                }
            }
            return parsedData;
        }

    }
}
