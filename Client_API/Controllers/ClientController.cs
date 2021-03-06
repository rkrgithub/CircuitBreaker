﻿using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Client_API.Controllers
{
    public class ClientController : ApiController
    {
        string _baseAddress = "http://localhost:8088/api/products";
        //52892
        static Action<Exception, TimeSpan> onBreak = (exception, timespan) =>
        {
            LogMessageToFile("On Break, time span : " + timespan.ToString());
        };
        static Action onReset = () =>
        {
            LogMessageToFile("On Reset");
        };
        static Action onHalfOpen = () =>
        {
            LogMessageToFile("on HalfOpen");
        };
        static CircuitBreakerPolicy circuitBreaker = Policy
                                .Handle<Exception>()
                                .CircuitBreakerAsync(
                                    exceptionsAllowedBeforeBreaking: 1,
                                    durationOfBreak: TimeSpan.FromSeconds(20),
                                    onBreak: onBreak,
                                    onReset: onReset,
                                    onHalfOpen: onHalfOpen
                                );


        [HttpGet]
        public async Task<string> Get()
        {
            LogMessageToFile("------- Calling Get API ------");

            HttpResponseMessage res = null;
            await circuitBreaker.ExecuteAsync(async () => res = await ServiceCall(new Uri($"{_baseAddress}/GetProducts")));
            return await res.Content.ReadAsStringAsync();
        }

        [HttpGet]
        public async Task<string> GetwithDelay(int delay = 0)
        {
            LogMessageToFile("------- Calling GetwithDelay API with dalay " + delay + " -----");

            HttpResponseMessage res = null;
            await circuitBreaker.ExecuteAsync(async () => res = await ServiceCall(new Uri($"{_baseAddress}/GetdelayedProducts?delay={delay}")));
            return await res.Content.ReadAsStringAsync();
        }

        [HttpGet]
        public async Task<string> GetException()
        {
            LogMessageToFile("------- Calling GetException -----");

            HttpResponseMessage res = null;
            await circuitBreaker.ExecuteAsync(async () => res = await ServiceCall(new Uri($"{_baseAddress}/GetException")));
            return await res.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> ServiceCall(Uri uri)
        {
            HttpResponseMessage response = null;
            try
            {
                using (var client = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(5)
                })
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    LogMessageToFile("Executing Http Call");
                    response = await client.GetAsync(uri);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResponseMessage(response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return response;
        }

        /// <summary>
        /// For logging to a file
        /// </summary>
        /// <param name="msg"></param>
        public static void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                 AppDomain.CurrentDomain.BaseDirectory + "My Log File.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }


    }
}
