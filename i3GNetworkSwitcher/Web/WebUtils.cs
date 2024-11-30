using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Web
{
    static class WebUtils
    {
        /// <summary>
        /// Responds to a request with text, setting the headers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="response"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static async Task RespondText(this HttpListenerResponse e, string response, int statusCode = 200)
        {
            //Serialize
            byte[] ser = Encoding.UTF8.GetBytes(response);

            //Set headers
            e.ContentLength64 = ser.Length;
            e.ContentType = "application/text";
            e.StatusCode = statusCode;

            //Send
            using (Stream s = e.OutputStream)
                await s.WriteAsync(ser, 0, ser.Length);
        }

        /// <summary>
        /// Responds to a request with JSON, setting the headers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="response"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static async Task RespondJson<T>(this HttpListenerResponse e, T response, int statusCode = 200)
        {
            //Serialize
            byte[] ser = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, Formatting.Indented));

            //Set headers
            e.ContentLength64 = ser.Length;
            e.ContentType = "application/json";
            e.StatusCode = statusCode;

            //Send
            using (Stream s = e.OutputStream)
                await s.WriteAsync(ser, 0, ser.Length);
        }

        /// <summary>
        /// Reads a request body JSON and deserializes it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task<T> ReadRequestBodyJson<T>(this HttpListenerRequest e)
        {
            //Read all text
            string text;
            using (Stream s = e.InputStream)
            using (StreamReader sr = new StreamReader(s))
                text = await sr.ReadToEndAsync();

            //Deserialize JSON
            return JsonConvert.DeserializeObject<T>(text);
        }
    }
}
