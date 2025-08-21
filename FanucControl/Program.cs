using l99.driver.fanuc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanucControl
{
    internal class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;

        static bool _exit = false;
        static void Main(string[] args)
        {
            Thread t = new Thread(new ThreadStart(ExitCheck));
            t.Start();

            _ret = Focas1.cnc_allclibhndl3("192.168.50.20", 8192, 0, out _handle);

            if (_ret != Focas1.EW_OK)
            {
                Console.WriteLine($"Unable to connect to 192.168.50.20 on port 8192\n\nReturn Code: {_ret}\n\nExiting...");
                Console.Read();
            }
            else
            {
                Console.WriteLine($"Our Focas handle is {_handle}");

                string mode = GetMode();
                Console.WriteLine($"\nMode is: {mode}");

                string status = GetStatus();
                Console.WriteLine($"\nStatus is: {status}");
                /*
                while (!_exit)
                {
                    Console.WriteLine($"\rOP Signal: {GetOpSignal().ToString()}\r");
                    Thread.Sleep(500);
                }
                */

                /*
                while (true) {
                    Console.WriteLine($"Program Name: {GetProgramName()}\tSub-Program Name: {GetSubProgramName()}");

                    Thread.Sleep(500);
                }
                */

                var progList = GetProgramDirectory(_handle);

                int index = 0;

                foreach (var prog in progList) {
                    Console.WriteLine($"{index}) Name: {prog}\tComment: {prog}");
                    index++;
                }

            }
        }

        public static string GetProgramName()
        {
            if (_handle == 0)
            {
                return "UNAVAILABLE";
            }

            Focas1.ODBPRO rdProg = new Focas1.ODBPRO();

            
            _ret = Focas1.cnc_rdprgnum(_handle, rdProg);

            if (_ret != Focas1.EW_OK)
            {
                return _ret.ToString();
            }

            return rdProg.data.ToString();
            
        }

        public static string GetSubProgramName()
        {
            if (_handle == 0)
            {
                return "UNAVAILABLE";
            }

            Focas1.ODBPRO subProg = new Focas1.ODBPRO();

            _ret = Focas1.cnc_rdprgnum(_handle, subProg);

            if (_ret != Focas1.EW_OK) {
                return _ret.ToString();
            }

            if (subProg.data != subProg.mdata) {
                return subProg.data.ToString();
            }

            return "No Sub Program";
        }

        public static List<Focas1.PRGDIR> GetProgramDirectory(ushort handle)
        {
            List<Focas1.PRGDIR> programs = new List<Focas1.PRGDIR>();

            short type = 0;   // 0 = programas
            short num = 10;   // cantidad a leer por llamada
            short from = 0;   // posición inicial
            ushort length = (ushort)Marshal.SizeOf(typeof(Focas1.PRGDIR));

            for (; ; )
            {
                Focas1.PRGDIR dir = new Focas1.PRGDIR();

                short ret = Focas1.cnc_rdprogdir(handle, type, num, from, length, dir);

                if (ret != Focas1.EW_OK)
                {
                    Console.WriteLine($"Error en cnc_rdprogdir: {ret}");
                    break;
                }

                // Guardar resultado
                programs.Add(dir);

                // Avanzar puntero
                from += num;

                // Si ya no hay más, salir
                // if (dir.progno == 0)
                    // break;
            }

            return programs;
        }


        private static void ExitCheck()
        {
            while (Console.ReadLine() != "exit")
            {
                continue;
            }

            _exit = true;
        }

        public static bool GetOpSignal()
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return false;
            }

            short addr_kind = 1; // F
            short data_type = 0; // Byte
            ushort start = 0;
            ushort end = 0;
            ushort data_length = 9; // 8 + N
            Focas1.IODBPMC0 pmc = new Focas1.IODBPMC0();

            _ret = Focas1.pmc_rdpmcrng(_handle, addr_kind, data_type, start, end, data_length, pmc);

            if(_ret != Focas1.EW_OK)
            {
                Console.WriteLine($"Error: Unable to obtain the OP signal");
                return false;
            }

            return pmc.cdata[0].GetBit(7);
        }

        public static string GetMode()
        {
            if(_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Mode = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Mode);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain mode.\nReturn code: {_ret}");
                return "";
            }

            string modestr = ModeNumberToString(Mode.aut);

            return modestr;
        }

        public static string ModeNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "MDI"; }
                case 1: { return "MEM"; }
                case 3: { return "EDIT"; }
                case 4: { return "HND"; }
                case 5: { return "JOG"; }
                case 6: { return "Teach in JOG"; }
                case 7: { return "Teach in HND"; }
                case 8: { return "INC"; }
                case 9: { return "REF"; }
                case 10: { return "RMT"; }
                default: { return "UNAVAILABLE"; }
            }
        }

        public static string GetStatus()
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Status = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Status);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain status.\nReturn Code: {_ret}");
                return "";
            }

            string statusstr = StatusNumberToString(Status.run);

            return $"Mode is: {statusstr}";
        }

        public static string StatusNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "****"; }
                case 1: { return "STOP"; }
                case 2: { return "HOLD"; }
                case 3: { return "STRT"; }
                case 4: { return "MSTR"; }
                default: { return "UNAVAILABLE"; }
            }
        }
    }
}
