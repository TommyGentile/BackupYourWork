using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.IO.Compression;

namespace BackupYourWork
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "BackupYourWork " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine("***************************************************************************");
            Console.WriteLine("                            BACKUP YOUR WORK                               ");
            Console.WriteLine("***************************************************************************");
            Console.WriteLine();

            ConfigManager i = ConfigManager.Instance;
            String error = i.readSettings();
            if (error != null) { Console.WriteLine("Error .config: " + error); return; }

            DirectoryInfo backupDir = new DirectoryInfo(Environment.CurrentDirectory + "\\" + i.backup_foldername);

            //DateTime from_date = DateTime.Now.AddDays(i.days_interval);
            //DateTime to_date = DateTime.Now;

            //var files = percorso_netasiu.GetFiles().Where(file => file.LastWriteTime >= from_date && file.LastWriteTime <= to_date);

            Console.WriteLine(" Searching..");

            List<FileInfo> files = Program.GetLastUpdatedFileInDirectory(i.backup_path, i.days_interval, i.extensions);

            Console.WriteLine();
            Console.WriteLine(" -> GetLastUpdatedFileInDirectory: " + files.Count + " files.");
            Console.WriteLine();

            if (files.Count > 0)
            {
                String deltaSlice = DateTime.Now.ToString("yyyyMMdd_hhmmss");
                DirectoryInfo deltaDir = new DirectoryInfo(Environment.CurrentDirectory + "\\" + i.backup_foldername + "\\" + deltaSlice);
                deltaDir.Create();

                Console.WriteLine();
                Console.WriteLine(" Backup..");
                Console.WriteLine();
                Console.WriteLine(" -> /" + deltaSlice + "/");
                Console.WriteLine();

                BackupFilesInDirectory(files, i.backup_path, deltaDir);

                Console.WriteLine();
                Console.WriteLine(" Consolidating..");
                Console.WriteLine();

                DirectoryInfo consolidateDir = new DirectoryInfo(Environment.CurrentDirectory + "\\" + i.backup_foldername + "\\" + "__ALL");
                consolidateDir.Create();

                ConsolidateBackup(deltaDir, consolidateDir);

                if (i.mail_enable)
                {
                    Console.WriteLine();
                    Console.WriteLine(" Compressing.. ");
                    Console.WriteLine();

                    String zipName = String.Format("{0}_{1}.zip", i.backup_foldername.ToLower(), deltaSlice);
                    FileInfo zipFile = new FileInfo(backupDir.FullName + "\\" + zipName);

                    ZipFile.CreateFromDirectory(deltaDir.FullName, zipFile.FullName, CompressionLevel.Fastest, true);

                    Console.WriteLine();
                    Console.WriteLine(" Sending eMail.. ");
                    Console.WriteLine();

                    sendMail(zipFile.FullName);

                    //DeleteZip(zipFile.FullName);
                }
            }

            Console.WriteLine(" Done!");
            Console.WriteLine(" made by Tommy Gentile.");

            Console.ReadLine();
        }

        private static void DeleteZip(String zipFile)
        {
            try
            {
                FileInfo zip = new FileInfo(zipFile);
                zip.Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [E] " + ex.Message);
                File.AppendAllText(Environment.CurrentDirectory + "\\byw_log.txt", ex.Message + Environment.NewLine);
            }
        }

        private static void ConsolidateBackup(DirectoryInfo source, DirectoryInfo target)
        {
            if (Directory.Exists(target.FullName) == false) Directory.CreateDirectory(target.FullName);

            foreach (FileInfo file in source.GetFiles())
            {
                try
                {
                    file.CopyTo(CheckReadOnly(Path.Combine(target.FullName, file.Name)), true);

                    //Console.WriteLine(" + " + file.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" [E] " + file.Name);
                    File.AppendAllText(Environment.CurrentDirectory + "\\byw_log.txt", file.Name + Environment.NewLine);
                }
            }

            //Recursive
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                ConsolidateBackup(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static void sendMail(String attachmentFile)
        {
            try
            {
                ConfigManager i = ConfigManager.Instance;
                String error = i.readSettings();

                MailMessage mm = new MailMessage(i.mail_from, i.mail_to);

                SmtpClient client = new SmtpClient();
                client.Port = i.mail_port ?? 587;
                client.Host = i.mail_host ?? "smtp.gmail.com";

                client.EnableSsl = true;
                client.Timeout = 90000;

                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("tommy.gentile@eng.it", "StoCazzo1981!");

                //mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                mm.Subject = "BackupYourWork - Your daily backup";

                //mm.BodyEncoding = UTF8Encoding.UTF8;
                //mm.Body = "";

                Attachment attachment = new Attachment(attachmentFile);
                mm.Attachments.Add(attachment);

                client.Send(mm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" [E] " + ex.Message);
                File.AppendAllText(Environment.CurrentDirectory + "\\byw_log.txt", ex.Message + Environment.NewLine);
            }
        }

        public static string CheckReadOnly(string filePath)
        {
            if (!File.Exists(filePath)) return filePath;

            FileAttributes attr = File.GetAttributes(filePath);
            if ((attr & FileAttributes.ReadOnly) != 0) File.SetAttributes(filePath, FileAttributes.Normal);
            return filePath;
        }

        private static void BackupFilesInDirectory(List<FileInfo> files, DirectoryInfo baseDir, DirectoryInfo targetDir)
        {
            foreach (FileInfo file in files)
            {
                if (file.Name.Contains("TemporaryGeneratedFile"))
                    continue;

                string dirPath = Path.GetDirectoryName(file.FullName);

                string target = dirPath.Replace(Path.GetDirectoryName(baseDir.FullName), targetDir.FullName);
                Directory.CreateDirectory(target);

                try
                {
                    file.CopyTo(Path.Combine(target, file.Name), true);
                    Console.WriteLine(" + " + file.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" [E] " + file.Name);
                    File.AppendAllText(Environment.CurrentDirectory + "\\byw_log.txt", file.Name + Environment.NewLine);
                }
            }
        }

        private static List<FileInfo> GetLastUpdatedFileInDirectory(DirectoryInfo directory, double daysInterval, string[] extensions)
        {
            List<FileInfo> files = new List<FileInfo>();

            foreach (String ext in extensions)
                files.AddRange(directory.GetFiles("*" + ext, SearchOption.AllDirectories));

            List<FileInfo> lastUpdatedFile = new List<FileInfo>();

            DateTime lastUpdate = DateTime.Now.AddDays(daysInterval);
            Console.WriteLine("Reference date: " + lastUpdate);

            foreach (FileInfo file in files)
            {
                if (file.Name.Contains("TemporaryGeneratedFile"))
                    continue;

                if (file.CreationTime > lastUpdate
                    || file.LastWriteTime > lastUpdate)
                {
                    lastUpdatedFile.Add(file);
                    Console.WriteLine(" * " + file.Name);
                }
            }

            return lastUpdatedFile;
        }
    }
}