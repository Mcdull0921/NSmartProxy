﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSmartProxy.Data;
using NSmartProxy.Data.DTO;

namespace NSmartProxy.ClientRouter.Dispatchers
{
    public class NSPDispatcher
    {
        private string BaseUrl = "localhost";
        public NSPDispatcher(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public async Task<HttpResult<LoginFormClientResult>> LoginFromClient(string username, string userpwd)
        {
            string url = $"http://{BaseUrl}/LoginFromClient";
            HttpClient client = new HttpClient();
            var httpmsg = await client.GetAsync($"{url}?username={username}&userpwd={userpwd}");
            var httpstr = await httpmsg.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HttpResult<LoginFormClientResult>>(httpstr);
        }

        public async Task<HttpResult<LoginFormClientResult>> Login(string userid, string userpwd)
        {
            string url = $"http://{BaseUrl}/LoginFromClientById";
            HttpClient client = new HttpClient();
            var httpmsg = await client.GetAsync($"{url}?username={userid}&userpwd={userpwd}");
            var httpstr = await httpmsg.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HttpResult<LoginFormClientResult>>(httpstr);
        }

    }
}