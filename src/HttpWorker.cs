using System;
using Godot;
using System.Text;
using System.Collections.Generic;

public class HttpWorker : Godot.Object
{
    private Queue<HTTPClient.Method> methods = new Queue<HTTPClient.Method>();

    private Queue<string> paths = new Queue<string>();

    private Queue<string> jsons = new Queue<string>();

    private string baseURL;

    private string serverPassword;

    public void Setup(HTTPClient.Method method, string path, string jsonString)
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        this.baseURL = (string)cf.GetValue("gameapi", "base_url");
        this.serverPassword = (string)cf.GetValue("gameapi", "server_auth");

        this.methods.Enqueue(method);
        this.paths.Enqueue(path);
        this.jsons.Enqueue(jsonString);
    }

    public void MakeRequest(object userdata = null)
    {
        while (paths.Count > 0)
        {
            int port = 80;
            baseURL = baseURL.Replace("http://", "");
            string[] baseURLParts = baseURL.Split(":");
            port = int.Parse(baseURLParts[1]);
            HTTPClient http = new HTTPClient();
            Error err = http.ConnectToHost(baseURLParts[0], port);
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

            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("gameserver:" + serverPassword));
            string[] requestHeaders = new string[1];
            requestHeaders[0] = $"Authorization: Basic {svcCredentials}";
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
        }
    }
}