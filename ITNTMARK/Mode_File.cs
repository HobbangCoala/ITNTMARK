using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualBasic;
using static ITNTMARK.MarkController;

namespace ITNTMARK
{
   public class Mode_File
    {
        public static string[] FONT_ = new string[1];// ARRAY FONT
        public static string FONT_PATH = "C:\\ITNTMARK\\ITNTMARK_TEST\\FONT\\";
        public static string PARA_PATH = "C:\\ITNTMARK\\ITNTMARK_TEST\\"; 
        public static string PATTERN_PATH = "C:\\ITNTMARK\\ITNTMARK_RUN\\FILE\\";
        public static string LOGGIN_PATH = "C:\\ITNTMARK\\ITNTMARK_RUN\\Login\\";
        public static string Result_Data_TEXT;
        public static int Step_Length;
        public static short Step_Length_U;
        internal static short MAX_X;
        internal static short MAX_Y;
        public static long G_Sensor;
        public static long G_Input;
        public static bool AREA_Test = false;
        public static bool Download_Data = false;
        public static short gMaxX; 
        public static short gMinX;
        public static short gMaxY; 
        public static short gMinY;
        public static short Accel;
        public static short Decel;
        public static short SAccel;
        public static short SDecel;
        public static string Parking;
        public static short OpMode;
        public static string Port;
        public static int Baud;
        public static long HomeU;
        public static short StartPosU;
        public static short ScanLenU;

        public static void LOAD_FONT(string FONT_NAME)
        {   // Load font
            StreamReader input = new StreamReader(FONT_PATH + FONT_NAME, false);
            FONT_ = new string[2];
            do
            {
                FONT_[FONT_.Length - 1] = input.ReadLine();
                Array.Resize(ref FONT_, (FONT_.Length - 1) + 1 + 1);
            } while (!input.EndOfStream);
            input.Close(); //Done close the file
        }

        public static void GET_START_POS_LINEAR(int STR_COUNT, MPOINT CP, MPOINT START_XY, double PITCH, double ANG, ref MPOINT[] POS)
        {
            POS = new MPOINT[STR_COUNT];
            int i;
            for (i = 0; i <= STR_COUNT - 1; i++)
            {
                POS[i] = Rotate_Point(START_XY.X + (i * PITCH), START_XY.Y, CP.X, CP.Y, ANG);
            }
           
        }

        public static MPOINT Rotate_Point(double tx, double ty, double cx, double cy, double deg)
        {
            MPOINT returnValue = default;
            double nx;
            double ny;
            double q;

            q = deg * System.Math.PI / 180;

            System.Double cosq = System.Math.Cos(q);
            System.Double sinq = System.Math.Sin(q);
            tx -= cx;
            ty -= cy;

            nx = tx * cosq - ty * sinq;
            ny = ty * cosq + tx * sinq;
            nx += cx;
            ny += cy;
            returnValue.X = nx;
            returnValue.Y = ny;

            return returnValue;
        }
        public static string READ_INI(string F_name, string Parameter)
        {
            string[] PP;
            StreamReader FILE_ = new StreamReader(F_name);
            try
            {
                do
                {

                    string P = FILE_.ReadLine();

                    if (P.IndexOf(Parameter) + 1 >= 1)
                    {
                        PP = P.Split('=');
                        if ((PP.Length - 1) >= 1)
                        {
                            FILE_.Close();
                            return PP[1];
                        }
                        else
                        {
                            FILE_.Close();
                            return "";
                        }
                    }
                    else
                    {

                    }
                } while (FILE_.EndOfStream != true);
                FILE_.Close();
                return "";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                FILE_.Close();
                return "";
            }
        }
        public static bool WRITE_INI(string F_name, string Parameter, string valume)
        {
            string[] P = new string[1];
            int i = 0;

            try
            {
                StreamReader FILE_ = new StreamReader(F_name);
                do
                {
                    Array.Resize(ref P, i + 1);
                    P[i] = FILE_.ReadLine();
                    i++;
                } while (FILE_.EndOfStream != true);

                FILE_.Close();

                Write_LINE(F_name, Parameter, P, valume);

            }
            catch (Exception ee)
            {
                MessageBox.Show("The process failed: " + ee.ToString());

                return false;
            }

            return false;
        }

        private static void Write_LINE(string FILE, string parameter, string[] p, string valume)
        {
            StreamWriter FILE_W = new StreamWriter(FILE, false);
            int i = 0;
            for (i = 0; i <= (p.Length - 1); i++)
            {
                if ((p[i]).IndexOf(parameter) + 1 >= 1)
                {
                    FILE_W.WriteLine(parameter + ";" + valume);
                }
                else
                {
                    FILE_W.WriteLine(p[i]);
                }
            }
            FILE_W.Close();
        }

        public static string Dec2Bin(long nVal)
        {
            string returnValue = "";

            int i = 0;
            int nNa = 0;
            int nMok = 0;
            returnValue = "";
            i = 0;
            do
            {
                if (nVal <= 1)
                {
                    if (i == 0)
                    {
                        returnValue = nVal.ToString();
                    }
                    else
                    {
                        returnValue = returnValue.Trim();
                        returnValue = nMok.ToString() + returnValue;
                    }
                    for (i = i + 1; i <= 15; i++)
                    {
                        returnValue = returnValue.Trim();
                        returnValue = 0.ToString() + returnValue;
                    }
                    break;
                }
                nMok = (int)(nVal / 2);
                nNa = (int)(nVal % 2);
                nVal = nMok;
                returnValue = returnValue.Trim();
                returnValue = nNa.ToString() + returnValue;
                i++;
            } while (true);

            return returnValue.Trim();
        }
















































    }
}
