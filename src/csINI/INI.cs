using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace csINI
{
    public class INI
    {
        public static string LibName = "csINI";
        public static string Version = "1.00";
        public static string Author = "Kris Craig";
        public static string Email = "kriscraig@php.net";
        public static string Repo = "https://github.com/sirkris/csINI";

        /*
          * Note on logging:
          * 
          * The csLog library is OPTIONAL and not included in this repo.
          * It is recommended as it enables the INI library to generate useful logs.
          * 
          * If you do NOT want to use the csLog library, pass false for the logging 
          * argument when instantiating the INI class.  If you want to use an existing 
          * instance of the csLog class, simply pass the Assembly, Type, and instance 
          * variables when instantiating the INI class.
          * 
          * If the csLog library is not present or fails to load during instantiation, 
          * a caught Exception will occur.  Because csLog is not critical to the INI 
          * library's successful operation, the default behavior is for the Exception 
          * to be ignored.  You can change this by passing false for the failSilently 
          * argument when instantiating the INI class.  That will cause the Exception 
          * to be thrown normally.
          * 
          * --Kris
          */
        protected const string Logname = "INI";
        protected string logLibDir = Environment.CurrentDirectory;

        protected Assembly csLog = null;
        protected Type csLogType = null;
        protected object csLogInstance = null;

        protected bool csLogEnabled = false;

        public INI(bool logging = true, bool failSilently = true, Assembly csLogPass = null, Type csLogTypePass = null, object csLogInstancePass = null)
        {
            if (logging == true)
            {
                csLogEnabled = InitLog(failSilently, csLogPass, csLogTypePass, csLogInstancePass);
            }
        }

        public INI() { }

        internal bool InitLog(bool failSilently, Assembly csLogPass, Type csLogTypePass, object csLogInstancePass)
        {
            if (csLogPass == null)
            {
                try
                {
                    csLog = Assembly.LoadFile(logLibDir + @"\csLog.dll");
                }
                catch (Exception e)
                {
                    if (failSilently == false)
                    {
                        throw e;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                csLog = csLogPass;
            }

            if (csLog != null
                && csLogTypePass == null)
            {
                try
                {
                    csLogType = csLog.GetType("csLog.Log");
                }
                catch (Exception e)
                {
                    if (failSilently == false)
                    {
                        throw e;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                csLogType = csLogTypePass;
            }

            if (csLog != null && csLogType != null
                && csLogInstancePass == null)
            {
                try
                {
                    csLogInstance = Activator.CreateInstance(csLogType);
                }
                catch (Exception e)
                {
                    if (failSilently == false)
                    {
                        throw e;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                csLogInstance = csLogInstancePass;
            }

            /* Initialize the INI log.  --Kris */
            try
            {
                csLogType.InvokeMember("Init", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, csLogInstance,
                    new object[] { Logname, "string" });
            }
            catch (Exception e)
            {
                if (failSilently == false)
                {
                    throw e;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /* Interact with the log handler.  --Kris */
        internal void Log(string text = null, string action = "append", bool newline = true)
        {
            if (csLogEnabled == true)
            {
                switch (action.ToLower())
                {
                    default:
                    case "append":
                        csLogType.InvokeMember("Append", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding,
                            null, csLogInstance, new object[] { Logname, text, newline });
                        break;
                    case "increment":
                        csLogType.InvokeMember("Increment", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding,
                            null, csLogInstance, new object[] { Logname, Int32.Parse(text) });
                        break;
                    case "decrement":
                        csLogType.InvokeMember("Decrement", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding,
                            null, csLogInstance, new object[] { Logname, Int32.Parse(text) });
                        break;
                    case "save":
                        csLogType.InvokeMember("Save", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, csLogInstance,
                            new object[] { Logname });
                        break;
                }
            }
        }

        /* Load and parse an INI file.  Wrapper for complete workflow.  --Kris */
        public Dictionary<string, string> Load(string filename)
        {
            return GetDirectives(Parse(ReadFile(filename)));
        }

        /* Load and parse an INI file with headers.  Wrapper for complete workflow.  --Kris */
        public Dictionary<string, Dictionary<string, string>> LoadWithHeaders(string filename)
        {
            return GetDirectivesWithHeaders(Parse(ReadFile(filename), true));
        }

        /* Read the INI file into a string.  --Kris */
        public string ReadFile(string filename)
        {
            try
            {
                FileStream fileStream = new FileStream(@filename, FileMode.Open, FileAccess.Read);

                byte[] buf;
                try
                {
                    int len = (int)fileStream.Length;
                    buf = new byte[len];
                    int count;
                    int sum = 0;

                    while ((count = fileStream.Read(buf, sum, len - sum)) > 0)
                    {
                        sum += count;
                    }
                }
                finally
                {
                    fileStream.Close();
                }

                return System.Text.ASCIIEncoding.ASCII.GetString(buf);
            }
            catch
            {
                Log(String.Concat("ERROR!  Unable to open file '", filename, "' for read!"));
                return "ERROR";
            }
        }

        /* Parse the INI string into key => value pairs.  --Kris */
        // Note - Ignores section headers by default!
        internal string[][] Parse(string filedata, bool useheaders = false)
        {
            string[][] directives = new string[99999][];
            string[] lines = new string[99999];
            string[] dump = new string[999];
            string lineclean;

            lines = Regex.Split(filedata, "\r\n");

            int i = 0;
            foreach (string line in lines)
            {
                /* First, remove anything that comes after # or ;  --Kris */
                lineclean = line;
                if (lineclean.IndexOf(";") != -1)
                {
                    dump = Regex.Split(lineclean, @"(?<![\\]);");
                    lineclean = dump[0];
                }
                if (lineclean.IndexOf("#") != -1)
                {
                    dump = Regex.Split(lineclean, @"(?<![\\])#");
                    lineclean = dump[0];
                }

                /* If it doesn't have assignment, skip to the next line.  --Kris */
                if (lineclean.IndexOf("=") != -1)
                {
                    directives[i] = new string[2];
                    directives[i] = Regex.Split(lineclean, @"[ ]*(?<!(?<!\\)*\\)\=[ ]*");

                    i++;
                }
                else if (useheaders == true
                    && lineclean.IndexOf(@"[") == 0
                    && lineclean.LastIndexOf(@"]") > lineclean.IndexOf(@"["))
                {
                    directives[i] = new string[2];
                    directives[i][0] = @";===INIHEADER===;";
                    directives[i][1] = lineclean.Substring(lineclean.IndexOf(@"[") + 1, lineclean.LastIndexOf(@"]") - lineclean.IndexOf("[") - 1);

                    i++;
                }
            }

            return directives;
        }

        /* Parse the key => value pairs into a sanitized dictionary.  --Kris */
        internal Dictionary<string, string> GetDirectives(string[][] lines)
        {
            Dictionary<string, string> directives = new Dictionary<string, string>();

            foreach (string[] pair in lines)
            {
                if (pair == null)
                {
                    continue;
                }

                directives.Add(ParseVars(pair[0]).Trim(), ParseVars(pair[1]).Trim());
            }

            return directives;
        }

        /* Parse the key => value pairs into a sanitized dictionary that includes INI section headers.  --Kris */
        internal Dictionary<string, Dictionary<string, string>> GetDirectivesWithHeaders(string[][] lines)
        {
            Dictionary<string, Dictionary<string, string>> directives = new Dictionary<string, Dictionary<string, string>>();

            string[][] sectionlines = new string[99999][];
            string header = "";
            int i = 0;
            int ii = 0;
            foreach (string[] pair in lines)
            {
                if (pair == null)
                {
                    continue;
                }

                if (String.Compare(lines[i][0], @";===INIHEADER===;") == 0)
                {
                    if (header != "")
                    {
                        directives.Add(header, GetDirectives(sectionlines));
                    }

                    header = pair[1];
                    sectionlines = new string[99999][];
                    ii = 0;
                }
                else
                {
                    sectionlines[ii] = new string[2];
                    sectionlines[ii][0] = pair[0];
                    sectionlines[ii][1] = pair[1];

                    ii++;
                }

                i++;
            }

            if (directives.ContainsKey(header) == false)
            {
                directives.Add(header, GetDirectives(sectionlines));
            }

            return directives;
        }

        public bool Save(string filename, Dictionary<string, string> directives)
        {
            string filedata = ReadFile(filename);
            string[] lines = new string[99999];
            string newdata = null;
            Dictionary<string, int> keysused = new Dictionary<string, int>();
            string[] pair;
            string lineclean;
            string[] dump = new string[999];

            lines = Regex.Split(filedata, "\r\n");

            foreach (string line in lines)
            {
                /* First, remove anything that comes after # or ;  --Kris */
                lineclean = line;
                if (lineclean.IndexOf(";") != -1)
                {
                    dump = Regex.Split(lineclean, ";");
                    lineclean = dump[0];
                }
                if (lineclean.IndexOf("#") != -1)
                {
                    dump = Regex.Split(lineclean, "#");
                    lineclean = dump[0];
                }

                /* Unrecognized directives/text will be left alone.  --Kris */
                pair = new string[2];

                pair = Regex.Split(lineclean, @"[ ]*[^\\]=[ ]*");

                if (lineclean.IndexOf("=") != -1)
                {
                    if (directives.ContainsKey(pair[0].Trim()) == true)
                    {
                        if (keysused.ContainsKey(pair[0].Trim()) == true)
                        {
                            Log("Skipping duplicate setting '" + line + "'....");
                            keysused[pair[0].Trim()]++;
                            continue;
                        }
                        else
                        {
                            newdata += pair[0].Trim() + " = " + directives[pair[0].Trim()];
                            keysused.Add(pair[0].Trim(), 1);
                        }
                    }
                    else
                    {
                        newdata += line;
                    }
                }
                else
                {
                    newdata += line;
                }

                newdata += "\r\n";
            }

            newdata = newdata.Trim() + "\r\n";

            System.IO.File.WriteAllText(@filename, @newdata);

            return true;
        }

        /* Overload for saving with headers.  --Kris */
        public bool Save(string filename, Dictionary<string, Dictionary<string, string>> directives)
        {
            string filedata = ReadFile(filename);
            string[] lines = new string[99999];
            string newdata = null;
            Dictionary<string, int> keysused = new Dictionary<string, int>();
            List<string> sectionsused = new List<string>();
            string[] pair;
            string lineclean;
            string[] dump = new string[999];
            string section = "";

            lines = Regex.Split(filedata, "\r\n");

            foreach (string line in lines)
            {
                /* First, remove anything that comes after # or ;  --Kris */
                lineclean = line;
                if (lineclean.IndexOf(";") != -1)
                {
                    dump = Regex.Split(lineclean, ";");
                    lineclean = dump[0];
                }
                if (lineclean.IndexOf("#") != -1)
                {
                    dump = Regex.Split(lineclean, "#");
                    lineclean = dump[0];
                }

                /* Unrecognized directives/text will be left alone.  --Kris */
                pair = new string[2];

                pair = Regex.Split(lineclean, @"[ ]*[^\\]=[ ]*");

                if (lineclean.IndexOf("=") != -1)
                {
                    if (directives.ContainsKey(section) && directives[section].ContainsKey(pair[0].Trim()) == true)
                    {
                        if (keysused.ContainsKey(pair[0].Trim()) == true)
                        {
                            Log("Skipping duplicate setting '" + line + "'....");
                            keysused[pair[0].Trim()]++;
                            continue;
                        }
                        else
                        {
                            newdata += pair[0].Trim() + " = " + directives[pair[0].Trim()];
                            keysused.Add(pair[0].Trim(), 1);
                        }
                    }
                    else
                    {
                        newdata += line;
                    }
                }
                else if (lineclean.IndexOf(@"[") == 1
                    && lineclean.LastIndexOf(@"]") > lineclean.IndexOf(@"["))
                {
                    /* Add any new directives for this section (TODO - do this on sectionless save function as well).  --Kris */
                    if (directives.ContainsKey(section))
                    {
                        foreach (KeyValuePair<string, string> directive in directives[section])
                        {
                            if (keysused.ContainsKey(directive.Key) == false)
                            {
                                newdata += "\r\n" + directive.Key + " = " + directive.Value;
                            }
                        }

                        sectionsused.Add(section);
                    }

                    section = lineclean.Substring(lineclean.IndexOf(@"[") + 1, lineclean.LastIndexOf(@"]") - lineclean.IndexOf("[") - 1);
                    newdata += "\r\n" + @"[" + section + @"]";

                    keysused = new Dictionary<string, int>();
                }
                else
                {
                    newdata += line;
                }

                newdata += "\r\n";
            }

            /* Add any new directives for this section (TODO - do this on sectionless save function as well).  --Kris */
            if (directives.ContainsKey(section))
            {
                foreach (KeyValuePair<string, string> directive in directives[section])
                {
                    if (keysused.ContainsKey(directive.Key) == false)
                    {
                        newdata += "\r\n" + directive.Key + " = " + directive.Value;
                    }
                }

                sectionsused.Add(section);
            }

            /* Add any new sections and their respective directives.  --Kris */
            foreach (KeyValuePair<string, Dictionary<string, string>> header in directives)
            {
                if (sectionsused.Contains(header.Key) == false)
                {
                    newdata += "\r\n\r\n" + @"[" + header.Key + @"]";

                    foreach (KeyValuePair<string, string> directive in header.Value)
                    {
                        newdata += "\r\n" + directive.Key + " = " + directive.Value;
                    }
                }
            }

            newdata = newdata.Trim() + "\r\n";

            System.IO.File.WriteAllText(@filename, @newdata);

            return true;
        }

        /* Generate an INI file based on the given parameters.  --Kris */
        public bool Create(string INIPath, string title, string subtitle, Dictionary<string, Dictionary<string, string>> directives)
        {
            string outbuf = null;
            outbuf = @"; " + title + Environment.NewLine;
            outbuf += @"; " + subtitle + Environment.NewLine + Environment.NewLine;

            foreach (KeyValuePair<string, Dictionary<string, string>> section in directives)
            {
                if (section.Key != "")
                {
                    outbuf += @"[" + section.Key + "]" + Environment.NewLine;
                }

                foreach (KeyValuePair<string, string> directive in section.Value)
                {
                    outbuf += directive.Key + @" = " + directive.Value + Environment.NewLine;
                }
            }

            File.WriteAllText(INIPath, outbuf);

            return true;
        }

        /* Clear all data from an existing INI file while preserving the top two comment lines ("title" and "subtitle", respectively).  --Kris */
        public bool Clear(string INIPath)
        {
            string filedata = ReadFile(INIPath);
            string[] lines = new string[99999];

            lines = Regex.Split(filedata, "\r\n");

            /* We only care about the top two.  If it's not a comment, assume empty.  --Kris */
            string title = "";
            string subtitle = "";

            if (lines != null && lines[0] != null && lines[1] != null)
            {
                if (lines[0].Trim().IndexOf(';') == 0)
                {
                    title = lines[0].Substring(1).Trim();
                }

                if (lines[1].Trim().IndexOf(';') == 0)
                {
                    subtitle = lines[1].Substring(1).Trim();
                }
            }

            return Create(INIPath, title, subtitle, new Dictionary<string, Dictionary<string, string>>());
        }

        public bool ContainsKeyRecursive(Dictionary<string, Dictionary<string, string>> directives, string key)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section in directives)
            {
                if (section.Value.ContainsKey(key) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /* So we can get accurate line numbers, including comments/etc.  --Kris */
        // TODO - Merge these rules with Parse?
        internal Dictionary<int, int> LineModifiers(string filedata)
        {
            Dictionary<int, int> linemod = new Dictionary<int, int>();

            string[] lines = new string[99999];
            string[] dump = new string[999];
            string lineclean;

            lines = Regex.Split(filedata, "\r\n");

            int i = 0;
            int linereal = 0;
            foreach (string line in lines)
            {
                i++;

                /* First, remove anything that comes after # or ;  --Kris */
                lineclean = line;
                if (lineclean.IndexOf(";") != -1)
                {
                    dump = Regex.Split(lineclean, ";");
                    lineclean = dump[0];
                }
                if (lineclean.IndexOf("#") != -1)
                {
                    dump = Regex.Split(lineclean, "#");
                    lineclean = dump[0];
                }

                if (lineclean.IndexOf("=") != -1)
                {
                    linereal++;
                    linemod[linereal] = i;
                }
            }

            return linemod;
        }

        /* Convert variables into literal strings.  --Kris */
        // TODO - Support for user-defined variables.
        internal string ParseVars(string text)
        {
            Match m;
            Match mm;

            Dictionary<string, string> staticvars = new Dictionary<string, string>();
            staticvars.Add(@"_TAB_", "\\t");
            staticvars.Add(@"_SPACE_", " ");
            staticvars.Add(@"_ENTER_", "\\r\\n");
            staticvars.Add(@"_BASEDIR_", Environment.CurrentDirectory);

            foreach (KeyValuePair<string, string> entry in staticvars)
            {
                text = Regex.Replace(@text, @entry.Key, @entry.Value);
            }

            /* If multiplication is detected, duplicate entire string.  --Kris */
            if (Regex.IsMatch(text, @"\(\*[\d]*\)"))
            {
                m = Regex.Match(text, @"\(\*[\d]*\)");
                mm = Regex.Match(m.ToString(), @"[0-9]+");

                text = Regex.Replace(text, @"\(\*[\d]*\)", "");

                string newtext = null;
                for (int i = 1; i <= Convert.ToInt32(Convert.ToString(mm)); i++)
                {
                    newtext += text;
                }

                text = newtext;
            }

            /* Handle escaped = signs.  --Kris */
            text = Regex.Replace(@text, @"\\=", @"=");

            return text;
        }

        internal void LineFail(string command, string value, Dictionary<int, int> linemod, int line, string errmsg = null, string filename = null)
        {
            Log("**** FATAL ERROR DETECTED! ****");
            if (errmsg != null)
            {
                Log(errmsg);
            }
            if (filename != null)
            {
                filename = " of " + filename;
            }
            Log("Failure at Line " + linemod[line].ToString() + filename + " : " + command + " = " + value);
            Log("INI Parse Aborted!");
        }

        internal void LineWarning(string command, string value, Dictionary<int, int> linemod, int line, string errmsg = null, string filename = null)
        {
            Log("Warning:");
            if (errmsg != null)
            {
                Log(errmsg);
            }
            if (filename != null)
            {
                filename = " of " + filename;
            }
            Log("Occurred at Line " + linemod[line].ToString() + filename + " : " + command + " = " + value);
        }
    }
}
