namespace LoanApp.IServices
{
    public interface IUtilityServer
    {
        /// <summary>
        /// ตรวจสอบ DB ทดสอบ หรือไหม [true ใช่]
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        bool CheckDBtest(string dbName = "NORA178");
    }
}
