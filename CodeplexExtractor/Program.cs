using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeplexExtractor
{
    class MainClass
    {
        public const int loopLimit = 999;
        public static string unpackDir = "SomeDir";
        public static void Main(string[] args)
        {
            Console.WriteLine("Codeplex Extractor");
            if(args.Length==1)
            {
                string zip = args[0];
                if (File.Exists(zip))
                {
                    try
                    {
                        Extract(zip);

                    }
                    catch(Exception E)
                    {
                        Console.Error.WriteLine(E.Message);
                        Console.Error.WriteLine(E.StackTrace);
                    }
                }
            }
        }

        public static void Extract(string filename)
        {
            unpackDir = System.Guid.NewGuid().ToString();

            Action<string> extractFunc = (string s) => { };

            if (Win) extractFunc = ExtractWin;
            if (Nix) extractFunc = ExtractNix;

            extractFunc(filename);

            string release = "./"+unpackDir+"/releases/releaseList.json";
            if (File.Exists(release))
            {
                string outdir = "./codeplex_out";

                if (Directory.Exists(outdir+"/"))
                {
                    int index = 2;
                    while (true)
                    {
                        Console.WriteLine("Loop: {0}/{1}", index, loopLimit);
                        string outdirNext = outdir + index.ToString() + "/";
                        if (Directory.Exists(outdirNext))
                        {
                            ++index;
                        }
                        else
                        {
                            outdir = outdirNext;
                            break;
                        }
                        if (index > loopLimit) return;
                    }
                }

                Directory.CreateDirectory(outdir + "/");
                Directory.CreateDirectory(outdir + "/releases");

                Console.WriteLine("json");
                string txt = System.IO.File.ReadAllText(release);
                dynamic json = JArray.Parse(txt);
                foreach(JObject element in json)
                {
                    dynamic d = element;
                    Console.WriteLine(d.Id+" "+d.Name);
                    string rDir=outdir+"/releases/" + d.Id.ToString();
                    Directory.CreateDirectory(rDir);
                    if (d.Description!=null)
                    {
                        if(!String.IsNullOrWhiteSpace(d.Description.ToString()))
                        {
                            string descFile = rDir + "/desc.txt";
                            File.WriteAllText(descFile, d.Description.ToString());
                        }
                    }

                    dynamic Files = d.Files;
                    foreach(JObject file in Files)
                    {

                        dynamic dd = file;
                        Console.WriteLine("{0} <=> {1}", dd.Id, dd.FileName);
                        string src = "./"+unpackDir+"/releases/" + d.Id + "/" + dd.Id;
                        string dest = rDir + "/" + dd.FileName;
                        Console.WriteLine("{0} <=> {1}", src, dest);
                        File.Copy(src, dest);
                    }

                }
                Console.WriteLine("parsed!");
            }
            else
            {
                Console.WriteLine("Json not detected...");
            }

        }
        public static bool Nix
        {
            get
            {
                int Platform = (int)Environment.OSVersion.Platform;
                switch (Platform)
                {
                    case 4:
                        return true;
                    case 6:
                        return true;
                    case 128:
                        return true;
                    default:
                        return false;
                }
            }
        }
        public static bool Win
        {
            get
            {
                return !Nix;
            }
        }
        public static void ExtractNix(string filename)
        {
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo();
            p.StartInfo = psi;
            psi.FileName = "/usr/bin/unzip";
            psi.Arguments = String.Format("-o {0} -d "+unpackDir,filename);
            psi.WorkingDirectory = Environment.CurrentDirectory;
            p.Start();
            p.WaitForExit();

        }
        public static void ExtractWin(string filename)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(filename,unpackDir);
        }
    }
}
