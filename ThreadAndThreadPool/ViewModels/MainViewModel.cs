using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CopyFile.MVVM.Views;

namespace CopyFile.MVVM.ViewModels
{
    public class MainViewModel
    {
        private List<string> allLine;
        private CancellationTokenSource _ct;
        public MainWindow MainWindow { get; set; }


        public MainViewModel()
        {
            allLine = new List<string>();
            _ct = new CancellationTokenSource();
            Thread setDataThread = new Thread(setData);
            setDataThread.Start();
        }

        private void setData()
        {
            MainWindow.OpenFile.Click += OpenFileOnClick;
            MainWindow.Start.Click += StartOnClick;
            MainWindow.Cancel.Click += CancelOnClick;
        }

        private void CancelOnClick(object sender, RoutedEventArgs e)
        {
            File.WriteAllLines(MainWindow.FilePath.Text, allLine);
            MainWindow.OpenFile.IsEnabled = true;
            MainWindow.FilePath.IsEnabled = true;
            MainWindow.EDKey.IsEnabled = true;
            MainWindow.Encrypt.IsEnabled = true;
            MainWindow.Decrypt.IsEnabled = true;
            MainWindow.Start.IsEnabled = true;
            MainWindow.Cancel.IsEnabled = false;

            _ct.Cancel();
        }
         
        private void StartOnClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow.FilePath.Text == "" || !File.Exists(MainWindow.FilePath.Text))
            {
                MessageBox.Show("Please enter path or enter correct path", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            else if (MainWindow.EDKey.Password == "")
            {
                MessageBox.Show("Please enter Encrypt or Decrypt key", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            else
            {
                allLine = File.ReadAllLines(MainWindow.FilePath.Text).ToList();
                if (allLine.Count > 0)
                {
                    MainWindow.OpenFile.IsEnabled = false;
                    MainWindow.FilePath.IsEnabled = false;
                    MainWindow.EDKey.IsEnabled = false;
                    MainWindow.Encrypt.IsEnabled = false;
                    MainWindow.Decrypt.IsEnabled = false;
                    MainWindow.Start.IsEnabled = false;
                    MainWindow.Cancel.IsEnabled = true;

                    bool state = false;

                    state = MainWindow.Encrypt.IsChecked != false;

                    ThreadPool.QueueUserWorkItem((o)=>
                    {
                        work(_ct.Token, state);
                    });
                }
                else
                {
                    MessageBox.Show("Please select file inside anything write", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
        }

        private void work(CancellationToken token, bool state)
        {
            List<string> EDData = new List<string>();
            string key = "";
            string path = "";
            MainWindow.EDKey.Dispatcher.BeginInvoke(new Action((() => { key = MainWindow.EDKey.Password; })));
            MainWindow.FilePath.Dispatcher.BeginInvoke(new Action((() => { path = MainWindow.FilePath.Text; })));

            if (state)
            {
                foreach (var line in allLine)
                    EDData.Add(Encrypt(line, key));
            }
            else if (!state)
            {
                foreach (var line in allLine)
                    EDData.Add(Decrypt(line, key));
            }

            File.WriteAllText(path, string.Empty);

            double count = 100.0 / EDData.Count;
            for (int i = 0; i < allLine.Count; i++)
            {
                if (!token.IsCancellationRequested)
                {
                    File.AppendAllText(path, EDData[i] + '\n');
                    Thread.Sleep(1500);
                    MainWindow.State.Dispatcher.BeginInvoke(new Action(() => { MainWindow.State.Value += count; }));
                }
                else break;
            }

            MainWindow.OpenFile.Dispatcher.BeginInvoke(new Action((() => { MainWindow.OpenFile.IsEnabled = true; })));
            MainWindow.FilePath.Dispatcher.BeginInvoke(new Action((() => { MainWindow.FilePath.IsEnabled = true; })));
            MainWindow.EDKey.Dispatcher.BeginInvoke(new Action((() => { MainWindow.EDKey.IsEnabled = true; })));
            MainWindow.Encrypt.Dispatcher.BeginInvoke(new Action((() => { MainWindow.Encrypt.IsEnabled = true; })));
            MainWindow.Decrypt.Dispatcher.BeginInvoke(new Action((() => { MainWindow.Decrypt.IsEnabled = true; })));
            MainWindow.Start.Dispatcher.BeginInvoke(new Action((() => { MainWindow.Start.IsEnabled = true; })));
            MainWindow.Cancel.Dispatcher.BeginInvoke(new Action((() => { MainWindow.Cancel.IsEnabled = false; })));

            if (Convert.ToBoolean(state))
                MessageBox.Show("Encrypt ended successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            if (!Convert.ToBoolean(state))
                MessageBox.Show("Decrypt ended successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

            MainWindow.State.Dispatcher.BeginInvoke(new Action(() => { MainWindow.State.Value = 0; }));
        }

        public string Encrypt(string line, string key)
        {
            string EncryptionKey = key;
            byte[] clearBytes = Encoding.Unicode.GetBytes(line);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    line = Convert.ToBase64String(ms.ToArray());
                }
            }
            return line;
        }

        public string Decrypt(string line, string key)
        {
            string EncryptionKey = key;
            line = line.Replace(" ", "+");
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(line);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        line = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return line;
        }
        
        private void OpenFileOnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Txt File (*.txt)|*.txt";
            openFile.Title = "Select File";
            if (openFile.ShowDialog() != null)
            {
                MainWindow.FilePath.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainWindow.FilePath.Text = openFile.FileName;
                }));
            }
        }
    } 
}