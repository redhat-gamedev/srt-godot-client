using Godot;
using System;
using System.Text;
using Godot.Collections;
using System.Threading.Tasks;

public class Authorization : Control
{
  public Serilog.Core.Logger _serilogger;

  const int PORT = 31419;
  const string HOST = "127.0.0.1";
  const string BINDING = "localhost";
  const string clientID = "Str-game-client-001";
  const string clientSecret = "bc88ada1-98c2-4afa-ba88-5656933bf1d0";
  const string authServer = "https://sso-sso.vabar-vpc-cluster-153f1de160110098c1928a6c05e19444-0000.eu-gb.containers.appdomain.cloud/auth/realms/str-game/protocol/openid-connect/auth";
  const string tokenServer = "https://sso-sso.vabar-vpc-cluster-153f1de160110098c1928a6c05e19444-0000.eu-gb.containers.appdomain.cloud/auth/realms/str-game/protocol/openid-connect/token";
  TCP_Server redirectServer = new TCP_Server();
  string redirectUri = String.Format("http://{0}:{1}", BINDING, PORT);
  string refreshToken;
  string token;

  const string SAVE_DIR = "user://token/";
  const string HTML_REDIRECTION_PAGE = "res://Assets/Artwork/Home.html";

  string save_path = SAVE_DIR + "token.dat";

  [Signal] public delegate void playerAuthenticated(bool isAuthorized);

  private void saveToken()
  {
    var dir = new Directory();
    if (!dir.DirExists(SAVE_DIR))
    {
      dir.MakeDirRecursive(SAVE_DIR);
    }

    var file = new File();
    var error = file.OpenEncryptedWithPass(save_path, File.ModeFlags.Write, "game-test");

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
      var error = file.OpenEncryptedWithPass(save_path, File.ModeFlags.Read, "game-test");

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

  public override void _Ready()
  {
    authorize();
  }

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
        SetProcess(false);
        await getTokenFromAuthCode(authCode);

        connection.PutData(Encoding.ASCII.GetBytes("HTTP/1.1 200 \r\n\r\n"));
        connection.PutData(Encoding.ASCII.GetBytes(loadHTML(HTML_REDIRECTION_PAGE)));
        GD.Print("stop server");
        redirectServer.Stop();
      }

    }
  }
  public async void authorize()
  {
    GD.Print("Authorizing");
    bool isAuthorized = false;
    loadToken();
    SetProcess(false);

    if (!(isAuthorized = await isTokenValid()))
    {
      if (!(isAuthorized = await refreshTokens()))
      {
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
    SetProcess(true);
    redirectServer.Listen(PORT, HOST);

    string[] bodyPart = {
    String.Format("client_id={0}", clientID),
    String.Format("redirect_uri={0}", redirectUri),
    "response_type=code",
    "scope=openId"

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


    HTTPRequest httpRequest = new HTTPRequest();
    AddChild(httpRequest);

    Error Err = httpRequest.Request(tokenServer, header, true, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (Err != Error.Ok)
    {
      GD.Print("An error occurred in HTTP request", Err);
    }

    var response = await ToSignal(httpRequest, "request_completed");
    JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(response[3] as byte[]));
    var bodyParsed = json.Result as Dictionary;

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
      return false;

    }

    string[] header ={
    "Content-Type:application/x-www-form-urlencoded"
  };

    string[] bodyPart = {
    String.Format("client_id={0}", clientID),
    String.Format("client_secret={0}", clientSecret),
    String.Format("token={0}", token),
    "token_type_hint=access_token"
  };

    HTTPRequest httpRequest = new HTTPRequest();
    AddChild(httpRequest);
    var Err = httpRequest.Request(tokenServer + "/introspect", header, true, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (Err != Error.Ok)
    {
      GD.Print("An error occurred in HTTP request", Err);
    }


    var response = await ToSignal(httpRequest, "request_completed");
    JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(response[3] as byte[]));
    var bodyParsed = json.Result as Dictionary;

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

    string[] header ={
    "Content-Type:application/x-www-form-urlencoded"
  };

    string[] bodyPart = {
    String.Format("client_id={0}", clientID),
    String.Format("client_secret={0}", clientSecret),
    String.Format("refresh_token={0}", refreshToken),
    "grant_type=refresh_token",
  };

    HTTPRequest httpRequest = new HTTPRequest();
    AddChild(httpRequest);
    Error Err = httpRequest.Request(tokenServer, header, true, HTTPClient.Method.Post, String.Join("&", bodyPart));

    if (Err != Error.Ok)
    {
      GD.Print("An error occurred in HTTP request", Err);
    }

    var response = await ToSignal(httpRequest, "request_completed");

    JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(response[3] as byte[]));
    var bodyParsed = json.Result as Dictionary;

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
}

