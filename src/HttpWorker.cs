using System;
using Godot;
using System.Text;
using System.Collections.Generic;

public class HttpWorker : Godot.Object
{
    private Queue<HTTPClient.Method> methods = new Queue<HTTPClient.Method>();

    private Queue<string> paths = new Queue<string>();

    private Queue<string> jsons = new Queue<string>();

    public string BaseURL;

    private string serverPassword;

    private string jwt;

    public string LastResponse = "";

    public void Setup(HTTPClient.Method method, string path, string jsonString, string userJwt)
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        this.BaseURL = (string)cf.GetValue("gameapi", "base_url");
        this.serverPassword = (string)cf.GetValue("gameapi", "server_auth");
        this.jwt = userJwt;

        this.methods.Enqueue(method);
        this.paths.Enqueue(path);
        this.jsons.Enqueue(jsonString);
    }

    public void MakeRequest(object userdata = null)
    {
        while (paths.Count > 0)
        {
            bool useSsl = false;
            int port = 80;
            if (BaseURL.Contains("http://")) 
            {
                BaseURL = BaseURL.Replace("http://", "");
            }
            if (BaseURL.Contains("https://")) 
            {
                port = 443;
                useSsl = true;
                BaseURL = BaseURL.Replace("https://", "");
            }
            string[] baseURLParts = BaseURL.Split(":");
            if (baseURLParts.Length > 1)
            {
                port = int.Parse(baseURLParts[1]);
            }
            HTTPClient http = new HTTPClient();
            Error err = http.ConnectToHost(baseURLParts[0], port, useSsl, false);
            if (err != Error.Ok) 
            {
                GD.Print($"Inventory save http setup error: {err}");
                return;
            }

            while (http.GetStatus() == HTTPClient.Status.Connecting || http.GetStatus() == HTTPClient.Status.Resolving)
            {
                http.Poll();
                OS.DelayMsec(500);
            }

            if (http.GetStatus() != HTTPClient.Status.Connected)
            {
                GD.Print($"Inventory save connection failed: {http.GetStatus()}");
                return;
            }

            HTTPClient.Method method = methods.Dequeue();
            string path = paths.Dequeue();
            string json = jsons.Dequeue();

            string[] requestHeaders = null;
            if (serverPassword != "")
            {
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("gameserver:" + serverPassword));
                requestHeaders = new string[1];
                requestHeaders[0] = $"Authorization: Basic {svcCredentials}";
            }
            if (jwt != "")
            {
                requestHeaders = new string[1];
                requestHeaders[0] = $"Authorization: Bearer {jwt}";
            }

            err = http.Request(method, path, requestHeaders, json);
            if (err != Error.Ok) 
            {
                GD.Print($"Inventory save error: {err}");
                return;
            }

            while (http.GetStatus() == HTTPClient.Status.Requesting)
            {
                http.Poll();
            }

            GD.Print($"http done: {http.GetStatus()}");

            if (http.HasResponse())
            {
                List<byte> rb = new List<byte>();
                while (http.GetStatus() == HTTPClient.Status.Body)
                {
                    http.Poll();
                    byte[] chunk = http.ReadResponseBodyChunk();
                    if (chunk.Length == 0)
                    {
                        OS.DelayMsec(500);
                    }
                    else
                    {
                        rb.AddRange(chunk);
                    }
                }

                LastResponse = Encoding.UTF8.GetString(rb.ToArray());
            }   
        }
    }
}