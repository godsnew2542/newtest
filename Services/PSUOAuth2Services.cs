using IdentityModel.Client;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.IServices;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using LoanApp.Model.ThaIdModel;

namespace LoanApp.Services
{
    public class PsuoAuth2Services : IPsuoAuth2Services
    {
        private HttpClient _httpClient { get; }
        private AppSettings _appSettings { get; }
        private IOptions<ThaIdSettings> ThaId { get; }
        private ModelContext Context { get; set; }

        public PsuoAuth2Services(HttpClient httpClient,IOptions<AppSettings> appSettings, ModelContext context, IOptions<ThaIdSettings> thaId)
        {
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
            Context = context;
            ThaId = thaId;
        }

        public string CallAuthorize()
        {
            var ru = new RequestUrl(_appSettings.OAuthBaseAddress + "?oauth=authorize");
            var OAuthRedirectUri = _appSettings.OAuthRedirectUri;
            var ClientId = _appSettings.OAuthClientID;

            var url = ru.CreateAuthorizeUrl(
                clientId: ClientId,
                responseType: "code",
                redirectUri: OAuthRedirectUri,
                scope: _appSettings.OAuthScope
                );
            return url;
        }

        public async Task<TokenInfo> CallTokenAsync(string code)
        {
            var OAuthRedirectUri = _appSettings.OAuthRedirectUri;
            var ClientId = _appSettings.OAuthClientID;
            var ClientSecret = _appSettings.OAuthClientSecret;

            var response = await _httpClient.RequestTokenAsync(new TokenRequest
            {
                Address = _appSettings.OAuthBaseAddress + "?oauth=token",
                GrantType = "authorization_code",
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Parameters =
            {
                { "response_type" , "code" },
                { "code", code },
                { "redirect_uri", OAuthRedirectUri },
            }
            });

            TokenInfo? tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(response.Json.ToString());


            return tokenInfo == null ? new() : tokenInfo;
        }

        private class RevokeInfo
        {
            public bool revoked { get; set; }
        }

        public async Task<User> GetProfileByAccessTokenAsync(string accessToken)
        {
            var response = await _httpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = _appSettings.OAuthBaseAddress + $"?oauth={_appSettings.OAuthProfile}",
                Token = accessToken,
            });
            UserToken? userToken = JsonConvert.DeserializeObject<UserToken>(response.Json.ToString());

            User user = new()
            {
                UserName = userToken?.Username ?? string.Empty,
                StaffId = userToken?.Staffid ?? string.Empty,
                StaffNameThai = userToken?.Description ?? string.Empty,
                StaffNameEng = userToken?.Displayname ?? string.Empty,
                CampusId = userToken?.CampusId ?? string.Empty,
            };

            return user;
        }

        public async Task<List<LoanGroup>> GetAllLoanGroups()
        {
            try
            {
                List<LoanGroup> roleGroup = await Context.LoanGroups.ToListAsync();
                return roleGroup;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region ThaID
        public string CallAuthorizeThaiId()
        {
            var ru = new RequestUrl(ThaId.Value.OAuthBaseAddress + "/api/v2/oauth2/auth/");
            var OAuthRedirectUri = ThaId.Value.OAuthRedirectUri;
            var clientId = ThaId.Value.OAuthClientID;
            var scope = ThaId.Value.OAuthScope;
            var state = ThaId.Value.OAuthState;

            var url = ru.CreateAuthorizeUrl(
                clientId: clientId!,
                responseType: "code",
                redirectUri: OAuthRedirectUri,
                scope: scope,
                state: state
                );
            return url;
        }

        public async Task<Tuple<TokenInfo?, UserInfo?>> CallTokenThaId(string code)
        {
            var OAuthRedirectUri = ThaId.Value.OAuthRedirectUri;
            var clientId = ThaId.Value.OAuthClientID;
            var clientSecret = ThaId.Value.OAuthClientSecret;

            var form = new Dictionary<string, string>
               {
                   {"grant_type", "authorization_code"},
                   {"code", code},
                   { "redirect_uri", OAuthRedirectUri ?? throw new InvalidOperationException() },
            };

            var base64Auth = System.Text.Encoding.UTF8.GetBytes(clientId + ":" + clientSecret);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(base64Auth));

            var response = _httpClient.PostAsync(ThaId.Value.OAuthBaseAddress
                + "/api/v2/oauth2/token/", new FormUrlEncodedContent(form)).Result;

            string json = await response.Content.ReadAsStringAsync();


            TokenInfo? tokenInfo = JsonConvert.DeserializeObject<TokenInfo?>(json);
            UserInfo? userInfo = JsonConvert.DeserializeObject<UserInfo?>(json);

            Tuple<TokenInfo?, UserInfo?> info = new Tuple<TokenInfo?, UserInfo?>(tokenInfo, userInfo);

            //if (tokenInfo == null)
            //{
            //    logger.LogError("tokenInfo is null");
            //}

            return info;
        }

        public async Task<bool> SignOut(string bearerToken)
        {
            var OAuthRedirectUri = ThaId.Value.OAuthRedirectUri;
            var clientId = ThaId.Value.OAuthClientID;
            var clientSecret = ThaId.Value.OAuthClientSecret;

            var form = new Dictionary<string, string>
               {
                   {"token", "Bearer "+ bearerToken },
                   { "redirect_uri", OAuthRedirectUri ?? throw new InvalidOperationException() },
            };

            var base64Auth = System.Text.Encoding.UTF8.GetBytes(clientId + ":" + clientSecret);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(base64Auth));

            var response = _httpClient.PostAsync(ThaId.Value.OAuthBaseAddress
                + "/api/v2/oauth2/revoke/", new FormUrlEncodedContent(form)).Result;

            var tokenInfo = response.Content.ReadAsStringAsync().Result;

            return await Task.FromResult(response.IsSuccessStatusCode);
        }

        #endregion

    }
}
