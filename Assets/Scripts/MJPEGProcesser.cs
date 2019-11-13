using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class MJPEGProcesser {
    private readonly byte [] jpegHeader = new byte[] { 0xff, 0xd8 };
    private int _chunkSize;
    private bool _isStreamActive = false;
    public byte [] CurrentFrame { get; private set; }
    private SynchronizationContext _context;
    private bool _responseReceived;
    public event EventHandler<FrameReadyEventArgs> FrameReady;
    public event EventHandler<ErrorEventArgs> Error;

    public MJPEGProcesser(int chunkSize = 1024 * 4)
    {
        _context = SynchronizationContext.Current;
        _chunkSize = chunkSize;
    }


    public void SetUri(Uri uri)
    {
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
        request.BeginGetResponse(OnGetResponse, request);
    }

    private void OnGetResponse(IAsyncResult result)
    {
        _responseReceived = true;
        Debug.Log("OnGetResponse");
        byte [] imageBuffer = new byte[1024 * 1024];
        HttpWebRequest request = (HttpWebRequest) result.AsyncState;

        Debug.Log("Starting request");
        try
        {
            HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(result);
            Debug.Log("response received");
            string contentType = response.Headers["Content-Type"];
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("="))
            {
                Debug.Log("MJPEG Exception thrown");
                throw new Exception("Invalid content-type header");
            }

            string boundary = contentType.Split('=')[1].Replace("\"", "");
            byte[] boundaryBytes = Encoding
                .UTF8
                .GetBytes(boundary.StartsWith("--") ? boundary : "--" + boundary);
            Stream stream = response.GetResponseStream();
            BinaryReader reader = new BinaryReader(stream);

            _isStreamActive = true;
            byte[] buff = reader.ReadBytes(_chunkSize);
            while (_isStreamActive)
            {
                int imageStart = FindBytes(buff, jpegHeader);
                if (imageStart != -1)
                {
                    int size = buff.Length - imageStart;
                    Array.Copy(buff, imageStart, imageBuffer, 0, size);

                    while (true)
                    {
                        buff = reader.ReadBytes(_chunkSize);

                        int imageEnd = FindBytes(buff, boundaryBytes);
                        if (imageEnd != -1)
                        {
                            Array.Copy(buff, 0, imageBuffer, size, imageEnd);
                            size += imageEnd;
                            
                            byte [] frame = new byte[size];
                            Array.Copy(imageBuffer, 0, frame, 0, size);
                            CurrentFrame = frame;

                            if (FrameReady != null)
                            {
                               FrameReady(this, new FrameReadyEventArgs()); 
                            }
                            
                            Array.Copy(buff, imageEnd, buff, 0, buff.Length - imageEnd);
                            byte[] temp = reader.ReadBytes(imageEnd);
                            Array.Copy(temp, 0, buff, buff.Length - imageEnd, temp.Length);
                            break;
                        }
                        
                        Array.Copy(buff, 0, imageBuffer, size, buff.Length);
                        size += buff.Length;

                        if (!_isStreamActive)
                        {
                            Debug.Log("CLOSING");
                            response.Close();
                            break;
                        }
                    }
                }
            }
            
            response.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            if (Error != null)
            {
                _context.Post(delegate { Error(this, new ErrorEventArgs() { Message = e.Message});  }, null);
            }
        }
    }

// パフォーマンス？
    private int FindBytes(byte[] buff, byte[] search)
    {
        for (int i = 0; i < buff.Length - search.Length; i++)
        {
            if (buff[i] == search[0])
            {
                int next;

                for (next = 1; next < search.Length; next++)
                {
                    if (buff[i + next] != search[next])
                    {
                        break;
                    }
                }

                if (next == search.Length)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}


public class FrameReadyEventArgs : EventArgs
{
  
}

public sealed class ErrorEventArgs : EventArgs

{
    public string Message { get; set; }
    public int ErrorCode { get; set; }
}