using System;
using System.Net.Sockets;
using System.IO;
using System.Management;

namespace PrinterTool
{

	public class App
	{

		const int PORT = 9100;
		const string UEL = "\x1B%-12345X";
		
		static void Main(string[] Args)
		{

			Console.WriteLine("\nPrinterTool - Tristan Phillips\n");

			string IP ="";
			string Filename = "";
			string Value = "";
			bool MemReq = false;
			bool Echo = false;
			bool Custom = false;
			bool DoRead = true;
			bool ReadStatus = false;
			bool Info = false;
			bool WMI = false;
			bool Query = false;
			bool FixDisplay = false;

			try 
			{
				for(int x = 0; x < Args.Length; x++)
				{
					string Arg = Args[x];
					if (Arg == "-h") { IP = Args[x+1]; }
					if (Arg == "-f") { Filename = Args[x+1]; }
					if (Arg == "-v") { Value = Args[x+1]; }
					if (Arg == "-m") { MemReq = true; }
					if (Arg == "-e") { Echo = true; }
					if (Arg == "-c") { Custom = true; }
					if (Arg == "-I") { DoRead = false; }
					if (Arg == "-i") { Info = true; }
					if (Arg == "-s") { ReadStatus = true; }
					if (Arg == "-?") { Help(); return; } 
					if (Arg == "-w") { WMI = true; }
					if (Arg == "-q") { Query = true; }
					if (Arg == "-d") { FixDisplay = true; }
				}
			} 
			catch 
			{	
				Console.WriteLine("Invalid Usage\r\n");
				Help();
				return;
			}

			if (FixDisplay)
			{
				if (Value == "" || IP == "" || Value.Length > 16) { Help(); return; }
				SendCommandReadResponse(IP, String.Format(UEL + 
					"@PJL RDYMSG DISPLAY = \"{0}\"\r\n" + UEL, Value), false, false);
				return;
			}

			if (Query)
			{
				if (Value == "") { Help(); return; }
				WMIQuery(Value);
				return;
			}

			if (WMI)
			{
				if (Value == "") { Help(); return; }
				GetWMIInfo(Value);
				return;
			}

			if (IP == "")
			{
				Help();
				return;
			}	

			if (ReadStatus)
			{
				SendCommandReadResponse(IP, UEL + "@PJL USTATUS DEVICE = ON\r\n" + UEL, true, true);
				
			}

			if (Info)
			{
				SendCommandReadResponse(IP, UEL + "@PJL INFO " + Value + "\r\n" + UEL, true, false);
			}

			if (Custom)
			{
				Value = Value.Replace(@"\x1B", "\x1B");
				Value = Value.Replace(@"\r", "\r");
				Value = Value.Replace(@"\n", "\n");
				SendCommandReadResponse(IP, UEL + Value + UEL, DoRead, false);
			}

			if (Echo)
			{
				SendCommandReadResponse(IP, UEL + "@PJL ECHO " + Value + "\r\n" + UEL, true, false);
			}

			if (MemReq)
			{
				SendCommandReadResponse(IP, UEL + "\x1B*s1M" + UEL, true, false);
			}
		
			if (Filename != "")
			{
				SpoolFile(IP, Filename);
			}

		}

		static void SendCommandReadResponse(string IP, string Command, bool DoRead, bool Continuos)
		{
			TcpClient S = new TcpClient(IP, PORT);
			NetworkStream oS = S.GetStream();
			byte[] InBuffer = new byte[1024];
			string In = "";
			byte[] OutBuffer = System.Text.Encoding.ASCII.GetBytes(Command);
			oS.Write(OutBuffer, 0, OutBuffer.Length);
			oS.Flush();
			Console.WriteLine(String.Format("Data sent [{0}]\n", Command));
			if (DoRead)
			{
				bool bRead = false;
				while (!bRead)
				{
					while(oS.DataAvailable)
					{
						int read = oS.Read(InBuffer, 0, InBuffer.Length);
						In += System.Text.ASCIIEncoding.ASCII.GetString(InBuffer);
						if (!Continuos) { bRead = true; }
					}
					Console.Write(In);
					In = "";
				}
			}
			oS.Close();
			S.Close();
		}

		static void SpoolFile(string IP, string Filename)
		{
			
			FileStream oR = new FileStream(Filename, FileMode.Open);
					
			TcpClient S = new TcpClient(IP, PORT);
			NetworkStream oS = S.GetStream();

			byte[] Buffer = new byte[1024];
			int Read = 0;
			int Total = 0;

			Console.WriteLine("Spooling File . . .");
			do 
			{
				Read = oR.Read(Buffer, 0, Buffer.Length);
				Total += Read;
				if (Read > 0)
				{
					oS.Write(Buffer,0,Read);
				}
			} 
			while (Read != 0);

			oS.Close();
			S.Close();
			oR.Close();

		}

		static void Help()
		{
			Console.WriteLine("A tool for finding and playing with printers.\r\n");
			Console.WriteLine("Usage: printertool -h <IP> | -f <file> \r\n" +
				"\t-m | -e | -c | -I | -i | -s | -d | -w | -q | -? | -v <value>\n");
			Console.WriteLine("-h	- Specifies printers IP address");
			Console.WriteLine("-f	- Spools <file> to printer");
			Console.WriteLine("-m	- Requests memory information");
			Console.WriteLine("-e	- Request echo of -v value");
			Console.WriteLine("-c	- Custom command");
			Console.WriteLine("-I	- Ignore response");
			Console.WriteLine("-i	- Send PJL info request with -v <value> as request type.\n\t\tValid requests are: ID, CONFIG, FILESYS, MEMORY, PAGECOUNT\n\t\tSTATUS, VARIABLES, USTATUS");
			Console.WriteLine("-s	- Enters status read mode, (blocks printer)");
			Console.WriteLine("-w	- Use WMI to obtain info about -v <value>");
			Console.WriteLine("-q	- Use WMI to Query and show results of -v <value>");
			Console.WriteLine("-d	- Set the display of -h <host IP> to the value of -v <value> (16 chars)");
			Console.WriteLine("-?	- Shows this message");
		}

		static void GetWMIInfo(string Printer)
		{
			try 
			{
				string Path = "win32_printer.DeviceId=\"" + Printer + "\"";
				Console.WriteLine("Getting WMI Status on " + Path);
				ManagementObject printer = new ManagementObject(Path);
				printer.Get();
				PropertyDataCollection props = printer.Properties;	
				foreach (PropertyData item in props)
				{
					Console.WriteLine(item.Name + " = " + item.Value);
				}
			} 
			catch (Exception e) 
			{
				Console.WriteLine("WMI Error - " + e.Message);
			}
		}

		static void WMIQuery(string sQuery)
		{
			SelectQuery Query = new SelectQuery(sQuery);
			ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Query);
			foreach(ManagementBaseObject obj in Searcher.Get())
			{
				Console.WriteLine("----------------------------------\n" + obj["name"] + 
					"\n----------------------------------");
				foreach (PropertyData item in obj.Properties)
				{
					Console.WriteLine(item.Name + " = " + item.Value);
				}
				Console.WriteLine();
			}
		}

	}
}
