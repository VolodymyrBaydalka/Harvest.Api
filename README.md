[![zv MyGet Build Status](https://www.myget.org/BuildSource/Badge/zv?identifier=3b4888a9-f4db-434e-baac-7b3518d0f7af)](https://www.myget.org/Package/Details/zv?packageType=nuget&packageId=Harvest.Api)

# Harvest.Api

A .Net client for the [Harvest API v2][0].

Installation
------------

This library is hosted as a [nuget package][1].

To install Harvest.Net, run the following command in the Package Manager Console

    PM> Install-Package Harvest.Api
    
Usage
-----
Create a client object:

    var client = new HarvestClient("access_token");

Call API methods

    var projects = await client.GetProjectAssignmentsAsync();
    
Use authorization helper for OAuth2 Authorization
    
    var auth = new HarvestAuthentication
    {
        RedirectUri = new Uri("<RedirectUri>"),
        ClientId = "<ClientId>", // TODO
        ClientSecret = "<ClientSecret>", // TODO
        UserAgent = "HavestApiClient" // TODO
    };
    
    var authUrl = auth.BuildUrl();
    
    // open url via web browser component for WPF, Windows Forms application or redirect to this url for asp.net application

    // you can use helper to parse redirect url 
    var client = await auth.HandleCallback(callbackUri);
    
# Current State

Library contains all API I needed. Everything else will be added someday.

[0]:https://help.getharvest.com/api-v2/
[1]:https://www.nuget.org/packages/Harvest.Api/
