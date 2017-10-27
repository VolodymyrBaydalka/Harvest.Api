# Harvest.Api

A .Net client for the [Harvest API v2][0].

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
    

[0]:https://help.getharvest.com/api-v2/
