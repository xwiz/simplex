using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            Extractor ex = null;
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (File.Exists(args[i]))
                    {
                        ex = new Extractor(args[i]);
                        ex.Process();
                        ex.OutputToFile("output" + i + ".txt");
                    }
                }
            }
            else
            {
                Console.Write("Enter file path to process: ");
                ex = new Extractor(Console.ReadLine());
                ex.Process();
                Console.WriteLine();
                Console.Write("Enter the output path (Press enter to use default): ");
                string output = Console.ReadLine();
                if (output == "")
                {
                    ex.Output();
                }
                else
                {
                    ex.OutputToFile(output);
                }
            }
        }
    }

    /// <summary>
    /// Extractor class
    /// Keeps data in memory until extraction is complete
    /// then dumps output to file 
    /// Can be initialized with input file
    /// </summary>
    internal class Extractor
    {
        #region Constructors
        /// <summary>
        /// Public constructor with input specification
        /// </summary>
        /// <param name="input">The full or relative path to the input file</param>
        public Extractor(string input)
        {
            _input = input;
        }

        /// <summary>
        /// 
        /// </summary>
        public Extractor()
        {
            //do nothing
        }
        #endregion

        #region Private Variables
        private string _input;
        private StreamReader _inputStream;
        private StreamWriter _outputStream;
        private List<Reserve> _reserves;

        public List<Reserve> Reserves
        {
            get { return _reserves; }
        }

        #endregion

        #region Private Methods
        private bool PowerExtract(string input)
        {
            try
            {
                _reserves = new List<Reserve>();
                //keep track of the currently extracted item
                int itemNo = 0;
                _inputStream = new StreamReader(input, Encoding.UTF8);
                Reserve reserve = null;
                Item item = null;
                //to store current word
                StringBuilder sb = new StringBuilder();
                //iterate through each character to retrieve data and store appropriately
                while (!_inputStream.EndOfStream)
                {
                    char c = (char) _inputStream.Read();
                    if (c == '\r' || c == '\n')
                    {
                        continue;
                    }
                    if (Char.IsWhiteSpace(c))
                    {
                        //find out if we're on reserve item date
                        DateTime cDate;
                        if (DateTime.TryParse(sb.ToString(), out cDate))
                        {
                            reserve = new Reserve();
                            reserve.Date = cDate;
                            GetReserveDetail(reserve);
                            Reserves.Add(reserve);
                            sb.Clear();
                            itemNo = 0;
                            item = new Item();
                        }
                        else
                        {
                            //check if the next char is whitespace too so we can store item element
                            string s = sb.ToString().Trim();
                            if (s != "" && Char.IsWhiteSpace((char) _inputStream.Peek()))
                            {
                                //try begin item extract
                                switch (itemNo)
                                {
                                    case 0:
                                        item.Title = sb.ToString();
                                        break;
                                    case 1:
                                        if (sb.ToString().HasComma())
                                        {
                                            item.Author = s;
                                        }
                                        else
                                        {
                                            //ignore item as missing and fall to next case
                                            itemNo++;
                                            goto case 2;
                                        }
                                        break;
                                    case 2:
                                        if (s.HasDot() && s.HasNumber())
                                        {
                                            item.Shelfmark = s;
                                        }
                                        else
                                        {
                                            //cannot be empty
                                            //so set back seek position to neutralize seek
                                            itemNo--;
                                        }
                                        break;
                                    case 3:
                                        item.LocationCode = s;
                                        break;
                                    case 4:
                                        if (s.HasNumber())
                                        {
                                            item.Barcode = s;
                                        }
                                        else
                                        {
                                            //cannot be empty
                                            //so set back seek position to neutralize seek
                                            itemNo--;
                                        }
                                        break;
                                    case 5:
                                        if (s.IsNumber())
                                        {
                                            item.Checkouts = Int32.Parse(s);
                                        }
                                        else
                                        {
                                            //ignore item and fall to next case
                                            //no need to increment item no
                                            goto case 6;
                                        }
                                        break;
                                    case 6:
                                        item.LocationDescription = s;
                                        //add item to list and reset
                                        reserve.Items.Add(item);
                                        item = new Item();
                                        itemNo = -1;
                                        break;
                                }
                                //get ready for next capture
                                itemNo++;
                                sb.Clear();
                                //read to next non-whitespace
                                SkipSpace(_inputStream);
                            }
                            else
                            {
                                //if next char not whitespace append this
                                sb.Append(c);
                            }
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the details for specified reserve and updates item
        /// </summary>
        /// <param name="reserve">The reserve to modify</param>
        private void GetReserveDetail(Reserve reserve)
        {
            //begin detail capture
            _inputStream.ReadLine(); //we don't need current line
            string line = _inputStream.ReadLine();
            if (line != null && line.Trim() != "")
            {
                //get Lecturer Name from first line
                reserve.Author =
                    line.Substring(line.IndexOf("LECTURER", StringComparison.Ordinal) + 8).Trim();
            }
            //get course details from next three lines
            Course course = new Course();
            line = _inputStream.ReadLine();
            if (line != null && line.Trim() != "")
            {
                //get course Name from first line
                course.Code =
                    line.Substring(line.IndexOf("COURSE", StringComparison.Ordinal) + 6).Trim();
                //get course title from next line
                line = _inputStream.ReadLine();
                course.Title =
                    line.Substring(line.IndexOf("COURSE", StringComparison.Ordinal) + 6).Trim();
                //get course detail from next line
                line = _inputStream.ReadLine();
                course.Detail =
                    line.Substring(line.IndexOf("COURSE", StringComparison.Ordinal) + 6).Trim();
            }
            //add course description to reserve
            reserve.CourseDescription = course;
        }

        /// <summary>
        /// Generates the output file to specified file path
        /// </summary>
        /// <param name="file">The file where output will be dumped</param>
        /// <returns>True if operation completes successfully, otherwise false</returns>
        private bool GenerateResult(string file)
        {
            if(!File.Exists(file))
            {
                File.WriteAllText(file, "");
            }
            int runningNo = 1;
            try
            {
                using (_outputStream = new StreamWriter(file))
                {
                    foreach (var cReserve in Reserves)
                    {
                        foreach (var item in cReserve.Items)
                        {
                            _outputStream.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", runningNo,
                                                                  item.Title,
                                                                  item.Author, item.Shelfmark, item.Barcode,
                                                                  item.LocationCode,
                                                                  item.LocationDescription, item.Checkouts));
                            runningNo++;
                        }
                    }
                    System.Diagnostics.Process.Start(file);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return false;
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Processes input file specified in constructor if it exists
        /// </summary>
        public bool Process()
        {
            if (File.Exists(_input))
            {
                return PowerExtract(_input);
            }
            return false;
        }

        private static void SkipSpace(StreamReader sr)
        {
            while (sr.Peek() == ' ')
            {
                sr.Read();
            }
        }

        /// <summary>
        /// Processes explicitly specified input file if given
        /// Sample usage :: ProcessFile("C:\\inputfile.txt");
        /// => Result can be written by calling Output();
        /// </summary>
        /// <param name="input">The file to process</param>
        /// <returns>True if the file is processed successfully. Otherwise false.</returns>
        public bool ProcessFile(string input)
        {
            if (File.Exists(input))
            {
                return PowerExtract(input);
            }
            return false;
        }

        /// <summary>
        /// Writes the processed data output to 'output.csv'
        /// in current directory and opens for viewing
        /// </summary>
        /// <returns>True if the output file is written successfully otherwise false.</returns>
        public bool Output()
        {
            return GenerateResult("output.txt");
        }

        /// <summary>
        /// Writes the processed data output to specified
        /// input file and opens the file for viewing
        /// </summary>
        /// <param name="output">The file to write the output to.
        ///  File will be created if it doesn't exist.</param>
        /// <returns>True if the output file is written successfully otherwise false.</returns>
        public bool OutputToFile(string output)
        {
            return GenerateResult(output);
        }
        #endregion

        #region Logging
        private void WriteLog(Exception e)
        {
            File.AppendAllText("log.txt", string.Format("Exception at {0} : \nMessage: {1}\nStack Trace: {2}\n", DateTime.Now.ToString(CultureInfo.InvariantCulture), e.Message, e.StackTrace));
        }

        #endregion
    }

    #region Models

    class Item
    {
        private string _title;
        private string _author;
        private string _shelfmark;
        private string _barcode;
        private string _locationCode;
        private string _locationDescription;
        private int _checkouts;

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        public string Shelfmark
        {
            get { return _shelfmark; }
            set { _shelfmark = value; }
        }

        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; }
        }

        public string LocationCode
        {
            get { return _locationCode; }
            set { _locationCode = value; }
        }

        public string LocationDescription
        {
            get { return _locationDescription; }
            set { _locationDescription = value; }
        }

        public int Checkouts
        {
            get { return _checkouts; }
            set { _checkouts = value; }
        }
    }

    class Reserve
    {
        private string _author;
        private DateTime _date;
        private Course _courseDescription;
        private List<Item> _items;

        public Reserve()
        {
            _items = new List<Item>();
        }

        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }

        public Course CourseDescription
        {
            get { return _courseDescription; }
            set { _courseDescription = value; }
        }

        public List<Item> Items
        {
            get { return _items; }
            set { _items = value; }
        }
    }

    class Course
    {
        private string _code;
        private string _title;
        private string _detail;

        public string Code
        {
            get { return _code; }
            set { _code = value; }
        }

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string Detail
        {
            get { return _detail; }
            set { _detail = value; }
        }
    }

    #endregion

    #region Extensions
    public static class Extensions
    {
        /// <summary>
        /// Checks if a string has a comma
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns>True if a comma is found in the string, otherwise false</returns>
        public static bool HasComma(this string s)
        {
            return s.Contains(",");
        }

        /// <summary>
        /// Checks if a string has a dot/fullstop
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns>True if a dot/fullstop is found in the string, otherwise false</returns>
        public static bool HasDot(this string s)
        {
            return s.Contains(".");
        }

        /// <summary>
        /// Checks if the string contains at least one number
        /// </summary>
        /// <param name="c">The string to check</param>
        /// <returns>True if a number is found, otherwise false</returns>
        public static bool HasNumber(this string c)
        {
            for (int i = 0; i < c.Length; i++)
            {
                if (Char.IsNumber(c[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a string is an number(integer)
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns>True if the string is a number, otherwise false</returns>
        public static bool IsNumber(this string s)
        {
            int i;
            if (Int32.TryParse(s, out i))
            {
                return true;
            }
            return false;
        }
    }
    #endregion
}
