using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

using EeveeBot.Classes.Json;
using System.Net.Http;

namespace EeveeBot.Classes.Services
{
    public class JsonWrapper_Service
    {
        public string ConfigPath { get; protected set; }

        public JsonWrapper_Service(string cnfgPath)
        {
            ConfigPath = cnfgPath;
            if(!File.Exists(ConfigPath))
            {
                UpdateJsonAsync(new Config_Json()).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize from</typeparam>
        /// <param name="tObj">The object to serialize</param>
        /// <param name="tPath">The path of the target json file</param>
        /// <returns></returns>
        public async Task UpdateJsonAsync<T>(T tObj, string tPath = null)
        {
            tPath = tPath ?? ConfigPath;
            try
            {
                using (StreamWriter writer = new StreamWriter(tPath))
                {
                    string objToString = JsonConvert.SerializeObject(tObj);
                    await writer.WriteAsync(objToString);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type to deserialize the json to</typeparam>
        /// <param name="tPath">The path of the target json file</param>
        /// <returns></returns>
        public async Task<T> GetJsonObjectAsync<T>(string tPath = null) where T : class
        {
            tPath = tPath ?? ConfigPath;
            try
            {
                using (StreamReader reader = new StreamReader(tPath))
                {
                    string jsonRaw = await reader.ReadToEndAsync();
                    T obj = JsonConvert.DeserializeObject<T>(jsonRaw);
                    return obj;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public async Task<T> GetJsonObjectAsync<T>(Uri uri) where T : class
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string jsonRaw = await httpClient.GetStringAsync(uri);
                    return GetJsonObjectFromString<T>(jsonRaw);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type to deserialize the json to</typeparam>
        /// <param name="raw">The full Json as a string</param>
        /// <returns></returns>
        public T GetJsonObjectFromString<T>(string raw) where T : class
        {
            try
            {
                T obj = JsonConvert.DeserializeObject<T>(raw);
                return obj;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
