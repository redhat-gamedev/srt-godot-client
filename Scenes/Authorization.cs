using Godot;
using System;
using System.Text;
using Godot.Collections;
using System.Threading.Tasks;

public class Authorization : Control
{
  public Serilog.Core.Logger _serilogger;

  int PORT;
  string HOST;
  string clientID;
  string clientSecret;
  string authServer;
  string tokenServer;
  string refreshToken;
  string token;
  string redirectUri;
  const string SAVE_DIR = "user://auth/";
  string save_path = SAVE_DIR + "token.dat";
  const string HTML_REDIRECTION_PAGE = "res://Assets/Artwork/Home.html";
  TCP_Server redirectServer = new TCP_Server();
  [Signal] public delegate void playerAuthenticated(bool isAuthorized);

  //Load the configuration and call the login process
  public override void _Ready()
  {
    var clientConfig = new ConfigFile();

    Godot.Error err = clientConfig.Load("res://Resources/client.cfg");
    if (err == Godot.Error.Ok)
    {

      PORT = (int)clientConfig.GetValue("auth", "port");
      HOST = (String)clientConfig.GetValue("auth", "host");
      clientID = (String)clientConfig.GetValue("auth", "client_id");
      clientSecret = (String)clientConfig.GetValue("auth", "client_secret");
      authServer = (String)clientConfig.GetValue("auth", "auth_api_url");
      tokenServer = (String)clientConfig.GetValue("auth", "token_api_url");

      redirectUri = String.Format("http://{0}:{1}", HOST, PORT);

      authorize();
    }
  }

