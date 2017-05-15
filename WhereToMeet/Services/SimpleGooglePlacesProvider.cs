﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhereToMeet.Transporters.Input;
using WhereToMeet.Transporters.Output;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Globalization;
using WhereToMeet.Transporters.Output.GooglePlaces;
using Newtonsoft.Json;
using WhereToMeet.Transporters;
using WheretoMeet.Services.PlacesProviders;

namespace WhereToMeet.Services.PlacesProviders
{
    public class SimpleGooglePlacesProvider : IPlacesProvider
    {
        class GooglePlacesResponseStatus
        {
            public const string Ok = "OK";
            public const string NotFound = "NOT_FOUND";
            public const string ZeroResults = "ZERO_RESULTS";
            public const string InvalidRequest = "INVALID_REQUEST";
        }
        protected HttpClient Client { get; set; }
        protected IConfigurationRoot Configuration { get; }
        public SimpleGooglePlacesProvider(IConfigurationRoot configuration)
        {
            this.Client = new HttpClient()
                          {
                            BaseAddress = new Uri(configuration["GeoDataServices:GooglePlaces:Url"])
                          };
            this.Configuration = configuration;
        }
        public async Task<IEnumerable<PlaceTransporter>> LookForNearbyPlacesAsync(PlacesQueryTransporter query)
        {
            var placesList = new List<PlaceTransporter>();
            foreach (string placeType in query.PlacesTypes)
            {
                var foundPlaces = await this.FireGooglePlacesQueryAsync(placeType, query);
                if (foundPlaces != null)
                    placesList.AddRange(foundPlaces.Results.Select(r => (PlaceTransporter)r));
            }
            return placesList;
        }

        private async Task<GooglePlacesResponse> FireGooglePlacesQueryAsync(string placeType, PlacesQueryTransporter query)
        {
            string latitudeString = query.Latitude.ToString(CultureInfo.InvariantCulture);
            string longitudeString = query.Longitude.ToString(CultureInfo.InvariantCulture);
            string radiusString = 500.ToString(CultureInfo.InvariantCulture);
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("key", Configuration["GeoDataServices:GooglePlaces:ApiKey"]),
                new KeyValuePair<string, string>("location", $"{latitudeString},{longitudeString}"),
                new KeyValuePair<string, string>("radius", radiusString),
                new KeyValuePair<string, string>("type", placeType)
            });
            var response = await this.Client.GetAsync($"?{await parameters.ReadAsStringAsync()}");
            if (response.IsSuccessStatusCode)
            {
                var gPlaceTransporter = JsonConvert.DeserializeObject<GooglePlacesResponse>(await response.Content.ReadAsStringAsync());
                if (gPlaceTransporter.Status == GooglePlacesResponseStatus.Ok)
                    return gPlaceTransporter;
            }
            return null;
        }
    }
}