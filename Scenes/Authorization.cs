using Godot;
using System;
using System.Text;
using Godot.Collections;
using System.Threading.Tasks;

public partial class Authorization : Control
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  string port;
  string host;
  string clientID;
  string clientSecret;
  string authServer;
  string tokenServer;
  string refreshToken;
  string token;
  string redirectUri;
  const string SAVE_DIR = "user://auth/";
  const string save_path = SAVE_DIR + "token.dat";
  const string HTML_REDIRECTION_PAGE = "res://Assets/Artwork/Home.html";

  TcpServer redirectServer = new TcpServer();
  [Signal] public delegate void PlayerAuthenticatedEventHandler(bool isAuthorized);

  //Load the configuration and call the login process
  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    var clientConfig = new ConfigFile();

    Godot.Error err = clientConfig.Load("res://Resources/client.cfg");
    // If config file failed to load and the game is in debug mode throw an error
    if (err != Godot.Error.Ok && OS.IsDebugBuild())
    {
      throw new Exception("Authorization.cs: Failed to load the client configuration file");
    }

    port = System.Environment.GetEnvironmentVariable("PORT") ?? (string)clientConfig.GetValue("auth", "port", "31419");
    host = System.Environment.GetEnvironmentVariable("ADDRESS") ?? (string)clientConfig.GetValue("auth", "address", "127.0.0.1");

    // TODO: handle when the settings are impossible to reconcile, like using auth without valid urls or whatever
    clientID = System.Environment.GetEnvironmentVariable("CLIENT_ID") ?? (string)clientConfig.GetValue("auth", "client_id", "nodefault");
    clientSecret = System.Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? (string)clientConfig.GetValue("auth", "client_secret", "nodefault");
    authServer = System.Environment.GetEnvironmentVariable("AUTH_API_URL") ?? (string)clientConfig.GetValue("auth", "auth_api_url", "nodefault");
    tokenServer = System.Environment.GetEnvironmentVariable("TOKEN_API_URL") ?? (string)clientConfig.GetValue("auth", "token_api_url", "nodefault");

    redirectUri = String.Format("http://{0}:{1}", host, port);

    authorize();
  }

  // Wait for a Oauth callback from the Identity manager to receive an auth code, then ask for a token
  // When the token is received close the server
  public override async void _Process(double delta)
  {
    if (redirectServer.IsConnectionAvailable())
    {
      _serilogger.Debug("Authorization.cs: get Auth Code from callback");
      StreamPeerTcp connection = redirectServer.TakeConnection();

      string request = connection.GetString(connection.GetAvailableBytes());

      string authCode = request.Split("&code")[1].Split("=")[1].Split(" ")[0];

      if (request != "" && authCode != null)
      {
        await getTokenFromAuthCode(authCode);

        var response = loadHTML(HTML_REDIRECTION_PAGE);
        connection.PutData(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK Content-Type: text/html; charset=utf-8 \r\n\r\n"));
        connection.PutData(response.ToUtf8Buffer());

        _serilogger.Debug("Authorization.cs: stop server");
        connection.DisconnectFromHost();
        redirectServer.Stop();
      }

    }
  }
  public async void authorize()
  {
    _serilogger.Debug("Authorization.cs: Authorizing");
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
      _serilogger.Debug("Authorization.cs: Authorized");
      EmitSignal("playerAuthenticated", isAuthorized);
    }
    else
    {
      _serilogger.Debug("Authorization.cs: No Authorized");
    }
  }

  public string getUserId()
  {
    if (token == null)
    {
      _serilogger.Debug("Authorization.cs: token not found");
      return null;
    }

    var payload = this.decodeJWTPayload(token);

    if (!payload.ContainsKey("preferred_username"))
    {
      return null;
    }

    return payload["preferred_username"].ToString();
  }

  private void getAuthCode()
  {
    _serilogger.Debug("Authorization.cs: call login - ask auth code");
    redirectServer.Listen(ushort.Parse(port), host);

    string[] bodyPart =
    {
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
    _serilogger.Debug("Authorization.cs: get Token from AuthCode");
    string[] header ={
    "Content-Type:application/x-www-form-urlencoded"
  };


    string[] bodyPart =
    {
      String.Format("code={0}", authCode),
      String.Format("client_id={0}", clientID),
      String.Format("client_secret={0}", clientSecret),
      String.Format("redirect_uri={0}", redirectUri),
      "grant_type=authorization_code",
      "scope=openId"
    };

    var bodyParsed = await makeRequest(tokenServer, header, HttpClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.ContainsKey("access_token"))
    {
      token = bodyParsed["access_token"].ToString();
      refreshToken = bodyParsed["refresh_token"].ToString();

      saveToken();
      EmitSignal("playerAuthenticated", true);
      return true;
    }
    else
    {
      _serilogger.Debug("Authorization.cs: Error: no token received");
      EmitSignal("playerAuthenticated", false);
      return false;
    }

  }

  private async Task<bool> isTokenValid()
  {
    _serilogger.Debug("Authorization.cs: validate token");

    if (token == null)
    {
      _serilogger.Debug("Authorization.cs: token not found");
      return false;
    }

    string[] header = { "Content-Type:application/x-www-form-urlencoded" };

    string[] bodyPart =
    {
      String.Format("client_id={0}", clientID),
      String.Format("client_secret={0}", clientSecret),
      String.Format("token={0}", token),
      "token_type_hint=access_token"
    };

    var bodyParsed = await makeRequest(tokenServer + "/introspect", header, HttpClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.ContainsKey("exp"))
    {
      var expiration = Double.Parse(bodyParsed["exp"].ToString());

      if (expiration > DateTimeOffset.Now.ToUnixTimeSeconds())
      {
        return true;
      }
    }

    _serilogger.Debug("Authorization.cs: token expired");
    return false;

  }

  private async Task<bool> refreshTokens()
  {
    _serilogger.Debug("Authorization.cs: fetch refresh - ask new access token");
    if (refreshToken == null)
    {
      _serilogger.Debug("Authorization.cs: refresh token not locally saved");
      return false;
    }

    string[] header = { "Content-Type:application/x-www-form-urlencoded" };

    string[] bodyPart =
    {
      String.Format("client_id={0}", clientID),
      String.Format("client_secret={0}", clientSecret),
      String.Format("refresh_token={0}", refreshToken),
      "grant_type=refresh_token"
    };

    var bodyParsed = await makeRequest(tokenServer, header, HttpClient.Method.Post, String.Join("&", bodyPart));

    if (bodyParsed.ContainsKey("access_token"))
    {
      token = bodyParsed["access_token"].ToString();
      saveToken();

      return true;
    }

    _serilogger.Debug("Authorization.cs: no new access token");
    return false;
  }

  private void saveToken()
  {
    if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
    {
      DirAccess.MakeDirRecursiveAbsolute(SAVE_DIR);
    }

    var fileObj = FileAccess.Open(save_path, FileAccess.ModeFlags.Write);

    if (fileObj != null)
    {
      var tokens = new Dictionary();

      tokens["token"] = token;
      tokens["refreshToken"] = refreshToken;

      fileObj.StoreVar(tokens);
      fileObj.Close();

      _serilogger.Debug("Authorization.cs: Token saved successfully");
    }
  }

  private void loadToken()
  {
    if (FileAccess.FileExists(save_path))
    {
      var fileObj = FileAccess.Open(save_path, FileAccess.ModeFlags.Read);

      if (fileObj != null)
      {
        var tokens = (Dictionary)fileObj.GetVar(true); // as Dictionary;

        token = tokens["token"].ToString();
        refreshToken = tokens["refreshToken"].ToString();

        fileObj.Close();
        _serilogger.Debug("Authorization.cs: get local token");
      }
    }
  }

  private string loadHTML(string path)
  {
    var HTML = "<!DOCTYPE html><html><body><p>Logged In</p></body></html>";

    if (FileAccess.FileExists(path))
    {
      var fileObj = FileAccess.Open(path, FileAccess.ModeFlags.Read);
      HTML = fileObj.GetAsText().Replace("	", "\t").Insert(0, "\n");
      fileObj.Close();

      return HTML;
    }

    return HTML;
  }

  private async Task<Dictionary> makeRequest(string url, string[] header, HttpClient.Method method, string body, String signal = "request_completed")
  {
    HttpRequest httpRequest = new HttpRequest();
    AddChild(httpRequest);
    var Err = httpRequest.Request(url, header, method, body);

    if (Err != Error.Ok)
    {
      _serilogger.Debug("Authorization.cs: An error occurred in HTTP request", Err);
      return null;
    }

    var response = await ToSignal(httpRequest, signal);
    Json json = (Json)Json.ParseString(Encoding.UTF8.GetString((byte[])response[3]));

    return (Dictionary)json.Data;
  }

  private Dictionary decodeJWTPayload(string jwt)
  {
    var parts = jwt.Split(".");
    var payloadPart = parts[1];

    switch (payloadPart.Length % 4)
    {
      case 2:
        payloadPart += "==";
        break;
      case 3:
        payloadPart += "=";
        break;
    }

    var payload = Marshalls.Base64ToRaw(payloadPart.Replace("_", "/").Replace("-", "+"));
    var json = (Json)Json.ParseString(Encoding.UTF8.GetString((byte[])payload));

    return (Dictionary)json.Data;
  }
}