  // Wait for a Oauth callback from the Identity manager to receive an auth code, then ask for a token
  // When the token is received close the server
  public override async void _Process(float delta)
  {
    if (redirectServer.IsConnectionAvailable())
    {
      GD.Print("get Auth Code from callback");
      StreamPeerTCP connection = redirectServer.TakeConnection();

      string request = connection.GetString(connection.GetAvailableBytes());

      string authCode = request.Split("&code")[1].Split("=")[1].Split(" ")[0];
      GD.Print(authCode);
      if (request != "" && authCode != null)
      {
        await getTokenFromAuthCode(authCode);

        var response = loadHTML(HTML_REDIRECTION_PAGE);
        connection.PutData(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK Content-Type: text/html; charset=utf-8 \r\n\r\n"));
        connection.PutData(response.ToUTF8());

        GD.Print("stop server");
        connection.DisconnectFromHost();
        redirectServer.Stop();
      }

    }
  }
  public async void authorize()
  {
    GD.Print("Authorizing");
    bool isAuthorized = false;

    // Load the access token from a previous login
    loadToken();
    // verify if the token exists locally and call the API "/introspect to verify if the token is expired
    if (!(isAuthorized = await isTokenValid()))
    {
      // verify if a refresh token exists locally and, ask for a new access token from the API /token.
      if (!(isAuthorized = await refreshTokens()))
      {
        // Standard auth flow:
        // 1) Open a browser to do a Oauth2 authentication and obtains an auth code
        // 2) ask the token to the /token api
        // 3) save it locally and send a signal to the main scene to enable the next player screen
        getAuthCode();
      };
    }

    if (isAuthorized)
    {
      GD.Print("Authorized");
      EmitSignal("playerAuthenticated", isAuthorized);

    }
    else
    {
      GD.Print("NO Authorized");
    }
  }

  private void getAuthCode()
  {
    GD.Print("call login - ask auth code");
    redirectServer.Listen((ushort)PORT, HOST);

    string[] bodyPart = {
  String.Format("client_id={0}", clientID),
  String.Format("redirect_uri={0}", redirectUri),
  "response_type=code",
  "scope=openid"
  };

    string url = String.Format("{0}?{1}", authServer, String.Join("&", bodyPart));

    OS.ShellOpen(url);
  }

  private async Task<bool> getTokenFromAuthCode(string authCode)
  {
    GD.Print("get Token from AuthCode");
    string[] header ={
  "Content-Type:application/x-www-form-urlencoded"
  };


    string[] bodyPart = {
  String.Format("code={0}", authCode),
  String.Format("client_id={0}", clientID),
  String.Format("client_secret={0}", clientSecret),
  String.Format("redirect_uri={0}", redirectUri),
  "grant_type=authorization_code",
  "scope=openId"
  };

    var bodyParsed = await makeRequest(tokenServer, header, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.Contains("access_token"))
    {
      token = bodyParsed["access_token"].ToString();
      refreshToken = bodyParsed["refresh_token"].ToString();

      saveToken();
      EmitSignal("playerAuthenticated", true);
      return true;
    }
    else
    {
      GD.Print("Error: no token received");
      EmitSignal("playerAuthenticated", false);
      return false;
    }

  }

  private async Task<bool> isTokenValid()
  {
    GD.Print("validate token");

    if (token == null)
    {
      GD.Print("token not found");
      return false;
    }

    string[] header = { "Content-Type:application/x-www-form-urlencoded" };

    string[] bodyPart = {
  String.Format("client_id={0}", clientID),
  String.Format("client_secret={0}", clientSecret),
  String.Format("token={0}", token),
  "token_type_hint=access_token"
  };

    var bodyParsed = await makeRequest(tokenServer + "/introspect", header, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.Contains("exp"))
    {
      var expiration = Double.Parse(bodyParsed["exp"].ToString());

      GD.Print(expiration);

      GD.Print(DateTimeOffset.Now.ToUnixTimeSeconds());

      if (expiration > DateTimeOffset.Now.ToUnixTimeSeconds())
      {
        return true;
      }
    }

    GD.Print("token expired");
    return false;

  }

  private async Task<bool> refreshTokens()
  {
    GD.Print("fetch refresh - ask new access token");
    if (refreshToken == null)
    {
      GD.Print("refresh token not locally saved");
      return false;
    }

    string[] header = { "Content-Type:application/x-www-form-urlencoded" };

    string[] bodyPart = {
  String.Format("client_id={0}", clientID),
  String.Format("client_secret={0}", clientSecret),
  String.Format("refresh_token={0}", refreshToken),
  "grant_type=refresh_token",
  };

    var bodyParsed = await makeRequest(tokenServer, header, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.Contains("access_token"))
    {
      token = bodyParsed["access_token"].ToString();
      saveToken();
      GD.Print("saved new access token");

      return true;
    }

    GD.Print("no new access token");
    return false;
  }

  private void saveToken()
  {
    var dir = new Directory();
    if (!dir.DirExists(SAVE_DIR))
    {
      dir.MakeDirRecursive(SAVE_DIR);
    }

    var file = new File();
    var error = file.Open(save_path, File.ModeFlags.Write);

    if (error == Error.Ok)
    {
      var tokens = new Dictionary();

      tokens["token"] = token;
      tokens["refreshToken"] = refreshToken;

      file.StoreVar(tokens);
      file.Close();

      GD.Print("Token saved successfully");
    }
  }

  private void loadToken()
  {
    var file = new File();
    if (file.FileExists(save_path))
    {
      var error = file.Open(save_path, File.ModeFlags.Read);

      if (error == Error.Ok)
      {
        var tokens = file.GetVar() as Dictionary;

        token = tokens["token"].ToString();
        refreshToken = tokens["refreshToken"].ToString();

        file.Close();
        GD.Print("get local token");
      }
    }
  }

  private string loadHTML(string path)
  {
    var file = new File();
    var HTML = "<!DOCTYPE html><html><body><p>Logged In</p></body></html>";

    if (file.FileExists(path))
    {
      file.Open(path, File.ModeFlags.Read);
      HTML = file.GetAsText().Replace("	", "\t").Insert(0, "\n");
      file.Close();

      return HTML;
    }

    return HTML;
  }

  private async Task<Dictionary> makeRequest(string url, string[] header, HTTPClient.Method method, string body, String signal = "request_completed")
  {
    HTTPRequest httpRequest = new HTTPRequest();
    AddChild(httpRequest);
    var Err = httpRequest.Request(url, header, true, method, body);

    if (Err != Error.Ok)
    {
      GD.Print("An error occurred in HTTP request", Err);
      return null;
    }


    var response = await ToSignal(httpRequest, signal);
    JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(response[3] as byte[]));

    return json.Result as Dictionary;
  }
}

