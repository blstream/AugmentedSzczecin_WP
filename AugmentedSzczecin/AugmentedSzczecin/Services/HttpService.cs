﻿using AugmentedSzczecin.Interfaces;
using AugmentedSzczecin.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AugmentedSzczecin.Services
{
    public class HttpService : IHttpService
    {
        private HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://78.133.154.62:1080/") };

        public async Task<ObservableCollection<PointOfInterest>> GetPointOfInterestList()
        {
            HttpResponseMessage response = await _client.GetAsync("places");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            ObservableCollection<PointOfInterest> PointOfInterestList = JsonConvert.DeserializeObject<ObservableCollection<PointOfInterest>>(json);
            return PointOfInterestList;
        }

        public async Task<ObservableCollection<PointOfInterest>> GetPointOfInterestList(string latitude, string longitude, string radius, string category = null)
        {
            HttpResponseMessage response = null;
            if (category == null)
            {
                response = await _client.GetAsync(string.Format("q?lt={0}&lg={1}&radius={2}", latitude, longitude, radius));
            }
            else
            {
                response = await _client.GetAsync(string.Format("q?lt={0}&lg={1}&radius={2}&cat={3}", latitude, longitude, radius, category));
            }
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            ObservableCollection<PointOfInterest> PointOfInterestList = JsonConvert.DeserializeObject<ObservableCollection<PointOfInterest>>(json);
            return PointOfInterestList;
        }

        public async Task<bool> CreateAccount(User user)
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.Unicode, "application/json");
            var response = await _client.PostAsync("users", content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> SignIn(User user)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Unicode.GetBytes(string.Format("{0}:{1}", user.Email, user.Password))));
            var response = await _client.GetAsync("places");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> ResetPassword(User user)
        {
            return false;
        }

        public async Task<bool> AddPointOfInterest(PointOfInterest poi)
        {
            var json = JsonConvert.SerializeObject(poi);
            var content = new StringContent(json, Encoding.Unicode, "application/json");
            var response = await _client.PostAsync("places", content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }
    }
}
