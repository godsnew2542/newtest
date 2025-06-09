using LoanApp.Model.Helper;
using WordDocument.IServices;

namespace LoanApp.IServices
{
    public interface IMSWordService
    {
        /// <summary>
        /// wwwroot\css\images\
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetLocationFileDirImages(string fileName);
    }

    public class MSWordService : IMSWordService
    {
        private IWebHostEnvironment env { get; } = null!;

        public MSWordService(IWordOptions wordOptions, IWebHostEnvironment env)
        {
            this.env = env;
        }

        public string GetLocationFileDirImages(string fileName)
        {
            var SeparatorChar = (Utility.CheckOSisWindows() ? Path.DirectorySeparatorChar : Path.AltDirectorySeparatorChar);

            var imageLoc = env.WebRootPath + SeparatorChar + "css" + SeparatorChar + "images" + SeparatorChar + fileName;

            return imageLoc;
        }
    }
}
