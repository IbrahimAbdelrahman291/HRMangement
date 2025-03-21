namespace HRMangement.Helper
{
    public static class FilesHandler
    {
        public static string Upload(IFormFile file, string FolderName)
        {
            string FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Files", FolderName);

            string FileName = $"{Guid.NewGuid()}{file.FileName}";

            string FilePath = Path.Combine(FolderPath, FileName);
            using var fs = new FileStream(FilePath, FileMode.Create);
            file.CopyTo(fs);
            return FileName;
        }
    }
}
