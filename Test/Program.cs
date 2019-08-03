﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlobHelper;

namespace TestNetCore
{
    class Program
    {
        static StorageType _StorageType;
        static Blobs _Blobs;
        static AwsSettings _AwsSettings;
        static AzureSettings _AzureSettings;
        static DiskSettings _DiskSettings;
        static KvpbaseSettings _KvpbaseSettings;

        static void Main(string[] args)
        {
            SetStorageType();
            InitializeClient();

            bool runForever = true;
            while (runForever)
            { 
                string cmd = InputString("Command [? for help]:", null, false);
                switch (cmd)
                {
                    case "?":
                        Menu();
                        break;
                    case "q":
                        runForever = false;
                        break;
                    case "c":
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;
                    case "get":
                        ReadBlob();
                        break;
                    case "write":
                        WriteBlob();
                        break;
                    case "del":
                        DeleteBlob();
                        break;
                    case "upload":
                        UploadBlob();
                        break;
                    case "download":
                        DownloadBlob();
                        break;
                    case "exists":
                        BlobExists();
                        break;
                    case "md":
                        BlobMetadata();
                        break;
                    case "enum":
                        Enumerate();
                        break;
                }
            }
        }

        static void SetStorageType()
        {
            bool runForever = true;
            while (runForever)
            {
                string storageType = InputString("Storage type [aws azure disk kvp]:", "disk", false);
                switch (storageType)
                {
                    case "aws":
                        _StorageType = StorageType.AwsS3;
                        runForever = false;
                        break;
                    case "azure":
                        _StorageType = StorageType.Azure;
                        runForever = false;
                        break;
                    case "disk":
                        _StorageType = StorageType.Disk;
                        runForever = false;
                        break;
                    case "kvp":
                        _StorageType = StorageType.Kvpbase;
                        runForever = false;
                        break;
                    default:
                        Console.WriteLine("Unknown answer: " + storageType);
                        break;
                }
            }
        }

        static void InitializeClient()
        {
            switch (_StorageType)
            {
                case StorageType.AwsS3:
                    _AwsSettings = new AwsSettings(
                        InputString("Access key :", null, false),
                        InputString("Secret key :", null, false),
                        InputString("Region     :", "USWest1", false),
                        InputString("Bucket     :", null, false));
                    _Blobs = new Blobs(_AwsSettings);
                    break;
                case StorageType.Azure:
                    _AzureSettings = new AzureSettings(
                        InputString("Account name :", null, false),
                        InputString("Access key   :", null, false),
                        InputString("Endpoint URL :", null, false),
                        InputString("Container    :", null, false));
                    _Blobs = new Blobs(_AzureSettings);
                    break;
                case StorageType.Disk:
                    _DiskSettings = new DiskSettings(
                        InputString("Directory :", null, false));
                    _Blobs = new Blobs(_DiskSettings);
                    break;
                case StorageType.Kvpbase:
                    _KvpbaseSettings = new KvpbaseSettings(
                        InputString("Endpoint URL :", null, false),
                        InputString("User GUID    :", null, false),
                        InputString("Container    :", null, true),
                        InputString("API key      :", null, false));
                    _Blobs = new Blobs(_KvpbaseSettings);
                    break;
            }
        }

        static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

        static void Menu()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?          Help, this menu");
            Console.WriteLine("  cls        Clear the screen");
            Console.WriteLine("  q          Quit");
            Console.WriteLine("  get        Get a BLOB");
            Console.WriteLine("  write      Write a BLOB");
            Console.WriteLine("  del        Delete a BLOB");
            Console.WriteLine("  upload     Upload a BLOB from a file");
            Console.WriteLine("  download   Download a BLOB from a file");
            Console.WriteLine("  exists     Check if a BLOB exists");
            Console.WriteLine("  md         Retrieve BLOB metadata");
            Console.WriteLine("  enum       Enumerate a bucket");
        }

        static void WriteBlob()
        {
            bool success = _Blobs.Write(
                InputString("Key:", null, false),
                InputString("Content type:", "text/plain", false),
                Encoding.UTF8.GetBytes(InputString("Data:", null, false))).Result;
            Console.WriteLine("Success: " + success);
        }

        static void ReadBlob()
        {
            byte[] data = _Blobs.Get(InputString("Key:", null, false)).Result;
            if (data != null && data.Length > 0)
            {
                Console.WriteLine(Encoding.UTF8.GetString(data));
            }
        }

        static void DeleteBlob()
        {
            bool success = _Blobs.Delete(
                InputString("Key:", null, false)).Result;
            Console.WriteLine("Success: " + success);
        }

        static void BlobExists()
        {
            Console.WriteLine(_Blobs.Exists(InputString("Key:", null, false)).Result);
        }

        static void UploadBlob()
        {
            string filename = InputString("Filename:", null, false);
            string key = InputString("Key:", null, false);
            string contentType = InputString("Content type:", null, true);

            FileInfo fi = new FileInfo(filename);
            long contentLength = fi.Length;

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                if (_Blobs.Write(key, contentType, contentLength, fs))
                {
                    Console.WriteLine("Success");
                }
                else
                {
                    Console.WriteLine("Failed");
                }
            }
        }

        static void DownloadBlob()
        {
            string key = InputString("Key:", null, false);
            string filename = InputString("Filename:", null, false);

            long contentLength = 0;
            Stream stream = null;

            if (_Blobs.Get(key, out contentLength, out stream))
            {
                using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    int bytesRead = 0;
                    long bytesRemaining = contentLength;
                    byte[] buffer = new byte[65536];

                    while (bytesRemaining > 0)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            fs.Write(buffer, 0, bytesRead);
                            bytesRemaining -= bytesRead;
                        }
                    }
                }

                Console.WriteLine("Success");
            }
            else
            {
                Console.WriteLine("Failed");
            } 
        }

        static void BlobMetadata()
        {
            BlobMetadata md = null;
            bool success = _Blobs.GetMetadata(
                InputString("Key:", null, false),
                out md);
            if (success)
            {
                Console.WriteLine(md.ToString());
            }
        }

        static void Enumerate()
        {
            List<BlobMetadata> blobs = null;
            string nextContinuationToken = null;

            if (_Blobs.Enumerate(
                InputString("Continuation token:", null, true),
                out nextContinuationToken,
                out blobs))
            {
                if (blobs != null && blobs.Count > 0)
                {
                    foreach (BlobMetadata curr in blobs)
                    {
                        Console.WriteLine(
                            String.Format("{0,-27}", curr.Key) +
                            String.Format("{0,-18}", curr.ContentLength.ToString() + " bytes") +
                            String.Format("{0,-30}", curr.Created.ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                }
                else
                {
                    Console.WriteLine("(none)");
                }

                if (!String.IsNullOrEmpty(nextContinuationToken)) Console.WriteLine("Continuation token: " + nextContinuationToken);
            }
        }
    }
}