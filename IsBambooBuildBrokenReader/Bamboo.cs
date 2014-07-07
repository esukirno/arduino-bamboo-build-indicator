﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IsBambooBuildBrokenReader
{
    public class Bamboo
    {
        private readonly Uri root;
        private HttpClient client;

        public Bamboo(string rootUrl)
        {
            this.root = new Uri(rootUrl);
            this.client = HttpClientFactory.Create(
                new WebRequestHandler
                {
                    CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.CacheIfAvailable)
                }
                );

            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Resource[] GetAll()
        {
            var response = client.GetStringAsync(root);
            response.Wait();

            dynamic result = JsonConvert.DeserializeObject<JObject>(response.Result);

            return JsonConvert.DeserializeObject<Resource[]>(result.resources.resource.ToString());
        }

        public Resource GetResource(string name)
        {
            return ExpectOne(GetAll().Where(x => x.Name.Equals(name)),
                "No resource found for name: " + name,
                "Multiple resources found for name:" + name);
        }

        public Result[] GetAllResults()
        {
            return GetList<Result>(
                    GetResource("result").Link,
                    response => response.results.result.ToString());
        }

        public Result GetLatestResultForPlan(string planKey)
        {
            return ExpectOne(GetAllResults().Where(result => result.IsFor(planKey)),
                "No results found for plan key: " + planKey,
                "Multiple results found for plan key: "+planKey);
        }

        public Plan[] GetAllPlans()
        {
            return GetList<Plan>(
                    GetResource("plan").Link,
                    response => response.plans.plan.ToString());
        }

        public Plan GetPlan(string key)
        {
            return ExpectOne(GetAllPlans().Where( x => x.Key.Equals(key)),

                "No plans found for key: " + key,
                "Multiple plans found for plan key: " + key);
        }

        private T[] GetList<T>(Link link, Func<dynamic, string> fieldAccessor)
        {
            var response = client.GetStringAsync(link.Href);
            response.Wait();

            dynamic result = JsonConvert.DeserializeObject<JObject>(response.Result);

            return JsonConvert.DeserializeObject<T[]>(fieldAccessor(result));
        }

        public T ExpectOne<T>(IEnumerable<T> array, string zeroMessage, string multipleMessage)
        {
            if (!array.Any())
            {
                throw new Exception(zeroMessage);
            }
            if (array.Count() > 1)
            {
                throw new Exception(multipleMessage);
            }

            return array.Single();
        }
        public class Resource
        {
            public string Name { get; set; }
            public Link Link { get; set; }
        }

        public class Link
        {
            public string Rel { get; set; }
            public string Href { get; set; }
        }

        public class Result
        {
            public int Id { get; set; }
            public int Number { get; set; }
            public string State { get; set;}
            public string LifeCycleState { get; set; }
            public Plan Plan { get; set; }
            public Link Link { get; set; }

            public bool IsFor(string planKey)
            {
                return Plan.Key.Contains(planKey);
            }

            public bool WasSuccessful()
            {
                return State.Equals("Successful");
            }
            public bool Finished()
            {
                return LifeCycleState.Equals("Finished");
            }
        }

        public class Plan
        {
            public string ShortName { get; set; }
            public string ShortKey { get; set; }
            public string Type { get; set; }
            public bool Enabled { get; set; }
            public string Key { get; set; }
            public string Name { get; set; }
            public bool IsBuilding { get; set; }
            public bool IsActive { get; set; }
            public Link Link { get; set; }
        }
    }
}