using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Model.ThaIdModel;

namespace LoanApp.IServices
{
    public interface IPsuoAuth2Services
    {
        public string CallAuthorize();
        public Task<TokenInfo> CallTokenAsync(string code);
        public Task<User> GetProfileByAccessTokenAsync(string accessToken);
        Task<List<LoanGroup>> GetAllLoanGroups();

        #region ThaID
        string CallAuthorizeThaiId();
        Task<Tuple<TokenInfo?, UserInfo?>> CallTokenThaId(string code);

        Task<bool> SignOut(string bearerToken);

        #endregion
    }

    public class TokenInfo
    {
        //{ "access_token":"xx","expires_in":3600,"token_type":"Bearer","scope":"profile","refresh_token":"yyyy"}
        public string? access_token { get; set; }
        public string? expires_in { get; set; }
        public string? token_type { get; set; }
        public string? scope { get; set; }
        public string? refresh_token { get; set; }
    }

    public class UserToken
    {
        public string? Username { get; set; }
        public string? Staffid { get; set; }
        public string? Description { get; set; }
        public string? Displayname { get; set; }
        public string? CampusId { get; set; }
    }
}
