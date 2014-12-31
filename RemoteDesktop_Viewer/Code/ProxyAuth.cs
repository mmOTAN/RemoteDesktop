﻿using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace RemoteDesktop_Viewer.Code
{
    public class ProxyAuth
    {
        private string _UserName;
        public string UserName { get { return _UserName; } }
        private bool _Authenticated = false;
        public bool Authenticated { get { return _Authenticated; } }
        private Cookie _AuthCookie = null;
        public Cookie AuthCookie { get { return _AuthCookie; } }
        private bool _UsingWindowsAuth = false;
        public bool UsingWindowsAuth { get { return _UsingWindowsAuth; } }

        public ProxyAuth(string username, string password, bool windowsauth)
        {
            _UserName = username;
            _UsingWindowsAuth = windowsauth;
            _Authenticated = AuthenticateUser(username, password);
        }
        private bool AuthenticateUser(string user, string password)
        {
            _Authenticated = false;
            if(_UsingWindowsAuth)
            {
                try
                {
                    using(var connection = new Microsoft.AspNet.SignalR.Client.HubConnection(Settings.SignalRHubUrl))
                    {
                        connection.TransportConnectTimeout = new TimeSpan(0, 0, 4);
                        Settings.SignalRHubUrl.Split('/').LastOrDefault();
                        IHubProxy stockTickerHubProxy = connection.CreateHubProxy(Settings.SignalRHubName);
                        connection.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                        connection.Error += connection_Error;
                        DateTime dt = DateTime.Now;
                        connection.Start().Wait(3000);
                        //if an error is thrown, it will set authenticated to false. If the client waits toe 2 seconds, it timed out and authenticated needs to be false
                        if((DateTime.Now - dt).TotalMilliseconds >= 3500) { _Authenticated = false; } else { _Authenticated = true; }
                        return _Authenticated;
                    }
                } catch(Exception e)
                {
                    _Authenticated = false;
                }
            } else
            {

                var request = WebRequest.Create(Settings.AuthenticationUrl) as HttpWebRequest;

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = new CookieContainer();
                request.Timeout = 5000;//5 second timeout..
                var authCredentials = "UserName=" + user + "&Password=" + password;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(authCredentials);
                request.ContentLength = bytes.Length;
                try
                {
                    using(var requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(bytes, 0, bytes.Length);
                    }

                    using(var response = request.GetResponse() as HttpWebResponse)
                    {
                        _AuthCookie = response.Cookies[".ASPXAUTH"];
                    }
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                _Authenticated = _AuthCookie != null;
            }
            return _Authenticated;


        }

        void connection_Error(Exception obj)
        {
            Debug.WriteLine(obj.Message);
        }
    }
}
